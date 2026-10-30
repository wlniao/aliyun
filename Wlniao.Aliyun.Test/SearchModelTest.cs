namespace Wlniao.Aliyun.TableStore.Test
{
    /// <summary>
    /// 搜索查询与排序的编码测试。标准字节由阿里云官方 SDK 的 Query.Serialize 与 SearchSortBuilder 生成。
    /// </summary>
    [TestClass]
    public class SearchModelTest
    {
        private const string QUERY_TERM = "0a046e616d6512080303000000616263";
        private const string Q_TERM_INT = "0a03636e741209002a00000000000000";
        private const string Q_TERMS = "0a0374616712060301000000611206030100000062";
        private const string Q_RANGE = "0a03636e7412090001000000000000001a09000a0000000000000020012800";
        private const string Q_PREFIX = "0a046e616d6512026162";
        private const string Q_WILD = "0a046e616d651203612a63";
        private const string Q_MATCHPHRASE = "0a057469746c65120b68656c6c6f20776f726c64";
        private const string Q_MATCH = "0a057469746c65120568656c6c6f18022002";
        private const string Q_BOOL = "0a0f0803120b0a016112060301000000311204080b12002801";
        private const string Q_CONST = "0a0f0803120b0a01611206030100000031";
        private const string Q_NESTED = "0a037375621204080b12001803";

        private const string S_FIELD = "0a090a03636e7410011801";
        private const string S_SCORE = "1a020801";
        private const string S_PK = "22020800";

        private const string QW_MATCHALL = "080b1200";
        private const string QW_MATCH = "080112100a057469746c65120568656c6c6f2001";
        private const string QW_MATCHPHRASE = "0802120b0a057469746c6512026869";
        private const string QW_TERM = "080312100a046e616d6512080303000000616263";
        private const string QW_RANGE = "0804121f0a03636e7412090001000000000000001a09000a0000000000000020002800";
        private const string QW_PREFIX = "0805120a0a046e616d6512026162";
        private const string QW_BOOL = "080612060a04080b1200";
        private const string QW_CONST = "080712060a04080b1200";
        private const string QW_NESTED = "0809120d0a037375621204080b12001801";
        private const string QW_WILD = "080a120b0a046e616d651203612a63";
        private const string QW_TERMS = "080f120d0a037461671206030100000061";

        #region 查询
        [TestMethod]
        public void TermQuery_字符串值_与标准字节一致()
        {
            var query = new TermQuery("name", new ColumnValue("abc"));
            Assert.AreEqual(QueryType.TermQuery, query.GetQueryType());
            PlainBufferTest.AssertHex(QUERY_TERM, query.Serialize());
        }

        [TestMethod]
        public void TermQuery_整型值_与标准字节一致()
        {
            var query = new TermQuery("cnt", new ColumnValue(42L));
            PlainBufferTest.AssertHex(Q_TERM_INT, query.Serialize());
        }

        [TestMethod]
        public void MatchAllQuery_编码为空消息()
        {
            var query = new MatchAllQuery();
            Assert.AreEqual(QueryType.MatchAllQuery, query.GetQueryType());
            Assert.AreEqual(0, query.Serialize().Length);
        }

        [TestMethod]
        public void TermsQuery_多值_与标准字节一致()
        {
            var query = new TermsQuery
            {
                FieldName = "tag",
                Terms = new List<ColumnValue> { new ColumnValue("a"), new ColumnValue("b") },
            };
            Assert.AreEqual(QueryType.TermsQuery, query.GetQueryType());
            PlainBufferTest.AssertHex(Q_TERMS, query.Serialize());
        }

        [TestMethod]
        public void RangeQuery_含开闭区间_与标准字节一致()
        {
            var query = new RangeQuery("cnt", new ColumnValue(1L), new ColumnValue(10L))
            {
                IncludeLower = true,
                IncludeUpper = false,
            };
            Assert.AreEqual(QueryType.RangeQuery, query.GetQueryType());
            PlainBufferTest.AssertHex(Q_RANGE, query.Serialize());
        }

        [TestMethod]
        public void PrefixQuery_与标准字节一致()
        {
            var query = new PrefixQuery("name", "ab");
            Assert.AreEqual(QueryType.PrefixQuery, query.GetQueryType());
            PlainBufferTest.AssertHex(Q_PREFIX, query.Serialize());
        }

        [TestMethod]
        public void WildcardQuery_与标准字节一致()
        {
            var query = new WildcardQuery("name", "a*c");
            Assert.AreEqual(QueryType.WildcardQuery, query.GetQueryType());
            PlainBufferTest.AssertHex(Q_WILD, query.Serialize());
        }

        [TestMethod]
        public void MatchPhraseQuery_与标准字节一致()
        {
            var query = new MatchPhraseQuery("title", "hello world");
            Assert.AreEqual(QueryType.MatchPhraseQuery, query.GetQueryType());
            PlainBufferTest.AssertHex(Q_MATCHPHRASE, query.Serialize());
        }

        [TestMethod]
        public void MatchQuery_含最小匹配与运算符_与标准字节一致()
        {
            var query = new MatchQuery("title", "hello")
            {
                MinimumShouldMatch = 2,
                Operator = QueryOperator.AND,
            };
            Assert.AreEqual(QueryType.MatchQuery, query.GetQueryType());
            PlainBufferTest.AssertHex(Q_MATCH, query.Serialize());
        }

        [TestMethod]
        public void BoolQuery_子查询按Query消息嵌套_与标准字节一致()
        {
            var query = new BoolQuery
            {
                MustQueries = new List<IQuery> { new TermQuery("a", new ColumnValue("1")) },
                MustNotQueries = new List<IQuery> { new MatchAllQuery() },
                MinimumShouldMatch = 1,
            };
            Assert.AreEqual(QueryType.BoolQuery, query.GetQueryType());
            PlainBufferTest.AssertHex(Q_BOOL, query.Serialize());
        }

        [TestMethod]
        public void ConstScoreQuery_与标准字节一致()
        {
            var query = new ConstScoreQuery { Filter = new TermQuery("a", new ColumnValue("1")) };
            Assert.AreEqual(QueryType.ConstScoreQuery, query.GetQueryType());
            PlainBufferTest.AssertHex(Q_CONST, query.Serialize());
        }

        [TestMethod]
        public void NestedQuery_与标准字节一致()
        {
            var query = new NestedQuery
            {
                Path = "sub",
                Query = new MatchAllQuery(),
                ScoreMode = ScoreMode.Max,
            };
            Assert.AreEqual(QueryType.NestedQuery, query.GetQueryType());
            PlainBufferTest.AssertHex(Q_NESTED, query.Serialize());
        }
        #endregion

        #region QueryType 线值
        private static byte[] Wrap(IQuery query)
        {
            return SearchQueryCodec.WrapQuery(query).ToArray();
        }

        [TestMethod]
        public void 包装查询_各类型线值与标准一致()
        {
            PlainBufferTest.AssertHex(QW_MATCHALL, Wrap(new MatchAllQuery()));
            PlainBufferTest.AssertHex(QW_MATCH, Wrap(new MatchQuery("title", "hello")));
            PlainBufferTest.AssertHex(QW_MATCHPHRASE, Wrap(new MatchPhraseQuery("title", "hi")));
            PlainBufferTest.AssertHex(QW_TERM, Wrap(new TermQuery("name", new ColumnValue("abc"))));
            PlainBufferTest.AssertHex(QW_RANGE, Wrap(new RangeQuery("cnt", new ColumnValue(1L), new ColumnValue(10L))));
            PlainBufferTest.AssertHex(QW_PREFIX, Wrap(new PrefixQuery("name", "ab")));
            PlainBufferTest.AssertHex(QW_WILD, Wrap(new WildcardQuery("name", "a*c")));
            PlainBufferTest.AssertHex(QW_TERMS, Wrap(new TermsQuery { FieldName = "tag", Terms = { new ColumnValue("a") } }));
            PlainBufferTest.AssertHex(QW_BOOL, Wrap(new BoolQuery { MustQueries = { new MatchAllQuery() } }));
            PlainBufferTest.AssertHex(QW_CONST, Wrap(new ConstScoreQuery { Filter = new MatchAllQuery() }));
            PlainBufferTest.AssertHex(QW_NESTED, Wrap(new NestedQuery { Path = "sub", Query = new MatchAllQuery() }));
        }

        [TestMethod]
        public void 查询模型_未设置字段的默认值与协议一致()
        {
            // 与官方 SDK 及协议默认值一致：运算符 OR、打分模式 NONE、开闭区间均为 false
            Assert.AreEqual(QueryOperator.OR, new MatchQuery("t", "x").Operator);
            Assert.AreEqual(ScoreMode.None, new NestedQuery().ScoreMode);
            var range = new RangeQuery("cnt", new ColumnValue(1L), new ColumnValue(10L));
            Assert.IsFalse(range.IncludeLower);
            Assert.IsFalse(range.IncludeUpper);
        }
        #endregion

        #region 排序
        [TestMethod]
        public void FieldSort_与标准字节一致()
        {
            PlainBufferTest.AssertHex(S_FIELD, new FieldSort("cnt", SortOrder.DESC) { Mode = SortMode.MAX }.Serialize());
        }

        [TestMethod]
        public void ScoreSort_与标准字节一致()
        {
            PlainBufferTest.AssertHex(S_SCORE, new ScoreSort { Order = SortOrder.DESC }.Serialize());
        }

        [TestMethod]
        public void PrimaryKeySort_与标准字节一致()
        {
            PlainBufferTest.AssertHex(S_PK, new PrimaryKeySort(SortOrder.ASC).Serialize());
        }
        #endregion
    }
}
