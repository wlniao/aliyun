/*-----------------------Copyright 2017 www.wlniao.com---------------------------
    文件名称：Wlniao\Aliyun\TableStore\Protocol.cs
    适用环境：NETCoreCLR 1.0+
    功能描述：TableStore 接口的 protobuf 编解码

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
using System.Collections.Generic;

namespace Wlniao.Aliyun.TableStore
{
    /// <summary>
    /// TableStore 接口的 protobuf 编解码
    /// </summary>
    internal static class Protocol
    {
        private const int PK_TYPE_INTEGER = 1;
        private const int PK_TYPE_STRING = 2;
        private const int PK_TYPE_BINARY = 3;

        private const int COLUMN_RETURN_ALL = 1;
        private const int COLUMN_RETURN_SPECIFIED = 2;

        #region 请求编码
        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static byte[] EncodeCreateTable(CreateTableRequest request)
        {
            var w = new ProtoWriter();
            w.WriteMessage(1, EncodeTableMeta(request.TableMeta));
            if (request.ReservedThroughput != null)
            {
                var cu = new ProtoWriter();
                if (request.ReservedThroughput.Read.HasValue)
                {
                    cu.WriteInt32(1, request.ReservedThroughput.Read.Value);
                }
                if (request.ReservedThroughput.Write.HasValue)
                {
                    cu.WriteInt32(2, request.ReservedThroughput.Write.Value);
                }
                var rt = new ProtoWriter();
                rt.WriteMessage(1, cu);
                w.WriteMessage(2, rt);
            }
            // 与官方 SDK 保持一致：默认数据永不过期、保留 1 个版本
            var options = new ProtoWriter();
            options.WriteInt32(1, -1);
            options.WriteInt32(2, 1);
            w.WriteMessage(3, options);
            return w.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static byte[] EncodeDescribeTable(string tableName)
        {
            var w = new ProtoWriter();
            w.WriteString(1, tableName);
            return w.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static byte[] EncodeDeleteTable(string tableName)
        {
            var w = new ProtoWriter();
            w.WriteString(1, tableName);
            return w.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static byte[] EncodeCreateSearchIndex(CreateSearchIndexRequest request)
        {
            var w = new ProtoWriter();
            w.WriteString(1, request.TableName);
            w.WriteString(2, request.IndexName);
            w.WriteMessage(3, EncodeIndexSchema(request.IndexSchema));
            return w.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static byte[] EncodePutRow(PutRowRequest request)
        {
            var cells = new List<PlainBufferCell>();
            foreach (var kv in request.AttributeColumns)
            {
                cells.Add(new PlainBufferCell { Name = kv.Key, Value = kv.Value });
            }
            var w = new ProtoWriter();
            w.WriteString(1, request.TableName);
            w.WriteBytes(2, PlainBuffer.WriteRow(request.PrimaryKey, cells, false));
            w.WriteMessage(3, EncodeCondition(request.Condition));
            return w.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static byte[] EncodeUpdateRow(UpdateRowRequest request)
        {
            var cells = new List<PlainBufferCell>();
            foreach (var name in request.UpdateOfAttribute.AttributeColumnsToDelete)
            {
                cells.Add(new PlainBufferCell { Name = name, CellType = PlainBuffer.DELETE_ALL_VERSION });
            }
            foreach (var kv in request.UpdateOfAttribute.AttributeColumnsToPut)
            {
                cells.Add(new PlainBufferCell { Name = kv.Key, Value = kv.Value });
            }
            var w = new ProtoWriter();
            w.WriteString(1, request.TableName);
            w.WriteBytes(2, PlainBuffer.WriteRow(request.PrimaryKey, cells, false));
            w.WriteMessage(3, EncodeCondition(request.Condition));
            return w.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static byte[] EncodeGetRow(GetRowRequest request)
        {
            var w = new ProtoWriter();
            w.WriteString(1, request.TableName);
            w.WriteBytes(2, PlainBuffer.WritePrimaryKey(request.PrimaryKey));
            w.WriteInt32(5, 1);
            return w.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static byte[] EncodeSearch(SearchRequest request)
        {
            var w = new ProtoWriter();
            w.WriteString(1, request.TableName);
            w.WriteString(2, request.IndexName);
            if (request.ColumnsToGet != null)
            {
                var cols = new ProtoWriter();
                if (request.ColumnsToGet.ReturnAll)
                {
                    cols.WriteInt32(1, COLUMN_RETURN_ALL);
                }
                else if (request.ColumnsToGet.Columns.Count > 0)
                {
                    cols.WriteInt32(1, COLUMN_RETURN_SPECIFIED);
                    foreach (var name in request.ColumnsToGet.Columns)
                    {
                        cols.WriteString(2, name);
                    }
                }
                else
                {
                    cols.WriteInt32(1, 3);
                }
                w.WriteMessage(3, cols);
            }
            w.WriteMessage(4, EncodeSearchQuery(request.SearchQuery));
            return w.ToArray();
        }

        private static ProtoWriter EncodeSearchQuery(SearchQuery query)
        {
            var w = new ProtoWriter();
            if (query.Offset.HasValue)
            {
                w.WriteInt32(1, query.Offset.Value);
            }
            if (query.Limit.HasValue)
            {
                w.WriteInt32(2, query.Limit.Value);
            }
            if (query.Query != null)
            {
                w.WriteMessage(4, SearchQueryCodec.WrapQuery(query.Query));
            }
            if (query.Collapse != null)
            {
                var collapse = new ProtoWriter();
                collapse.WriteString(1, query.Collapse.FieldName);
                w.WriteMessage(5, collapse);
            }
            if (query.Sort != null)
            {
                var sort = new ProtoWriter();
                foreach (var sorter in query.Sort.Sorters)
                {
                    sort.WriteBytes(1, sorter.Serialize());
                }
                w.WriteMessage(6, sort);
            }
            w.WriteBool(8, query.GetTotalCount);
            if (query.Token != null)
            {
                w.WriteBytes(9, query.Token);
            }
            return w;
        }

        private static ProtoWriter EncodeCondition(Condition condition)
        {
            var w = new ProtoWriter();
            w.WriteInt32(1, (int)condition.RowExistenceExpect);
            return w;
        }

        private static ProtoWriter EncodeTableMeta(TableMeta tableMeta)
        {
            var w = new ProtoWriter();
            w.WriteString(1, tableMeta.TableName);
            foreach (var item in tableMeta.PrimaryKeySchema)
            {
                var pk = new ProtoWriter();
                pk.WriteString(1, item.Item1);
                pk.WriteInt32(2, ToPrimaryKeyType(item.Item2));
                if (item.Item3 != PrimaryKeyOption.NONE)
                {
                    pk.WriteInt32(3, (int)item.Item3);
                }
                w.WriteMessage(2, pk);
            }
            return w;
        }

        private static ProtoWriter EncodeIndexSchema(IndexSchema schema)
        {
            var w = new ProtoWriter();
            if (schema == null)
            {
                w.WriteMessage(2, new ProtoWriter());
                return w;
            }
            if (schema.FieldSchemas != null)
            {
                foreach (var field in schema.FieldSchemas)
                {
                    var fs = new ProtoWriter();
                    fs.WriteString(1, field.FieldName);
                    fs.WriteInt32(2, (int)field.FieldType);
                    fs.WriteInt32(3, (int)field.IndexOptions);
                    if (!string.IsNullOrEmpty(field.Analyzer))
                    {
                        fs.WriteString(4, field.Analyzer);
                    }
                    fs.WriteBool(5, field.Index);
                    fs.WriteBool(6, field.EnableSortAndAgg);
                    fs.WriteBool(7, field.Store);
                    fs.WriteBool(9, field.IsArray);
                    w.WriteMessage(1, fs);
                }
            }
            var setting = new ProtoWriter();
            var indexSetting = schema.IndexSetting ?? new IndexSetting();
            if (indexSetting.NumberOfShards.HasValue)
            {
                setting.WriteInt32(1, indexSetting.NumberOfShards.Value);
            }
            foreach (var routingField in indexSetting.RoutingFields)
            {
                setting.WriteString(2, routingField);
            }
            if (indexSetting.RoutingPartitionSize.HasValue)
            {
                setting.WriteInt32(3, indexSetting.RoutingPartitionSize.Value);
            }
            w.WriteMessage(2, setting);
            return w;
        }

        private static int ToPrimaryKeyType(ColumnValueType type)
        {
            switch (type)
            {
                case ColumnValueType.Integer:
                    return PK_TYPE_INTEGER;
                case ColumnValueType.String:
                    return PK_TYPE_STRING;
                case ColumnValueType.Binary:
                    return PK_TYPE_BINARY;
                default:
                    throw new OTSException("Unsupported primary key type: " + type);
            }
        }

        private static ColumnValueType ToColumnValueType(int type)
        {
            switch (type)
            {
                case PK_TYPE_INTEGER:
                    return ColumnValueType.Integer;
                case PK_TYPE_STRING:
                    return ColumnValueType.String;
                case PK_TYPE_BINARY:
                    return ColumnValueType.Binary;
                default:
                    throw new OTSException("Unsupported primary key type: " + type);
            }
        }
        #endregion

        #region 响应解码
        /// <summary>
        /// 
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public static ListTableResponse DecodeListTable(byte[] body)
        {
            var res = new ListTableResponse();
            var r = new ProtoReader(body);
            while (r.Next())
            {
                if (r.Field == 1)
                {
                    res.TableNames.Add(r.ReadString());
                }
                else
                {
                    r.Skip();
                }
            }
            return res;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public static DescribeTableResponse DecodeDescribeTable(byte[] body)
        {
            var res = new DescribeTableResponse();
            var r = new ProtoReader(body);
            while (r.Next())
            {
                if (r.Field == 1)
                {
                    res.TableMeta = DecodeTableMeta(r.ReadMessage());
                }
                else
                {
                    r.Skip();
                }
            }
            return res;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public static GetRowResponse DecodeGetRow(byte[] body)
        {
            byte[] row = null;
            var r = new ProtoReader(body);
            while (r.Next())
            {
                if (r.Field == 2)
                {
                    row = r.ReadBytes();
                }
                else
                {
                    r.Skip();
                }
            }
            if (row == null || row.Length == 0)
            {
                return new GetRowResponse();
            }
            var parsed = PlainBuffer.ReadRow(row);
            return new GetRowResponse { PrimaryKey = parsed.PrimaryKey, Attribute = parsed.AttributeColumns };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public static SearchResponse DecodeSearch(byte[] body)
        {
            var res = new SearchResponse();
            var r = new ProtoReader(body);
            while (r.Next())
            {
                switch (r.Field)
                {
                    case 1:
                        res.TotalCount = r.ReadInt64();
                        break;
                    case 2:
                        res.Rows.Add(PlainBuffer.ReadRow(r.ReadBytes()));
                        break;
                    case 3:
                        res.IsAllSuccess = r.ReadBool();
                        break;
                    case 6:
                        res.NextToken = r.ReadBytes();
                        break;
                    default:
                        r.Skip();
                        break;
                }
            }
            return res;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public static void DecodeError(byte[] body, out string code, out string message)
        {
            code = null;
            message = null;
            var r = new ProtoReader(body);
            while (r.Next())
            {
                if (r.Field == 1)
                {
                    code = r.ReadString();
                }
                else if (r.Field == 2)
                {
                    message = r.ReadString();
                }
                else
                {
                    r.Skip();
                }
            }
        }

        private static TableMeta DecodeTableMeta(ProtoReader r)
        {
            string name = null;
            var schema = new PrimaryKeySchema();
            while (r.Next())
            {
                if (r.Field == 1)
                {
                    name = r.ReadString();
                }
                else if (r.Field == 2)
                {
                    var pk = r.ReadMessage();
                    string pkName = null;
                    var pkType = ColumnValueType.String;
                    var pkOption = PrimaryKeyOption.NONE;
                    while (pk.Next())
                    {
                        switch (pk.Field)
                        {
                            case 1:
                                pkName = pk.ReadString();
                                break;
                            case 2:
                                pkType = ToColumnValueType(pk.ReadInt32());
                                break;
                            case 3:
                                pkOption = (PrimaryKeyOption)pk.ReadInt32();
                                break;
                            default:
                                pk.Skip();
                                break;
                        }
                    }
                    schema.Add(pkName, pkType, pkOption);
                }
                else
                {
                    r.Skip();
                }
            }
            return new TableMeta(name, schema);
        }
        #endregion
    }
}
