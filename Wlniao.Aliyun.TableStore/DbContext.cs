/*-----------------------Copyright 2017 www.wlniao.com---------------------------
    文件名称：Wlniao\MongoDB\DbContext.cs
    适用环境：NETCoreCLR 1.0/2.0
    最后修改：2016年3月24日02:58:50
    功能描述：DbContext基类

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

               http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using Aliyun.TableStore;
using Aliyun.TableStore.Request;
using Aliyun.TableStore.DataModel;
using System.Reflection;
using Aliyun.TableStore.DataModel.Search;
using Aliyun.TableStore.DataModel.Search.Sort;
using Aliyun.TableStore.DataModel.Search.Query;
using System.Diagnostics;
using Wlniao.Log;

namespace Wlniao.Aliyun.TableStore
{
    /// <summary>
    /// DbContext基类
    /// </summary>
    public partial class DbContext
    {
        /// <summary>
        /// 
        /// </summary>
        protected static OTSClientConfig OtsConfig = null;

        /// <summary>
        /// 
        /// </summary>
        private static Dictionary<Type, String> tableNames = new Dictionary<Type, String>();
        /// <summary>
        /// 
        /// </summary>
        private static Dictionary<String, PrimaryKeySchema> tableKey = new Dictionary<String, PrimaryKeySchema>();

        /// <summary>
        /// 
        /// </summary>
        protected static void OnConfiguring(OTSClientConfig config)
        {
            var watch = Stopwatch.StartNew();
            OtsConfig = config;
            OtsConfig.OTSDebugLogHandler = null;
            OtsConfig.OTSErrorLogHandler = null;
            var client = new OTSClient(config);
            var resListTable = client.ListTable(new ListTableRequest());
            foreach (var table in resListTable.TableNames)
            {
                var resDescribeTable = client.DescribeTable(new DescribeTableRequest(table));
                if (resDescribeTable != null && resDescribeTable.TableMeta != null && resDescribeTable.TableMeta.PrimaryKeySchema != null)
                {
                    tableKey.Add(table, resDescribeTable.TableMeta.PrimaryKeySchema);
                }
            }
            watch.Stop();
            Console.WriteLine("Ots Init: " + watch.Elapsed);
        }

        /// <summary>
        /// 
        /// </summary>
        protected static void OnConfiguring(String instanceName, String accessKeyID, String accessKeySecret, String regionId)
        {
            var watch = Stopwatch.StartNew();
            OtsConfig = new OTSClientConfig("https://" + instanceName + "." + regionId + ".ots.aliyuncs.com", accessKeyID, accessKeySecret, instanceName);
            try
            {
                var client = new OTSClient(OtsConfig);
                var resListTable = client.ListTable(new ListTableRequest());
                foreach (var table in resListTable.TableNames)
                {
                    var resDescribeTable = client.DescribeTable(new DescribeTableRequest(table));
                    if (resDescribeTable != null && resDescribeTable.TableMeta != null && resDescribeTable.TableMeta.PrimaryKeySchema != null)
                    {
                        tableKey.Add(table, resDescribeTable.TableMeta.PrimaryKeySchema);
                    }
                }
                OtsConfig.OTSDebugLogHandler = null;
                OtsConfig.OTSErrorLogHandler = null;
                watch.Stop();
                Console.WriteLine("Ots Init: " + watch.Elapsed);
                #region 尝试使用内网链接地址
                try
                {
#if !DEBUG
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        var tmp = new OTSClientConfig("https://" + instanceName + "." + regionId + ".ots-internal.aliyuncs.com", accessKeyID, accessKeySecret, instanceName);
                        var tmpListTable = new OTSClient(tmp).ListTable(new ListTableRequest());
                        foreach (var table in tmpListTable.TableNames)
                        {
                            OtsConfig = tmp;
                            OtsConfig.OTSDebugLogHandler = null;
                            OtsConfig.OTSErrorLogHandler = null;
                            return;
                        }
                    });
#endif
                }
                catch { }
#endregion
            }
            catch (Exception ex)
            {
                watch.Stop();
                Console.WriteLine("Ots Init: " + ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        protected static string SetTable(Type type)
        {
#region 初始化缓存
            if (!tableNames.ContainsKey(type))
            {
                foreach (var attr in type.CustomAttributes)
                {
                    if (attr.AttributeType == typeof(System.ComponentModel.DataAnnotations.Schema.TableAttribute))
                    {
                        tableNames.TryAdd(type, attr.ConstructorArguments[0].Value.ToString());
                        break;
                    }
                }
            }
#endregion

            var name = tableNames.ContainsKey(type) ? tableNames[type] : type.Name.ToLower();

#region 分析表结构
            var pkSchema = new PrimaryKeySchema();
            foreach (PropertyInfo info in Wlniao.Runtime.Reflection.GetPropertyList(type))
            {
                if (info.GetCustomAttribute(typeof(System.ComponentModel.DataAnnotations.KeyAttribute)) == null)
                {
                    continue;
                }
                else if (info.PropertyType == typeof(int))
                {
                    pkSchema.Add(info.Name, ColumnValueType.Integer);
                }
                else if (info.PropertyType == typeof(string))
                {
                    pkSchema.Add(info.Name, ColumnValueType.String);
                }
                else if (info.PropertyType == typeof(byte[]))
                {
                    pkSchema.Add(info.Name, ColumnValueType.Binary);
                }
                else
                {
                    throw new Exception("不支持当前类型的主键");
                }
            };
#endregion

#region 生成表结构
            if (tableKey.ContainsKey(name))
            {
#region 对比已有结构
                tableKey[name].ForEach(key =>
                {
                    pkSchema.ForEach(tmp =>
                    {
                        if (key.Item1 == tmp.Item1 && (key.Item2 != tmp.Item2 || key.Item3 != tmp.Item3))
                        {
                            Loger.Fatal("Table structure is inconsistent, table name:" + name);
                            return;
                        }
                    });
                });
#endregion
            }
            else
            {
#region 创建新表
                //通过表名和主键列的schema创建一个tableMeta
                var client = new OTSClient(OtsConfig);
                var tableMeta = new TableMeta(name, pkSchema);
                // 设定预留读吞吐量为0，预留写吞吐量为0
                var reservedThroughput = new CapacityUnit(0, 0);

                try
                {
                    // 构造CreateTableRequest对象
                    var reqCreateTable = new CreateTableRequest(tableMeta, reservedThroughput);
                    // 调用client的CreateTable接口，如果没有抛出异常，则说明成功，否则失败
                    client.CreateTable(reqCreateTable);

                    // 生成查询索引
                    var reqCreateSearchIndex = new CreateSearchIndexRequest(name, name);
                    var fieldSchemas = new List<FieldSchema>();
                    foreach (PropertyInfo info in Wlniao.Runtime.Reflection.GetPropertyList(type))
                    {
                        if (info.GetCustomAttribute(typeof(System.ComponentModel.DataAnnotations.CompareAttribute)) == null)
                        {
                            continue;
                        }
                        else if (info.PropertyType == typeof(int) || info.PropertyType == typeof(long))
                        {
                            fieldSchemas.Add(new FieldSchema(info.Name, FieldType.LONG) { index = true });
                        }
                        else if (info.PropertyType == typeof(string))
                        {
                            fieldSchemas.Add(new FieldSchema(info.Name, FieldType.TEXT) { index = true });
                        }
                        else if (info.PropertyType == typeof(double))
                        {
                            fieldSchemas.Add(new FieldSchema(info.Name, FieldType.DOUBLE) { index = true });
                        }
                        else if (info.PropertyType == typeof(bool))
                        {
                            fieldSchemas.Add(new FieldSchema(info.Name, FieldType.BOOLEAN) { index = true });
                        }
                    };
                    if (fieldSchemas.Count > 0)
                    {
                        reqCreateSearchIndex.IndexSchame = new IndexSchema() { FieldSchemas = fieldSchemas };
                        client.CreateSearchIndex(reqCreateSearchIndex);
                        Loger.Info("Create table succeeded, table name:" + name);
                    }
                }
                // 处理异常
                catch (Exception ex)
                {
                    Console.WriteLine("Create table failed, exception:{0}", ex.Message);
                }
#endregion
            }
#endregion

            return name;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        protected static void DelTable(Type type)
        {
#region 初始化缓存
            if (!tableNames.ContainsKey(type))
            {
                foreach (var attr in type.CustomAttributes)
                {
                    if (attr.AttributeType == typeof(System.ComponentModel.DataAnnotations.Schema.TableAttribute))
                    {
                        tableNames.TryAdd(type, attr.ConstructorArguments[0].Value.ToString());
                        break;
                    }
                }
            }
#endregion

            var name = tableNames.ContainsKey(type) ? tableNames[type] : type.Name.ToLower();
#region 删除已有表
            try
            {
                var request = new DeleteTableRequest(name);
                new OTSClient(OtsConfig).DeleteTable(request);
                Loger.Info("Delete table succeeded, table name:" + name);
            }
            // 处理异常
            catch (Exception ex)
            {
                Console.WriteLine("Delete table failed, exception:{0}", ex.Message);
            }
#endregion
        }


        /// <summary>
        /// 
        /// </summary>
        private OTSClient otsClient = null;
        /// <summary>
        /// 
        /// </summary>
        public OTSClient OtsClient
        {
            get
            {
                if (otsClient == null)
                {
                    otsClient = new OTSClient(OtsConfig);
                }
                return otsClient;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        public void AddRow(Object obj)
        {
            var type = obj.GetType();
            var name = tableNames.ContainsKey(type) ? tableNames[type] : SetTable(type);

            var keys = new PrimaryKey();
            var columns = new AttributeColumns();
            foreach (PropertyInfo info in Wlniao.Runtime.Reflection.GetPropertyList(type))
            {
                var methodInfo = info.GetGetMethod();
                if (methodInfo == null || methodInfo.IsStatic || !methodInfo.IsPublic)
                {
                    continue;
                }
                if (info.GetCustomAttribute(typeof(System.ComponentModel.DataAnnotations.KeyAttribute)) == null)
                {
#region 属性列处理
                    var propertyValue = info.GetValue(obj, null);
                    if (propertyValue == null)
                    {
                        continue;
                    }
                    else if (info.PropertyType == typeof(int))
                    {
                        columns.Add(info.Name, new ColumnValue((int)propertyValue));
                    }
                    else if (info.PropertyType == typeof(long))
                    {
                        columns.Add(info.Name, new ColumnValue((long)propertyValue));
                    }
                    else if (info.PropertyType == typeof(string))
                    {
                        columns.Add(info.Name, new ColumnValue((string)propertyValue));
                    }
                    else if (info.PropertyType == typeof(bool))
                    {
                        columns.Add(info.Name, new ColumnValue((bool)propertyValue));
                    }
                    else if (info.PropertyType == typeof(double))
                    {
                        columns.Add(info.Name, new ColumnValue((double)propertyValue));
                    }
                    else if (info.PropertyType == typeof(ulong))
                    {
                        columns.Add(info.Name, new ColumnValue((ulong)propertyValue));
                    }
                    else if (info.PropertyType == typeof(byte[]))
                    {
                        columns.Add(info.Name, new ColumnValue((byte[])propertyValue));
                    }
#endregion
                }
                else
                {
#region 主键字段处理
                    var propertyValue = info.GetValue(obj, null);
                    if (propertyValue == null)
                    {
                        throw new OTSException("Empty PrimaryKey: " + info.Name);
                    }
                    else if (info.PropertyType == typeof(int))
                    {
                        keys.Add(info.Name, new ColumnValue((int)propertyValue));
                    }
                    else if (info.PropertyType == typeof(string))
                    {
                        keys.Add(info.Name, new ColumnValue((string)propertyValue));
                    }
                    else if (info.PropertyType == typeof(byte[]))
                    {
                        keys.Add(info.Name, new ColumnValue((byte[])propertyValue));
                    }
#endregion
                }
            }

            var req = new PutRowRequest(name, new Condition(RowExistenceExpectation.EXPECT_NOT_EXIST), keys, columns);
            OtsClient.PutRow(req);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        public void PutRow(Object obj)
        {
            var type = obj.GetType();
            var name = tableNames.ContainsKey(type) ? tableNames[type] : SetTable(type);

            var keys = new PrimaryKey();
            var columns = new AttributeColumns();
            foreach (PropertyInfo info in Wlniao.Runtime.Reflection.GetPropertyList(type))
            {
                var methodInfo = info.GetGetMethod();
                if (methodInfo == null || methodInfo.IsStatic || !methodInfo.IsPublic)
                {
                    continue;
                }
                if (info.GetCustomAttribute(typeof(System.ComponentModel.DataAnnotations.KeyAttribute)) == null)
                {
#region 属性列处理
                    var propertyValue = info.GetValue(obj, null);
                    if (propertyValue == null)
                    {
                        continue;
                    }
                    else if (info.PropertyType == typeof(int))
                    {
                        columns.Add(info.Name, new ColumnValue((int)propertyValue));
                    }
                    else if (info.PropertyType == typeof(long))
                    {
                        columns.Add(info.Name, new ColumnValue((long)propertyValue));
                    }
                    else if (info.PropertyType == typeof(string))
                    {
                        columns.Add(info.Name, new ColumnValue((string)propertyValue));
                    }
                    else if (info.PropertyType == typeof(bool))
                    {
                        columns.Add(info.Name, new ColumnValue((bool)propertyValue));
                    }
                    else if (info.PropertyType == typeof(double))
                    {
                        columns.Add(info.Name, new ColumnValue((double)propertyValue));
                    }
                    else if (info.PropertyType == typeof(ulong))
                    {
                        columns.Add(info.Name, new ColumnValue((ulong)propertyValue));
                    }
                    else if (info.PropertyType == typeof(byte[]))
                    {
                        columns.Add(info.Name, new ColumnValue((byte[])propertyValue));
                    }
#endregion
                }
                else
                {
#region 主键字段处理
                    var propertyValue = info.GetValue(obj, null);
                    if (propertyValue == null)
                    {
                        throw new OTSException("Empty PrimaryKey: " + info.Name);
                    }
                    else if (info.PropertyType == typeof(int))
                    {
                        keys.Add(info.Name, new ColumnValue((int)propertyValue));
                    }
                    else if (info.PropertyType == typeof(string))
                    {
                        keys.Add(info.Name, new ColumnValue((string)propertyValue));
                    }
                    else if (info.PropertyType == typeof(byte[]))
                    {
                        keys.Add(info.Name, new ColumnValue((byte[])propertyValue));
                    }
#endregion
                }
            }

            var req = new PutRowRequest(name, new Condition(RowExistenceExpectation.IGNORE), keys, columns);
            OtsClient.PutRow(req);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        public void UpdateRow(Object obj)
        {
            var type = obj.GetType();
            var name = tableNames.ContainsKey(type) ? tableNames[type] : SetTable(type);

            var keys = new PrimaryKey();
            var columns = new UpdateOfAttribute();
            foreach (PropertyInfo info in Wlniao.Runtime.Reflection.GetPropertyList(type))
            {
                var methodInfo = info.GetGetMethod();
                if (methodInfo == null || methodInfo.IsStatic || !methodInfo.IsPublic)
                {
                    continue;
                }
                if (info.GetCustomAttribute(typeof(System.ComponentModel.DataAnnotations.KeyAttribute)) == null)
                {
#region 属性列处理
                    var propertyValue = info.GetValue(obj, null);
                    if (propertyValue == null)
                    {
                        continue;
                    }
                    else if (info.PropertyType == typeof(int))
                    {
                        columns.AddAttributeColumnToPut(info.Name, new ColumnValue((int)propertyValue));
                    }
                    else if (info.PropertyType == typeof(long))
                    {
                        columns.AddAttributeColumnToPut(info.Name, new ColumnValue((long)propertyValue));
                    }
                    else if (info.PropertyType == typeof(string))
                    {
                        columns.AddAttributeColumnToPut(info.Name, new ColumnValue((string)propertyValue));
                    }
                    else if (info.PropertyType == typeof(bool))
                    {
                        columns.AddAttributeColumnToPut(info.Name, new ColumnValue((bool)propertyValue));
                    }
                    else if (info.PropertyType == typeof(double))
                    {
                        columns.AddAttributeColumnToPut(info.Name, new ColumnValue((double)propertyValue));
                    }
                    else if (info.PropertyType == typeof(ulong))
                    {
                        columns.AddAttributeColumnToPut(info.Name, new ColumnValue((ulong)propertyValue));
                    }
                    else if (info.PropertyType == typeof(byte[]))
                    {
                        columns.AddAttributeColumnToPut(info.Name, new ColumnValue((byte[])propertyValue));
                    }
#endregion
                }
                else
                {
#region 主键字段处理
                    var propertyValue = info.GetValue(obj, null);
                    if (propertyValue == null)
                    {
                        throw new OTSException("PrimaryKey can not null");
                    }
                    else if (info.PropertyType == typeof(int))
                    {
                        keys.Add(info.Name, new ColumnValue((int)propertyValue));
                    }
                    else if (info.PropertyType == typeof(string))
                    {
                        keys.Add(info.Name, new ColumnValue((string)propertyValue));
                    }
                    else if (info.PropertyType == typeof(byte[]))
                    {
                        keys.Add(info.Name, new ColumnValue((byte[])propertyValue));
                    }
#endregion
                }
            }

            var req = new UpdateRowRequest(name, new Condition(RowExistenceExpectation.EXPECT_EXIST), keys, columns);
            OtsClient.UpdateRow(req);
        }

        /// <summary>
        /// 定义行的主键，必须与创建表时的TableMeta中定义的一致
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pk"></param>
        /// <returns></returns>
        public T GetRow<T>(PrimaryKey pk)
        {
            var type = typeof(T);
            var name = tableNames.ContainsKey(type) ? tableNames[type] : SetTable(type);
            var res = OtsClient.GetRow(new GetRowRequest(name, pk));
            return Serialization.ConvertTo<T>(res);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetRow<T>(String key)
        {
            var type = typeof(T);
            var obj = System.Activator.CreateInstance<T>();
            var name = tableNames.ContainsKey(type) ? tableNames[type] : SetTable(type);
            var pkSchema = tableKey[name];

            // 定义行的主键，必须与创建表时的TableMeta中定义的一致
            var pk = new PrimaryKey();
            tableKey[name].ForEach(item =>
            {
                if (item.Item2 == ColumnValueType.String)
                {
                    pk.Add(item.Item1, new ColumnValue(key));
                    return;
                }
            });
            var res = OtsClient.GetRow(new GetRowRequest(name, pk));
            return Serialization.ConvertTo<T>(res);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="sort"></param>
        /// <returns></returns>
        public T Find<T>(IQuery query, Sort sort = null)
        {
            try
            {
                var type = typeof(T);
                var name = tableNames.ContainsKey(type) ? tableNames[type] : SetTable(type);
                var search = new SearchQuery { Limit = 1, Query = query };
                if (sort != null)
                {
                    search.Sort = sort;
                }
                var req = new SearchRequest(name, name, search) { ColumnsToGet = new ColumnsToGet { ReturnAll = true } };
                var res = OtsClient.Search(req);
                if (res != null && res.Rows.Count > 0)
                {
                    var obj = Serialization.ConvertTo<T>(res.Rows[0]);
                    return obj;
                }
            }
            catch (EmptyPrimaryKeyException)
            {
                return default(T);
            }
            catch (Exception)
            {
                // ignored
            }

            return default(T);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fieldName"></param>
        /// <param name="term"></param>
        /// <param name="sort"></param>
        /// <returns></returns>
        public T FindByTerm<T>(string fieldName, ColumnValue term, Sort sort = null)
        {
            return Find<T>(new TermQuery(fieldName, term), sort);
        }

    }
}