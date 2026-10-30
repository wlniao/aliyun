/*-----------------------Copyright 2017 www.wlniao.com---------------------------
    文件名称：Wlniao\Aliyun\TableStore\DataModel.cs
    适用环境：NETCoreCLR 1.0+
    功能描述：TableStore 数据模型

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
using System.Text;

namespace Wlniao.Aliyun.TableStore
{
    /// <summary>
    /// 列值类型
    /// </summary>
    public enum ColumnValueType
    {
        /// <summary>整型</summary>
        Integer = 1,
        /// <summary>字符串</summary>
        String,
        /// <summary>二进制</summary>
        Binary,
        /// <summary>布尔</summary>
        Boolean,
        /// <summary>浮点</summary>
        Double
    }

    /// <summary>
    /// 行存在性预期
    /// </summary>
    public enum RowExistenceExpectation
    {
        /// <summary>忽略行是否存在</summary>
        IGNORE,
        /// <summary>要求行存在</summary>
        EXPECT_EXIST,
        /// <summary>要求行不存在</summary>
        EXPECT_NOT_EXIST
    }

    /// <summary>
    /// 主键选项
    /// </summary>
    public enum PrimaryKeyOption
    {
        /// <summary>无</summary>
        NONE,
        /// <summary>自增主键</summary>
        AUTO_INCREMENT
    }

    /// <summary>
    /// 列值
    /// </summary>
    public class ColumnValue
    {
        /// <summary>负无穷主键占位</summary>
        public static readonly ColumnValue INF_MIN = new ColumnValue("INF_MIN");
        /// <summary>正无穷主键占位</summary>
        public static readonly ColumnValue INF_MAX = new ColumnValue("INF_MAX");
        /// <summary>自增主键占位</summary>
        public static readonly ColumnValue AUTO_INCREMENT = new ColumnValue("AUTO_INCREMENT");

        /// <summary>
        /// 
        /// </summary>
        public ColumnValueType Type { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public long IntegerValue { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public string StringValue { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public bool BooleanValue { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public double DoubleValue { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public byte[] BinaryValue { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public ColumnValue(long value)
        {
            Type = ColumnValueType.Integer;
            IntegerValue = value;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public ColumnValue(ulong value)
        {
            Type = ColumnValueType.Integer;
            IntegerValue = (long)value;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public ColumnValue(string value)
        {
            Type = ColumnValueType.String;
            StringValue = value;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public ColumnValue(bool value)
        {
            Type = ColumnValueType.Boolean;
            BooleanValue = value;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public ColumnValue(double value)
        {
            Type = ColumnValueType.Double;
            DoubleValue = value;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public ColumnValue(byte[] value)
        {
            Type = ColumnValueType.Binary;
            BinaryValue = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] AsStringInBytes()
        {
            return Encoding.UTF8.GetBytes(StringValue);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsInfMin()
        {
            return StringValue == "INF_MIN";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsInfMax()
        {
            return StringValue == "INF_MAX";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsPlaceHolderForAutoIncr()
        {
            return StringValue == "AUTO_INCREMENT";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            switch (Type)
            {
                case ColumnValueType.Integer:
                    return IntegerValue.ToString();
                case ColumnValueType.String:
                    return StringValue;
                case ColumnValueType.Boolean:
                    return BooleanValue ? "true" : "false";
                case ColumnValueType.Double:
                    return DoubleValue.ToString();
                default:
                    return System.Convert.ToBase64String(BinaryValue);
            }
        }
    }

    /// <summary>
    /// 行主键
    /// </summary>
    public class PrimaryKey : Dictionary<string, ColumnValue>
    {
    }

    /// <summary>
    /// 行属性列
    /// </summary>
    public class AttributeColumns : Dictionary<string, ColumnValue>
    {
    }

    /// <summary>
    /// 数据行
    /// </summary>
    public class Row
    {
        /// <summary>
        /// 
        /// </summary>
        public PrimaryKey PrimaryKey { get; set; } = new PrimaryKey();
        /// <summary>
        /// 
        /// </summary>
        public AttributeColumns AttributeColumns { get; set; } = new AttributeColumns();
    }

    /// <summary>
    /// 行写入条件
    /// </summary>
    public class Condition
    {
        /// <summary>
        /// 
        /// </summary>
        public Condition()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rowExist"></param>
        public Condition(RowExistenceExpectation rowExist)
        {
            RowExistenceExpect = rowExist;
        }
        /// <summary>
        /// 
        /// </summary>
        public RowExistenceExpectation RowExistenceExpect { get; set; }
    }

    /// <summary>
    /// 行更新内容
    /// </summary>
    public class UpdateOfAttribute
    {
        /// <summary>
        /// 需删除的列名
        /// </summary>
        public IList<string> AttributeColumnsToDelete { get; set; } = new List<string>();
        /// <summary>
        /// 需写入的列
        /// </summary>
        public IDictionary<string, ColumnValue> AttributeColumnsToPut { get; set; } = new Dictionary<string, ColumnValue>();

        /// <summary>
        /// 删除指定列的全部版本
        /// </summary>
        /// <param name="columnName"></param>
        public void AddAttributeColumnToDelete(string columnName)
        {
            AttributeColumnsToDelete.Add(columnName);
        }
        /// <summary>
        /// 写入指定列
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="columnValue"></param>
        public void AddAttributeColumnToPut(string columnName, ColumnValue columnValue)
        {
            AttributeColumnsToPut.Add(columnName, columnValue);
        }
    }

    /// <summary>
    /// 表主键结构
    /// </summary>
    public class PrimaryKeySchema : List<Tuple<string, ColumnValueType, PrimaryKeyOption>>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="primaryKeyColumnName"></param>
        /// <param name="columnValueType"></param>
        public void Add(string primaryKeyColumnName, ColumnValueType columnValueType)
        {
            Add(Tuple.Create(primaryKeyColumnName, columnValueType, PrimaryKeyOption.NONE));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="primaryKeyColumnName"></param>
        /// <param name="columnValueType"></param>
        /// <param name="primaryKeyOption"></param>
        public void Add(string primaryKeyColumnName, ColumnValueType columnValueType, PrimaryKeyOption primaryKeyOption)
        {
            Add(Tuple.Create(primaryKeyColumnName, columnValueType, primaryKeyOption));
        }
    }

    /// <summary>
    /// 表结构
    /// </summary>
    public class TableMeta
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="primaryKeySchema"></param>
        public TableMeta(string tableName, PrimaryKeySchema primaryKeySchema)
        {
            TableName = tableName;
            PrimaryKeySchema = primaryKeySchema;
        }
        /// <summary>
        /// 
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public PrimaryKeySchema PrimaryKeySchema { get; set; }
    }

    /// <summary>
    /// 预留读写吞吐量
    /// </summary>
    public class CapacityUnit
    {
        /// <summary>
        /// 
        /// </summary>
        public CapacityUnit()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="read"></param>
        /// <param name="write"></param>
        public CapacityUnit(int? read, int? write)
        {
            Read = read;
            Write = write;
        }
        /// <summary>
        /// 
        /// </summary>
        public int? Read { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int? Write { get; set; }
    }
}
