namespace Wlniao.Aliyun.TableStore.Test
{
    /// <summary>
    /// 接口请求编码与响应解码测试。标准字节由阿里云官方 SDK 的 protobuf 生成类输出。
    /// </summary>
    [TestClass]
    public class ProtocolTest
    {
        internal const string REQ_PUTROW = "0a0464656d6f1250750000000103040200000069640507000000030200000072310a7a02030403000000636e7405090000000001000000000000000a960304040000006e616d65050800000003030000006162630ada09421a020802";
        internal const string REQ_UPDATEROW = "0a0464656d6f1247750000000103040200000069640507000000030200000072310a7a020304060000006f6c64636f6c06010a7b030403000000636e7405090000000007000000000000000afc09cd1a020801";
        internal const string REQ_GETROW = "0a0464656d6f121d750000000103040200000069640507000000030200000072310a7a09202801";
        internal const string REQ_CREATETABLE = "0a190a0464656d6f12080a0269641002180112070a03756964100112060a04080010001a0d08ffffffffffffffffff011001";
        internal const string REQ_SEARCH = "0a0464656d6f120464656d6f1a020801222f10012214080312100a046e616d651208030300000061626332130a0b0a090a03636e74100118010a041a0208014000";
        internal const string REQ_SEARCH_SPECIFIED = "0a0464656d6f120464656d6f1a080802120161120162220410014000";
        internal const string REQ_SEARCH_OPTS = "0a0464656d6f120464656d6f220f080310052a030a016640014a020102";
        internal const string REQ_CREATEIDX = "0a0464656d6f120464656d6f1a2a0a130a057469746c651005180128013000380048000a110a03636e741001180128013000380048001200";
        internal const string REQ_DESCRIBE = "0a0464656d6f";

        internal const string RES_GETROW = "0a060a04080110001252750000000103040200000069640507000000030200000072390a4202030403000000636e7405090000000005000000000000000ada0304040000006e616d65050a000000030500000068656c6c6f0a7409e9";
        internal const string RES_DESCRIBE = "0a190a0464656d6f12080a0269641002180112070a037569641001120a0a0408001000100018001a0d08ffffffffffffffffff0110012001";
        internal const string RES_LISTTABLE = "0a0274310a027432";
        internal const string RES_SEARCH = "08011235750000000103040200000069640507000000030200000061310a12020304010000007605090000000001000000000000000a9309aa1801";
        internal const string RES_SEARCH_TOKEN = "08011235750000000103040200000069640507000000030200000061310a12020304010000007605090000000001000000000000000a9309aa180032020102";
        internal const string RES_ERROR = "0a114f54534f626a6563744e6f744578697374122354686520737065636966696564207461626c6520646f6573206e6f742065786973742e";

        private static TableMeta BuildDemoMeta()
        {
            var schema = new PrimaryKeySchema();
            schema.Add("id", ColumnValueType.String, PrimaryKeyOption.AUTO_INCREMENT);
            schema.Add("uid", ColumnValueType.Integer);
            return new TableMeta("demo", schema);
        }

        #region 请求编码
        [TestMethod]
        public void EncodePutRow_与标准字节一致()
        {
            var cols = new AttributeColumns
            {
                ["cnt"] = new ColumnValue(1L),
                ["name"] = new ColumnValue("abc"),
            };
            var request = new PutRowRequest("demo", new Condition(RowExistenceExpectation.EXPECT_NOT_EXIST),
                new PrimaryKey { ["id"] = new ColumnValue("r1") }, cols);

            PlainBufferTest.AssertHex(REQ_PUTROW, Protocol.EncodePutRow(request));
        }

        [TestMethod]
        public void EncodeGetRow_与标准字节一致()
        {
            var request = new GetRowRequest("demo", new PrimaryKey { ["id"] = new ColumnValue("r1") });
            PlainBufferTest.AssertHex(REQ_GETROW, Protocol.EncodeGetRow(request));
        }

        [TestMethod]
        public void EncodeUpdateRow_删除列先于写入列_与标准字节一致()
        {
            var update = new UpdateOfAttribute();
            update.AddAttributeColumnToDelete("oldcol");
            update.AddAttributeColumnToPut("cnt", new ColumnValue(7L));
            var request = new UpdateRowRequest("demo", new Condition(RowExistenceExpectation.EXPECT_EXIST),
                new PrimaryKey { ["id"] = new ColumnValue("r1") }, update);

            PlainBufferTest.AssertHex(REQ_UPDATEROW, Protocol.EncodeUpdateRow(request));
        }

        [TestMethod]
        public void EncodeCreateTable_与标准字节一致()
        {
            var request = new CreateTableRequest(BuildDemoMeta(), new CapacityUnit(0, 0));
            PlainBufferTest.AssertHex(REQ_CREATETABLE, Protocol.EncodeCreateTable(request));
        }

        [TestMethod]
        public void EncodeDescribeTable_与标准字节一致()
        {
            PlainBufferTest.AssertHex(REQ_DESCRIBE, Protocol.EncodeDescribeTable("demo"));
        }

