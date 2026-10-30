namespace Wlniao.Aliyun.TableStore.Test
{
    /// <summary>
    /// Protobuf 线格式读写测试
    /// </summary>
    [TestClass]
    public class ProtobufTest
    {
        [TestMethod]
        public void Varint_各量级数值_往返一致()
        {
            foreach (var value in new ulong[] { 0, 1, 127, 128, 300, 16383, 16384, uint.MaxValue, ulong.MaxValue })
            {
                var w = new ProtoWriter();
                w.WriteTag(1, 0);
                w.WriteVarint(value);
                var r = new ProtoReader(w.ToArray());
                Assert.IsTrue(r.Next());
                Assert.AreEqual(value, r.ReadVarint(), "value: " + value);
            }
        }

        [TestMethod]
        public void Int32与Int64_含负数_往返一致()
        {
            var w = new ProtoWriter();
            w.WriteInt32(1, -1);
            w.WriteInt32(2, int.MinValue);
            w.WriteInt64(3, -2);
            w.WriteInt64(4, long.MinValue);

            var r = new ProtoReader(w.ToArray());
            Assert.IsTrue(r.Next());
            Assert.AreEqual(1, r.Field);
            Assert.AreEqual(-1, r.ReadInt32());
            Assert.IsTrue(r.Next());
            Assert.AreEqual(int.MinValue, r.ReadInt32());
            Assert.IsTrue(r.Next());
            Assert.AreEqual(-2L, r.ReadInt64());
            Assert.IsTrue(r.Next());
            Assert.AreEqual(long.MinValue, r.ReadInt64());
            Assert.IsFalse(r.Next());
        }

        [TestMethod]
        public void 字符串与字节_含中文_往返一致()
        {
            var w = new ProtoWriter();
            w.WriteString(1, "标题abc");
            w.WriteString(2, string.Empty);
            w.WriteBytes(3, new byte[] { 1, 2, 255 });

            var r = new ProtoReader(w.ToArray());
            Assert.IsTrue(r.Next());
            Assert.AreEqual("标题abc", r.ReadString());
            Assert.IsTrue(r.Next());
            Assert.AreEqual(string.Empty, r.ReadString());
            Assert.IsTrue(r.Next());
            CollectionAssert.AreEqual(new byte[] { 1, 2, 255 }, r.ReadBytes());
        }

        [TestMethod]
        public void 布尔_两种取值_往返一致()
        {
            var w = new ProtoWriter();
            w.WriteBool(1, true);
            w.WriteBool(1, false);

            var r = new ProtoReader(w.ToArray());
            Assert.IsTrue(r.Next());
            Assert.IsTrue(r.ReadBool());
            Assert.IsTrue(r.Next());
            Assert.IsFalse(r.ReadBool());
        }

        [TestMethod]
        public void 嵌套消息_按长度前缀读取()
        {
            var inner = new ProtoWriter();
            inner.WriteString(1, "inner");
            var w = new ProtoWriter();
            w.WriteMessage(1, inner);
            w.WriteInt32(2, 7);

            var r = new ProtoReader(w.ToArray());
            Assert.IsTrue(r.Next());
            var sub = r.ReadMessage();
            Assert.IsTrue(sub.Next());
            Assert.AreEqual("inner", sub.ReadString());
            Assert.IsFalse(sub.Next());
            Assert.IsTrue(r.Next());
            Assert.AreEqual(7, r.ReadInt32());
        }

        [TestMethod]
        public void 未知字段_按线类型跳过()
        {
            var w = new ProtoWriter();
            w.WriteString(1, "known");
            w.WriteInt64(2, 99);          // 线类型 0（变长整数）
            w.WriteString(4, "skipped");  // 线类型 2（长度前缀）
            // 字段 6 线类型 1（fixed64），字段 7 线类型 5（fixed32）
            var tail = new byte[] { (6 << 3) | 1, 1, 2, 3, 4, 5, 6, 7, 8, (7 << 3) | 5, 9, 9, 9, 9 };
            var bytes = w.ToArray().Concat(tail).ToArray();

            var r = new ProtoReader(bytes);
            Assert.IsTrue(r.Next());
            Assert.AreEqual("known", r.ReadString());
            Assert.IsTrue(r.Next());
            r.Skip();
            Assert.IsTrue(r.Next());
            r.Skip();
            Assert.IsTrue(r.Next());
            Assert.AreEqual(1, r.Wire);
            r.Skip();
            Assert.IsTrue(r.Next());
            Assert.AreEqual(5, r.Wire);
            r.Skip();
            Assert.IsFalse(r.Next());
        }

        [TestMethod]
        public void 定长字段_跳过后继续读取后续字段()
        {
            // 字段 1 线类型 5（fixed32，4 字节），字段 2 = 8
            var bytes = new byte[] { (1 << 3) | 5, 1, 2, 3, 4, (2 << 3) | 0, 8 };
            var r = new ProtoReader(bytes);
            Assert.IsTrue(r.Next());
            Assert.AreEqual(5, r.Wire);
            r.Skip();
            Assert.IsTrue(r.Next());
            Assert.AreEqual(8, r.ReadInt32());
        }

        [TestMethod]
        public void 截断的流_抛出异常()
        {
            var w = new ProtoWriter();
            w.WriteString(1, "hello world");
            var bytes = w.ToArray();
            var truncated = bytes.Take(bytes.Length - 3).ToArray();

            Assert.ThrowsExactly<OTSException>(() =>
            {
                var r = new ProtoReader(truncated);
                while (r.Next())
                {
                    r.ReadBytes();
                }
            });
        }

        [TestMethod]
        public void 不支持的线类型_抛出异常()
        {
            // 字段 1，线类型 7（非法）
            var w = new ProtoWriter();
            w.WriteVarint(1UL << 3 | 7);
            Assert.ThrowsExactly<OTSException>(() =>
            {
                var r = new ProtoReader(w.ToArray());
                r.Next();
                r.Skip();
            });
        }

        [TestMethod]
        public void 空流_读取不到字段()
        {
            var r = new ProtoReader(new byte[0]);
            Assert.IsFalse(r.Next());
        }
    }
}
