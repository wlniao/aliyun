using System.Globalization;

namespace Wlniao.Aliyun.TableStore.Test
{
    /// <summary>
    /// PlainBuffer 行编解码测试。标准字节由阿里云官方 SDK 的 PlainBufferBuilder 生成。
    /// </summary>
    [TestClass]
    public class PlainBufferTest
    {
        private const string PB_PK_SINGLE = "75000000010304020000006964050e0000000309000000757365722d313030310a9e0937";
        private const string PB_PK_MULTI = "750000000103040300000075696405090000000066270000000000000a220304040000006e616d65050b0000000306000000e5bca0e4b8890ab403040300000062696e05090000000704000000010203ff0aa609c5";
        private const string PB_ROW_MIXED = "75000000010304020000006964050a0000000305000000726f772d310a1102030403000000636e740509000000002a000000000000000a5503040500000073636f72650509000000010000000000000c400a2c0304020000006f6b050200000002010a830304050000007469746c65050b0000000306000000e6a087e9a2980a88030404000000626c6f62050800000007030000000908070af80976";
        private const string PB_ROW_UPDATE = "75000000010304020000006964050a0000000305000000726f772d320a18020304060000006f6c64636f6c06010a7b030403000000636e7405090000000007000000000000000afc09b4";
        private const string PB_PK_SPECIAL = "7500000001030405000000626567696e0501000000090a8a030403000000656e6405010000000a0a650938";
        private const string PB_ROW_DELETE = "750000000103040200000069640507000000030200000072330a740809f1";
        private const string PB_ROW_TS = "750000000103040200000069640507000000030200000072340a6102030403000000636e74050900000000010000000000000007d2029649000000000a1e092f";

        internal static byte[] HexToBytes(string hex)
        {
            var bytes = new byte[hex.Length / 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = byte.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber);
            }
            return bytes;
        }

        internal static void AssertHex(string expectedHex, byte[] actual)
        {
            CollectionAssert.AreEqual(HexToBytes(expectedHex), actual);
        }

        [TestMethod]
        public void WritePrimaryKey_单列字符串_与标准字节一致()
        {
            var pk = new PrimaryKey { ["id"] = new ColumnValue("user-1001") };
            AssertHex(PB_PK_SINGLE, PlainBuffer.WritePrimaryKey(pk));
        }

        [TestMethod]
        public void WritePrimaryKey_整型字符串二进制混合_与标准字节一致()
        {
            var pk = new PrimaryKey
            {
                ["uid"] = new ColumnValue(10086L),
                ["name"] = new ColumnValue("张三"),
                ["bin"] = new ColumnValue(new byte[] { 1, 2, 3, 255 }),
            };
            AssertHex(PB_PK_MULTI, PlainBuffer.WritePrimaryKey(pk));
        }

        [TestMethod]
        public void WriteRow_全类型属性列_与标准字节一致()
        {
            var pk = new PrimaryKey { ["id"] = new ColumnValue("row-1") };
            var cells = new List<PlainBufferCell>
            {
                new PlainBufferCell { Name = "cnt", Value = new ColumnValue(42L) },
                new PlainBufferCell { Name = "score", Value = new ColumnValue(3.5d) },
                new PlainBufferCell { Name = "ok", Value = new ColumnValue(true) },
                new PlainBufferCell { Name = "title", Value = new ColumnValue("标题") },
                new PlainBufferCell { Name = "blob", Value = new ColumnValue(new byte[] { 9, 8, 7 }) },
            };
            AssertHex(PB_ROW_MIXED, PlainBuffer.WriteRow(pk, cells, false));
        }

        [TestMethod]
        public void WriteRow_删除列并写入_与标准字节一致()
        {
            var pk = new PrimaryKey { ["id"] = new ColumnValue("row-2") };
            var cells = new List<PlainBufferCell>
            {
                new PlainBufferCell { Name = "oldcol", CellType = PlainBuffer.DELETE_ALL_VERSION },
                new PlainBufferCell { Name = "cnt", Value = new ColumnValue(7L) },
            };
            AssertHex(PB_ROW_UPDATE, PlainBuffer.WriteRow(pk, cells, false));
        }

        [TestMethod]
        public void WritePrimaryKey_含正负无穷占位_与标准字节一致()
        {
            var pk = new PrimaryKey
            {
                ["begin"] = ColumnValue.INF_MIN,
                ["end"] = ColumnValue.INF_MAX,
            };
            AssertHex(PB_PK_SPECIAL, PlainBuffer.WritePrimaryKey(pk));
        }

