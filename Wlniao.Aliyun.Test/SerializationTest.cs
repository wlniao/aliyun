namespace Wlniao.Aliyun.TableStore.Test
{
    /// <summary>
    /// 行到实体的序列化测试
    /// </summary>
    [TestClass]
    public class SerializationTest
    {
        /// <summary>
        /// 承载各类型属性的实体
        /// </summary>
        public class SampleEntity
        {
            public string Id { get; set; } = string.Empty;
            public long Cnt { get; set; }
            public int Num { get; set; }
            public string Name { get; set; } = string.Empty;
            public bool Ok { get; set; }
            public double Score { get; set; }
            public ulong Big { get; set; }
            public byte[] Bin { get; set; } = Array.Empty<byte>();
        }

        private static Row BuildRow()
        {
            var pk = new PrimaryKey { ["Id"] = new ColumnValue("user-1") };
            var cols = new AttributeColumns
            {
                ["Cnt"] = new ColumnValue(42L),
                ["Num"] = new ColumnValue(5L),
                ["Name"] = new ColumnValue("abc"),
                ["Ok"] = new ColumnValue(true),
                ["Score"] = new ColumnValue(3.5d),
                ["Bin"] = new ColumnValue(new byte[] { 1, 2, 3 }),
            };
            return new Row { PrimaryKey = pk, AttributeColumns = cols };
        }

        [TestMethod]
        public void ConvertTo_行_映射各类型属性()
        {
            var entity = Serialization.ConvertTo<SampleEntity>(BuildRow());

            Assert.IsNotNull(entity);
            Assert.AreEqual("user-1", entity.Id);
            Assert.AreEqual(42L, entity.Cnt);
            Assert.AreEqual(5, entity.Num);
            Assert.AreEqual("abc", entity.Name);
            Assert.IsTrue(entity.Ok);
            Assert.AreEqual(3.5d, entity.Score);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, entity.Bin);
        }

        [TestMethod]
        public void ConvertTo_行_主键与属性列都参与映射()
        {
            var entity = Serialization.ConvertTo<SampleEntity>(BuildRow());
            Assert.IsNotNull(entity);
            // Id 来自主键列，其余来自属性列
            Assert.AreEqual("user-1", entity.Id);
            Assert.AreEqual("abc", entity.Name);
        }

        [TestMethod]
        public void ConvertTo_二进制列_映射到字节数组属性()
        {
            var pk = new PrimaryKey { ["Id"] = new ColumnValue("user-2") };
            var cols = new AttributeColumns { ["Bin"] = new ColumnValue(new byte[] { 9, 8 }) };
            var entity = Serialization.ConvertTo<SampleEntity>(new Row { PrimaryKey = pk, AttributeColumns = cols });

            Assert.IsNotNull(entity);
            CollectionAssert.AreEqual(new byte[] { 9, 8 }, entity.Bin);
        }

        [TestMethod]
        public void ConvertTo_整型列_映射到ulong属性()
        {
            var pk = new PrimaryKey { ["Id"] = new ColumnValue("user-5") };
            var cols = new AttributeColumns { ["Big"] = new ColumnValue(7L) };
            var entity = Serialization.ConvertTo<SampleEntity>(new Row { PrimaryKey = pk, AttributeColumns = cols });

            Assert.IsNotNull(entity);
            Assert.AreEqual(7UL, entity.Big);
        }

        [TestMethod]
        public void ConvertTo_列值类型不匹配_跳过该列不影响其余列()
        {
            var pk = new PrimaryKey { ["Id"] = new ColumnValue("user-6") };
            var cols = new AttributeColumns
            {
                ["Cnt"] = new ColumnValue("not-a-number"),
                ["Name"] = new ColumnValue("abc"),
            };
            var entity = Serialization.ConvertTo<SampleEntity>(new Row { PrimaryKey = pk, AttributeColumns = cols });

            Assert.IsNotNull(entity);
            Assert.AreEqual("user-6", entity.Id);
            Assert.AreEqual("abc", entity.Name);
            Assert.AreEqual(0L, entity.Cnt);
        }

        [TestMethod]
        public void ConvertTo_列名在实体上不存在_忽略该列()
        {
            var pk = new PrimaryKey { ["Id"] = new ColumnValue("user-7") };
            var cols = new AttributeColumns
            {
                ["NoSuchProperty"] = new ColumnValue(1L),
                ["Name"] = new ColumnValue("abc"),
            };
            var entity = Serialization.ConvertTo<SampleEntity>(new Row { PrimaryKey = pk, AttributeColumns = cols });

            Assert.IsNotNull(entity);
            Assert.AreEqual("user-7", entity.Id);
            Assert.AreEqual("abc", entity.Name);
        }

        [TestMethod]
        public void ConvertTo_空主键行_返回默认()
        {
            var row = new Row { PrimaryKey = new PrimaryKey(), AttributeColumns = new AttributeColumns() };
            Assert.IsNull(Serialization.ConvertTo<SampleEntity>(row));
        }

        [TestMethod]
        public void ConvertTo_入参为空_返回默认()
        {
            Row row = null!;
            GetRowResponse res = null!;
            Assert.IsNull(Serialization.ConvertTo<SampleEntity>(row));
            Assert.IsNull(Serialization.ConvertTo<SampleEntity>(res));
        }

        [TestMethod]
        public void ConvertTo_遇到空字符串值_返回默认()
        {
            var pk = new PrimaryKey { ["Id"] = new ColumnValue("user-3") };
            var cols = new AttributeColumns { ["Name"] = new ColumnValue(string.Empty) };
            Assert.IsNull(Serialization.ConvertTo<SampleEntity>(new Row { PrimaryKey = pk, AttributeColumns = cols }));
        }

        [TestMethod]
        public void ConvertTo_GetRowResponse_映射主键与属性列()
        {
            var res = new GetRowResponse
            {
                PrimaryKey = new PrimaryKey { ["Id"] = new ColumnValue("user-4") },
                Attribute = new AttributeColumns { ["Cnt"] = new ColumnValue(7L) },
            };
            var entity = Serialization.ConvertTo<SampleEntity>(res);

            Assert.IsNotNull(entity);
            Assert.AreEqual("user-4", entity.Id);
            Assert.AreEqual(7L, entity.Cnt);
        }

        [TestMethod]
        public void ConvertTo_GetRowResponse_空主键_返回默认()
        {
            Assert.IsNull(Serialization.ConvertTo<SampleEntity>(new GetRowResponse()));
        }
    }
}