        [TestMethod]
        public void EncodeSearch_含查询与排序_与标准字节一致()
        {
            var search = new SearchQuery
            {
                Limit = 1,
                Query = new TermQuery("name", new ColumnValue("abc")),
                Sort = new Sort(new List<ISorter>
                {
                    new FieldSort("cnt", SortOrder.DESC) { Mode = SortMode.MAX },
                    new ScoreSort { Order = SortOrder.DESC },
                }),
            };
            var request = new SearchRequest("demo", "demo", search)
            {
                ColumnsToGet = new ColumnsToGet { ReturnAll = true }
            };

            PlainBufferTest.AssertHex(REQ_SEARCH, Protocol.EncodeSearch(request));
        }

        [TestMethod]
        public void EncodeSearch_指定返回列_与标准字节一致()
        {
            var request = new SearchRequest("demo", "demo", new SearchQuery { Limit = 1 })
            {
                ColumnsToGet = new ColumnsToGet { Columns = { "a", "b" } }
            };

            PlainBufferTest.AssertHex(REQ_SEARCH_SPECIFIED, Protocol.EncodeSearch(request));
        }

        [TestMethod]
        public void EncodeSearch_带分页折叠与令牌_与标准字节一致()
        {
            var search = new SearchQuery
            {
                Offset = 3,
                Limit = 5,
                GetTotalCount = true,
                Token = new byte[] { 1, 2 },
                Collapse = new Collapse { FieldName = "f" },
            };
            var request = new SearchRequest("demo", "demo", search);

            PlainBufferTest.AssertHex(REQ_SEARCH_OPTS, Protocol.EncodeSearch(request));
        }

        [TestMethod]
        public void EncodeCreateSearchIndex_与标准字节一致()
        {
            var schema = new IndexSchema
            {
                FieldSchemas = new List<FieldSchema>
                {
                    new FieldSchema("title", FieldType.TEXT) { Index = true },
                    new FieldSchema("cnt", FieldType.LONG) { Index = true },
                }
            };
            var request = new CreateSearchIndexRequest("demo", "demo") { IndexSchema = schema };

            PlainBufferTest.AssertHex(REQ_CREATEIDX, Protocol.EncodeCreateSearchIndex(request));
        }
        #endregion

        #region 响应解码
        [TestMethod]
        public void DecodeListTable_标准字节_得到表名列表()
        {
            var res = Protocol.DecodeListTable(PlainBufferTest.HexToBytes(RES_LISTTABLE));
            CollectionAssert.AreEqual(new[] { "t1", "t2" }, res.TableNames.ToArray());
        }

        [TestMethod]
        public void DecodeDescribeTable_标准字节_得到主键结构()
        {
            var res = Protocol.DecodeDescribeTable(PlainBufferTest.HexToBytes(RES_DESCRIBE));

            Assert.IsNotNull(res.TableMeta);
            Assert.AreEqual("demo", res.TableMeta.TableName);
            Assert.AreEqual(2, res.TableMeta.PrimaryKeySchema.Count);
            Assert.AreEqual("id", res.TableMeta.PrimaryKeySchema[0].Item1);
            Assert.AreEqual(ColumnValueType.String, res.TableMeta.PrimaryKeySchema[0].Item2);
            Assert.AreEqual(PrimaryKeyOption.AUTO_INCREMENT, res.TableMeta.PrimaryKeySchema[0].Item3);
            Assert.AreEqual("uid", res.TableMeta.PrimaryKeySchema[1].Item1);
            Assert.AreEqual(ColumnValueType.Integer, res.TableMeta.PrimaryKeySchema[1].Item2);
        }

        [TestMethod]
        public void DecodeGetRow_标准字节_得到行数据()
        {
            var res = Protocol.DecodeGetRow(PlainBufferTest.HexToBytes(RES_GETROW));

            Assert.AreEqual("r9", res.PrimaryKey["id"].StringValue);
            Assert.AreEqual(5L, res.Attribute["cnt"].IntegerValue);
            Assert.AreEqual("hello", res.Attribute["name"].StringValue);
        }

        [TestMethod]
        public void DecodeGetRow_无行数据_主键与属性为空()
        {
            var res = Protocol.DecodeGetRow(new byte[0]);
            Assert.AreEqual(0, res.PrimaryKey.Count);
            Assert.AreEqual(0, res.Attribute.Count);
        }

        [TestMethod]
        public void DecodeSearch_标准字节_得到行列表与总数()
        {
            var res = Protocol.DecodeSearch(PlainBufferTest.HexToBytes(RES_SEARCH));

            Assert.AreEqual(1L, res.TotalCount);
            Assert.IsTrue(res.IsAllSuccess);
            Assert.AreEqual(1, res.Rows.Count);
            Assert.AreEqual("a1", res.Rows[0].PrimaryKey["id"].StringValue);
            Assert.AreEqual(1L, res.Rows[0].AttributeColumns["v"].IntegerValue);
        }

        [TestMethod]
        public void DecodeSearch_带继续检索令牌_得到令牌与完成标记()
        {
            var res = Protocol.DecodeSearch(PlainBufferTest.HexToBytes(RES_SEARCH_TOKEN));

            Assert.IsFalse(res.IsAllSuccess);
            CollectionAssert.AreEqual(new byte[] { 1, 2 }, res.NextToken);
            Assert.AreEqual(1, res.Rows.Count);
        }

        [TestMethod]
        public void DecodeError_标准字节_得到错误码与描述()
        {
            Protocol.DecodeError(PlainBufferTest.HexToBytes(RES_ERROR), out var code, out var message);
            Assert.AreEqual("OTSObjectNotExist", code);
            Assert.AreEqual("The specified table does not exist.", message);
        }
        #endregion
    }
}
