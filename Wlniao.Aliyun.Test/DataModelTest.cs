namespace Wlniao.Aliyun.TableStore.Test
{
    /// <summary>
    /// 数据模型测试
    /// </summary>
    [TestClass]
    public class DataModelTest
    {
        [TestMethod]
        public void ColumnValue_整型_映射类型与取值()
        {
            var byLong = new ColumnValue(42L);
            Assert.AreEqual(ColumnValueType.Integer, byLong.Type);
            Assert.AreEqual(42L, byLong.IntegerValue);

            var byInt = new ColumnValue((ulong)7);
            Assert.AreEqual(ColumnValueType.Integer, byInt.Type);
            Assert.AreEqual(7L, byInt.IntegerValue);
        }

        [TestMethod]
        public void ColumnValue_字符串布尔浮点二进制_映射类型与取值()
        {
            var str = new ColumnValue("abc");
            Assert.AreEqual(ColumnValueType.String, str.Type);
            Assert.AreEqual("abc", str.StringValue);

            var ok = new ColumnValue(true);
            Assert.AreEqual(ColumnValueType.Boolean, ok.Type);
            Assert.IsTrue(ok.BooleanValue);

            var score = new ColumnValue(3.5d);
            Assert.AreEqual(ColumnValueType.Double, score.Type);
            Assert.AreEqual(3.5d, score.DoubleValue);

            var bin = new ColumnValue(new byte[] { 1, 2, 3 });
            Assert.AreEqual(ColumnValueType.Binary, bin.Type);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, bin.BinaryValue);
        }

        [TestMethod]
        public void ColumnValue_特殊值_按约定识别()
        {
            Assert.IsTrue(ColumnValue.INF_MIN.IsInfMin());
            Assert.IsFalse(ColumnValue.INF_MIN.IsInfMax());
            Assert.IsTrue(ColumnValue.INF_MAX.IsInfMax());
            Assert.IsTrue(ColumnValue.AUTO_INCREMENT.IsPlaceHolderForAutoIncr());
            Assert.IsFalse(new ColumnValue("INF_MINX").IsInfMin());
        }

        [TestMethod]
        public void ColumnValue_字符串转字节_使用UTF8()
        {
            CollectionAssert.AreEqual(new byte[] { 0xE6, 0xA0, 0x87 }, new ColumnValue("标").AsStringInBytes());
        }

        [TestMethod]
        public void ColumnValue_ToString_输出各类型字面值()
        {
            Assert.AreEqual("42", new ColumnValue(42L).ToString());
            Assert.AreEqual("abc", new ColumnValue("abc").ToString());
            Assert.AreEqual("true", new ColumnValue(true).ToString());
            Assert.AreEqual("false", new ColumnValue(false).ToString());
            Assert.AreEqual("3.5", new ColumnValue(3.5d).ToString());
        }

        [TestMethod]
        public void PrimaryKeySchema_两参Add_主键选项为NONE()
        {
            var schema = new PrimaryKeySchema();
            schema.Add("id", ColumnValueType.String);

            Assert.AreEqual(1, schema.Count);
            Assert.AreEqual("id", schema[0].Item1);
            Assert.AreEqual(ColumnValueType.String, schema[0].Item2);
            Assert.AreEqual(PrimaryKeyOption.NONE, schema[0].Item3);
        }

        [TestMethod]
        public void PrimaryKeySchema_三参Add_保留自增选项与顺序()
        {
            var schema = new PrimaryKeySchema();
            schema.Add("id", ColumnValueType.String, PrimaryKeyOption.AUTO_INCREMENT);
            schema.Add("uid", ColumnValueType.Integer);
            schema.Add("bin", ColumnValueType.Binary);

            Assert.AreEqual(3, schema.Count);
            Assert.AreEqual("id", schema[0].Item1);
            Assert.AreEqual(PrimaryKeyOption.AUTO_INCREMENT, schema[0].Item3);
            Assert.AreEqual("uid", schema[1].Item1);
            Assert.AreEqual(ColumnValueType.Integer, schema[1].Item2);
            Assert.AreEqual("bin", schema[2].Item1);
            Assert.AreEqual(ColumnValueType.Binary, schema[2].Item2);
        }

        [TestMethod]
        public void Condition_默认与指定行存在性()
        {
            Assert.AreEqual(RowExistenceExpectation.IGNORE, new Condition().RowExistenceExpect);
            Assert.AreEqual(RowExistenceExpectation.EXPECT_EXIST, new Condition(RowExistenceExpectation.EXPECT_EXIST).RowExistenceExpect);
        }

        [TestMethod]
        public void UpdateOfAttribute_记录写入与删除列()
        {
            var update = new UpdateOfAttribute();
            update.AddAttributeColumnToPut("cnt", new ColumnValue(1L));
            update.AddAttributeColumnToDelete("oldcol");

            Assert.AreEqual(1L, update.AttributeColumnsToPut["cnt"].IntegerValue);
            CollectionAssert.AreEqual(new[] { "oldcol" }, update.AttributeColumnsToDelete.ToArray());
        }

        [TestMethod]
        public void CapacityUnit_与TableMeta_保留构造入参()
        {
            var cu = new CapacityUnit(0, 0);
            Assert.AreEqual(0, cu.Read);
            Assert.AreEqual(0, cu.Write);

            var meta = new TableMeta("demo", new PrimaryKeySchema());
            Assert.AreEqual("demo", meta.TableName);
            Assert.IsNotNull(meta.PrimaryKeySchema);
        }
    }
}