        [TestMethod]
        public void WriteRow_删除行标记_与标准字节一致()
        {
            var pk = new PrimaryKey { ["id"] = new ColumnValue("r3") };
            AssertHex(PB_ROW_DELETE, PlainBuffer.WriteRow(pk, null, true));
        }

        [TestMethod]
        public void WriteRow_列带时间戳_与标准字节一致()
        {
            var pk = new PrimaryKey { ["id"] = new ColumnValue("r4") };
            var cells = new List<PlainBufferCell>
            {
                new PlainBufferCell { Name = "cnt", Value = new ColumnValue(1L), Timestamp = 1234567890L },
            };
            AssertHex(PB_ROW_TS, PlainBuffer.WriteRow(pk, cells, false));
        }

        [TestMethod]
        public void ReadRow_含正负无穷主键_按约定还原()
        {
            var row = PlainBuffer.ReadRow(HexToBytes(PB_PK_SPECIAL));
            Assert.IsTrue(row.PrimaryKey["begin"].IsInfMin());
            Assert.IsTrue(row.PrimaryKey["end"].IsInfMax());
        }

        [TestMethod]
        public void ReadRow_标准字节_解析出主键与属性列()
        {
            var row = PlainBuffer.ReadRow(HexToBytes(PB_ROW_MIXED));

            Assert.AreEqual("row-1", row.PrimaryKey["id"].StringValue);
            Assert.AreEqual(42L, row.AttributeColumns["cnt"].IntegerValue);
            Assert.AreEqual(3.5d, row.AttributeColumns["score"].DoubleValue);
            Assert.IsTrue(row.AttributeColumns["ok"].BooleanValue);
            Assert.AreEqual("标题", row.AttributeColumns["title"].StringValue);
            CollectionAssert.AreEqual(new byte[] { 9, 8, 7 }, row.AttributeColumns["blob"].BinaryValue);
        }

        [TestMethod]
        public void ReadRow_只含主键的字节_属性列为空()
        {
            var row = PlainBuffer.ReadRow(HexToBytes(PB_PK_SINGLE));
            Assert.AreEqual("user-1001", row.PrimaryKey["id"].StringValue);
            Assert.AreEqual(0, row.AttributeColumns.Count);
        }

        [TestMethod]
        public void WriteRow后ReadRow_各类型取值往返一致()
        {
            var pk = new PrimaryKey
            {
                ["uid"] = new ColumnValue(10086L),
                ["name"] = new ColumnValue("张三"),
                ["bin"] = new ColumnValue(new byte[] { 1, 2, 3, 255 }),
            };
            var cells = new List<PlainBufferCell>
            {
                new PlainBufferCell { Name = "cnt", Value = new ColumnValue(42L) },
                new PlainBufferCell { Name = "score", Value = new ColumnValue(3.5d) },
                new PlainBufferCell { Name = "ok", Value = new ColumnValue(true) },
                new PlainBufferCell { Name = "title", Value = new ColumnValue("标题") },
                new PlainBufferCell { Name = "blob", Value = new ColumnValue(new byte[] { 9, 8, 7 }) },
            };

            var row = PlainBuffer.ReadRow(PlainBuffer.WriteRow(pk, cells, false));

            Assert.AreEqual(10086L, row.PrimaryKey["uid"].IntegerValue);
            Assert.AreEqual("张三", row.PrimaryKey["name"].StringValue);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 255 }, row.PrimaryKey["bin"].BinaryValue);
            Assert.AreEqual(42L, row.AttributeColumns["cnt"].IntegerValue);
            Assert.AreEqual(3.5d, row.AttributeColumns["score"].DoubleValue);
            Assert.IsTrue(row.AttributeColumns["ok"].BooleanValue);
            Assert.AreEqual("标题", row.AttributeColumns["title"].StringValue);
            CollectionAssert.AreEqual(new byte[] { 9, 8, 7 }, row.AttributeColumns["blob"].BinaryValue);
        }

        [TestMethod]
        public void ReadRow_被篡改的字节_抛出异常()
        {
            var bytes = HexToBytes(PB_ROW_MIXED);
            bytes[bytes.Length / 2] ^= 0xFF;
            Assert.ThrowsExactly<OTSException>(() => PlainBuffer.ReadRow(bytes));
        }

        [TestMethod]
        public void ReadRow_非法头部_抛出异常()
        {
            var bytes = HexToBytes(PB_PK_SINGLE);
            bytes[0] = 0x00;
            Assert.ThrowsExactly<OTSException>(() => PlainBuffer.ReadRow(bytes));
        }
    }
}
