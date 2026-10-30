/*-----------------------Copyright 2017 www.wlniao.com---------------------------
    文件名称：Wlniao\Aliyun\TableStore\SearchModel.cs
    适用环境：NETCoreCLR 1.0+
    功能描述：TableStore 搜索模型（SearchIndex 查询/排序/索引结构）

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

namespace Wlniao.Aliyun.TableStore
{
    /// <summary>
    /// 查询类型
    /// </summary>
    public enum QueryType
    {
        /// <summary>分词匹配</summary>
        MatchQuery = 1,
        /// <summary>短语匹配</summary>
        MatchPhraseQuery = 2,
        /// <summary>精确匹配</summary>
        TermQuery = 3,
        /// <summary>范围匹配</summary>
        RangeQuery = 4,
        /// <summary>前缀匹配</summary>
        PrefixQuery = 5,
        /// <summary>布尔组合</summary>
        BoolQuery = 6,
        /// <summary>常量打分</summary>
        ConstScoreQuery = 7,
        /// <summary>嵌套查询</summary>
        NestedQuery = 9,
        /// <summary>通配符匹配</summary>
        WildcardQuery = 10,
        /// <summary>匹配全部</summary>
        MatchAllQuery = 11,
        /// <summary>多值精确匹配</summary>
        TermsQuery = 15
    }

    /// <summary>
    /// 排序顺序
    /// </summary>
    public enum SortOrder
    {
        /// <summary>升序</summary>
        ASC,
        /// <summary>降序</summary>
        DESC
    }

    /// <summary>
    /// 多值字段排序模式
    /// </summary>
    public enum SortMode
    {
        /// <summary>取最小值</summary>
        MIN,
        /// <summary>取最大值</summary>
        MAX,
        /// <summary>取平均值</summary>
        AVG
    }

    /// <summary>
    /// 嵌套查询打分模式
    /// </summary>
    public enum ScoreMode
    {
        /// <summary>不打分</summary>
        None = 1,
        /// <summary>取平均</summary>
        Avg = 2,
        /// <summary>取最大</summary>
        Max = 3,
        /// <summary>取总和</summary>
        Total = 4,
        /// <summary>取最小</summary>
        Min = 5
    }

    /// <summary>
    /// 匹配查询逻辑运算符
    /// </summary>
    public enum QueryOperator
    {
        /// <summary>或</summary>
        OR = 1,
        /// <summary>与</summary>
        AND = 2
    }

    /// <summary>
    /// 索引字段类型
    /// </summary>
    public enum FieldType
    {
        /// <summary>整型</summary>
        LONG = 1,
        /// <summary>浮点</summary>
        DOUBLE = 2,
        /// <summary>布尔</summary>
        BOOLEAN = 3,
        /// <summary>关键字</summary>
        KEYWORD = 4,
        /// <summary>全文文本</summary>
        TEXT = 5,
        /// <summary>嵌套类型</summary>
        NESTED = 6,
        /// <summary>地理位置</summary>
        GEO_POINT = 7
    }

    /// <summary>
    /// 索引选项
    /// </summary>
    public enum IndexOptions
    {
        /// <summary>仅文档</summary>
        DOCS = 1,
        /// <summary>文档与词频</summary>
        FREQS = 2,
        /// <summary>文档、词频与位置</summary>
        POSITIONS = 3,
        /// <summary>文档、词频、位置与偏移</summary>
        OFFSETS = 4
    }

    /// <summary>
    /// 查询条件
    /// </summary>
    public interface IQuery
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        QueryType GetQueryType();
        /// <summary>
        /// 编码为 QueryBody 的 protobuf 字节
        /// </summary>
        /// <returns></returns>
        byte[] Serialize();
    }

    /// <summary>
    /// 匹配全部
    /// </summary>
    public class MatchAllQuery : IQuery
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public QueryType GetQueryType()
        {
            return QueryType.MatchAllQuery;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            return new byte[0];
        }
    }

    /// <summary>
    /// 精确匹配
    /// </summary>
    public class TermQuery : IQuery
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="term"></param>
        public TermQuery(string fieldName, ColumnValue term)
        {
            FieldName = fieldName;
            Term = term;
        }
        /// <summary>
        /// 
        /// </summary>
        public string FieldName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ColumnValue Term { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public QueryType GetQueryType()
        {
            return QueryType.TermQuery;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            var w = new ProtoWriter();
            w.WriteString(1, FieldName);
            w.WriteBytes(2, SearchVariant.ToVariant(Term));
            return w.ToArray();
        }
    }

    /// <summary>
    /// 多值精确匹配
    /// </summary>
    public class TermsQuery : IQuery
    {
        /// <summary>
        /// 
        /// </summary>
        public string FieldName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<ColumnValue> Terms { get; set; } = new List<ColumnValue>();
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public QueryType GetQueryType()
        {
            return QueryType.TermsQuery;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            var w = new ProtoWriter();
            w.WriteString(1, FieldName);
            foreach (var term in Terms)
            {
                w.WriteBytes(2, SearchVariant.ToVariant(term));
            }
            return w.ToArray();
        }
    }

    /// <summary>
    /// 范围匹配
    /// </summary>
    public class RangeQuery : IQuery
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public RangeQuery(string fieldName, ColumnValue from, ColumnValue to)
        {
            FieldName = fieldName;
            From = from;
            To = to;
        }
        /// <summary>
        /// 
        /// </summary>
        public string FieldName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ColumnValue From { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ColumnValue To { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool IncludeLower { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool IncludeUpper { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public QueryType GetQueryType()
        {
            return QueryType.RangeQuery;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            var w = new ProtoWriter();
            w.WriteString(1, FieldName);
            if (From != null)
            {
                w.WriteBytes(2, SearchVariant.ToVariant(From));
            }
            if (To != null)
            {
                w.WriteBytes(3, SearchVariant.ToVariant(To));
            }
            w.WriteBool(4, IncludeLower);
            w.WriteBool(5, IncludeUpper);
            return w.ToArray();
        }
    }

    /// <summary>
    /// 前缀匹配
    /// </summary>
    public class PrefixQuery : IQuery
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="prefix"></param>
        public PrefixQuery(string fieldName, string prefix)
        {
            FieldName = fieldName;
            Prefix = prefix;
        }
        /// <summary>
        /// 
        /// </summary>
        public string FieldName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Prefix { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public QueryType GetQueryType()
        {
            return QueryType.PrefixQuery;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            var w = new ProtoWriter();
            w.WriteString(1, FieldName);
            w.WriteString(2, Prefix);
            return w.ToArray();
        }
    }

    /// <summary>
    /// 通配符匹配
    /// </summary>
    public class WildcardQuery : IQuery
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        public WildcardQuery(string fieldName, string value)
        {
            FieldName = fieldName;
            Value = value;
        }
        /// <summary>
        /// 
        /// </summary>
        public string FieldName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public QueryType GetQueryType()
        {
            return QueryType.WildcardQuery;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            var w = new ProtoWriter();
            w.WriteString(1, FieldName);
            w.WriteString(2, Value);
            return w.ToArray();
        }
    }

    /// <summary>
    /// 分词匹配
    /// </summary>
    public class MatchQuery : IQuery
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="text"></param>
        public MatchQuery(string fieldName, string text)
        {
            FieldName = fieldName;
            Text = text;
        }
        /// <summary>
        /// 
        /// </summary>
        public string FieldName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int? MinimumShouldMatch { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public QueryOperator Operator { get; set; } = QueryOperator.OR;
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public QueryType GetQueryType()
        {
            return QueryType.MatchQuery;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            var w = new ProtoWriter();
            w.WriteString(1, FieldName);
            w.WriteString(2, Text);
            if (MinimumShouldMatch.HasValue)
            {
                w.WriteInt32(3, MinimumShouldMatch.Value);
            }
            w.WriteInt32(4, (int)Operator);
            return w.ToArray();
        }
    }

    /// <summary>
    /// 短语匹配
    /// </summary>
    public class MatchPhraseQuery : IQuery
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="text"></param>
        public MatchPhraseQuery(string fieldName, string text)
        {
            FieldName = fieldName;
            Text = text;
        }
        /// <summary>
        /// 
        /// </summary>
        public string FieldName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public QueryType GetQueryType()
        {
            return QueryType.MatchPhraseQuery;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            var w = new ProtoWriter();
            w.WriteString(1, FieldName);
            w.WriteString(2, Text);
            return w.ToArray();
        }
    }

    /// <summary>
    /// 布尔组合查询
    /// </summary>
    public class BoolQuery : IQuery
    {
        /// <summary>
        /// 
        /// </summary>
        public List<IQuery> MustQueries { get; set; } = new List<IQuery>();
        /// <summary>
        /// 
        /// </summary>
        public List<IQuery> MustNotQueries { get; set; } = new List<IQuery>();
        /// <summary>
        /// 
        /// </summary>
        public List<IQuery> FilterQueries { get; set; } = new List<IQuery>();
        /// <summary>
        /// 
        /// </summary>
        public List<IQuery> ShouldQueries { get; set; } = new List<IQuery>();
        /// <summary>
        /// 
        /// </summary>
        public int? MinimumShouldMatch { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public QueryType GetQueryType()
        {
            return QueryType.BoolQuery;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            var w = new ProtoWriter();
            foreach (var q in MustQueries)
            {
                w.WriteMessage(1, SearchQueryCodec.WrapQuery(q));
            }
            foreach (var q in MustNotQueries)
            {
                w.WriteMessage(2, SearchQueryCodec.WrapQuery(q));
            }
            foreach (var q in FilterQueries)
            {
                w.WriteMessage(3, SearchQueryCodec.WrapQuery(q));
            }
            foreach (var q in ShouldQueries)
            {
                w.WriteMessage(4, SearchQueryCodec.WrapQuery(q));
            }
            if (MinimumShouldMatch.HasValue)
            {
                w.WriteInt32(5, MinimumShouldMatch.Value);
            }
            return w.ToArray();
        }
    }

    /// <summary>
    /// 常量打分查询
    /// </summary>
    public class ConstScoreQuery : IQuery
    {
        /// <summary>
        /// 
        /// </summary>
        public IQuery Filter { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public QueryType GetQueryType()
        {
            return QueryType.ConstScoreQuery;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            var w = new ProtoWriter();
            if (Filter != null)
            {
                w.WriteMessage(1, SearchQueryCodec.WrapQuery(Filter));
            }
            return w.ToArray();
        }
    }

    /// <summary>
    /// 嵌套查询
    /// </summary>
    public class NestedQuery : IQuery
    {
        /// <summary>
        /// 
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public IQuery Query { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ScoreMode ScoreMode { get; set; } = ScoreMode.None;
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public QueryType GetQueryType()
        {
            return QueryType.NestedQuery;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            var w = new ProtoWriter();
            w.WriteString(1, Path);
            if (Query != null)
            {
                w.WriteMessage(2, SearchQueryCodec.WrapQuery(Query));
            }
            w.WriteInt32(3, (int)ScoreMode);
            return w.ToArray();
        }
    }

    /// <summary>
    /// 排序项
    /// </summary>
    public interface ISorter
    {
        /// <summary>
        /// 编码为 Sorter 的 protobuf 字节
        /// </summary>
        /// <returns></returns>
        byte[] Serialize();
    }

    /// <summary>
    /// 排序
    /// </summary>
    public class Sort
    {
        /// <summary>
        /// 
        /// </summary>
        public Sort()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sorters"></param>
        public Sort(List<ISorter> sorters)
        {
            Sorters = sorters;
        }
        /// <summary>
        /// 
        /// </summary>
        public List<ISorter> Sorters { get; set; } = new List<ISorter>();
    }

    /// <summary>
    /// 按相关性得分排序
    /// </summary>
    public class ScoreSort : ISorter
    {
        /// <summary>
        /// 
        /// </summary>
        public SortOrder Order { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            var body = new ProtoWriter();
            body.WriteInt32(1, (int)Order);
            var w = new ProtoWriter();
            w.WriteMessage(3, body);
            return w.ToArray();
        }
    }

    /// <summary>
    /// 按主键排序
    /// </summary>
    public class PrimaryKeySort : ISorter
    {
        /// <summary>
        /// 
        /// </summary>
        public PrimaryKeySort()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="order"></param>
        public PrimaryKeySort(SortOrder order)
        {
            Order = order;
        }
        /// <summary>
        /// 
        /// </summary>
        public SortOrder Order { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            var body = new ProtoWriter();
            body.WriteInt32(1, (int)Order);
            var w = new ProtoWriter();
            w.WriteMessage(4, body);
            return w.ToArray();
        }
    }

    /// <summary>
    /// 按字段排序
    /// </summary>
    public class FieldSort : ISorter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="order"></param>
        public FieldSort(string fieldName, SortOrder order)
        {
            FieldName = fieldName;
            Order = order;
        }
        /// <summary>
        /// 
        /// </summary>
        public string FieldName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public SortOrder Order { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public SortMode Mode { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            var body = new ProtoWriter();
            body.WriteString(1, FieldName);
            body.WriteInt32(2, (int)Order);
            body.WriteInt32(3, (int)Mode);
            var w = new ProtoWriter();
            w.WriteMessage(1, body);
            return w.ToArray();
        }
    }

    /// <summary>
    /// 查询参数
    /// </summary>
    public class SearchQuery
    {
        /// <summary>
        /// 
        /// </summary>
        public int? Offset { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int? Limit { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public IQuery Query { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Collapse Collapse { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Sort Sort { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool GetTotalCount { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public byte[] Token { get; set; }
    }

    /// <summary>
    /// 折叠配置
    /// </summary>
    public class Collapse
    {
        /// <summary>
        /// 
        /// </summary>
        public string FieldName { get; set; }
    }

    /// <summary>
    /// 返回列配置
    /// </summary>
    public class ColumnsToGet
    {
        /// <summary>
        /// 
        /// </summary>
        public List<string> Columns { get; set; } = new List<string>();
        /// <summary>
        /// 
        /// </summary>
        public bool ReturnAll { get; set; }
    }

    /// <summary>
    /// 索引字段结构
    /// </summary>
    public class FieldSchema
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="fieldType"></param>
        public FieldSchema(string fieldName, FieldType fieldType)
        {
            FieldName = fieldName;
            FieldType = fieldType;
        }
        /// <summary>
        /// 
        /// </summary>
        public string FieldName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public FieldType FieldType { get; set; }
        /// <summary>
        /// 是否开启索引
        /// </summary>
        public bool Index { get; set; }
        /// <summary>
        /// 是否开启排序与统计
        /// </summary>
        public bool EnableSortAndAgg { get; set; }
        /// <summary>
        /// 是否存储原始值
        /// </summary>
        public bool Store { get; set; }
        /// <summary>
        /// 是否为数组
        /// </summary>
        public bool IsArray { get; set; }
        /// <summary>
        /// 分词器
        /// </summary>
        public string Analyzer { get; set; }
        /// <summary>
        /// 索引选项
        /// </summary>
        public IndexOptions IndexOptions { get; set; } = IndexOptions.DOCS;
    }

    /// <summary>
    /// 索引结构
    /// </summary>
    public class IndexSchema
    {
        /// <summary>
        /// 
        /// </summary>
        public List<FieldSchema> FieldSchemas { get; set; } = new List<FieldSchema>();
        /// <summary>
        /// 
        /// </summary>
        public IndexSetting IndexSetting { get; set; } = new IndexSetting();
    }

    /// <summary>
    /// 索引配置
    /// </summary>
    public class IndexSetting
    {
        /// <summary>
        /// 路由字段
        /// </summary>
        public List<string> RoutingFields { get; set; } = new List<string>();
        /// <summary>
        /// 分片数
        /// </summary>
        public int? NumberOfShards { get; set; }
        /// <summary>
        /// 路由分区大小
        /// </summary>
        public int? RoutingPartitionSize { get; set; }
    }

    /// <summary>
    /// 搜索值编码
    /// </summary>
    internal static class SearchVariant
    {
        private const byte VT_INTEGER = 0;
        private const byte VT_DOUBLE = 1;
        private const byte VT_BOOLEAN = 2;
        private const byte VT_STRING = 3;

        /// <summary>
        /// 编码为搜索值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] ToVariant(ColumnValue value)
        {
            switch (value.Type)
            {
                case ColumnValueType.Integer:
                    return WithPayload(VT_INTEGER, Int64Le(value.IntegerValue));
                case ColumnValueType.Double:
                    return WithPayload(VT_DOUBLE, Int64Le(BitConverter.DoubleToInt64Bits(value.DoubleValue)));
                case ColumnValueType.Boolean:
                    return WithPayload(VT_BOOLEAN, new byte[] { (byte)(value.BooleanValue ? 1 : 0) });
                case ColumnValueType.String:
                    return WithPayload(VT_STRING, Int32Le(value.AsStringInBytes().Length), value.AsStringInBytes());
                default:
                    throw new OTSException("Unsupported search value type: " + value.Type);
            }
        }

        private static byte[] WithPayload(byte type, params byte[][] parts)
        {
            var len = 1;
            foreach (var part in parts)
            {
                len += part.Length;
            }
            var result = new byte[len];
            result[0] = type;
            var pos = 1;
            foreach (var part in parts)
            {
                Array.Copy(part, 0, result, pos, part.Length);
                pos += part.Length;
            }
            return result;
        }

        private static byte[] Int64Le(long value)
        {
            var bytes = new byte[8];
            for (var i = 0; i < 8; i++)
            {
                bytes[i] = (byte)(value >> (i * 8));
            }
            return bytes;
        }

        private static byte[] Int32Le(int value)
        {
            return new byte[] { (byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24) };
        }
    }

    /// <summary>
    /// Query 包装编码
    /// </summary>
    internal static class SearchQueryCodec
    {
        /// <summary>
        /// 编码 Query 消息 {1: type, 2: body}
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static ProtoWriter WrapQuery(IQuery query)
        {
            var w = new ProtoWriter();
            w.WriteInt32(1, (int)query.GetQueryType());
            w.WriteBytes(2, query.Serialize());
            return w;
        }
    }
}
