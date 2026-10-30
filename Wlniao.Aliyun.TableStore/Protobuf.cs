/*-----------------------Copyright 2017 www.wlniao.com---------------------------
    文件名称：Wlniao\Aliyun\TableStore\Protobuf.cs
    适用环境：NETCoreCLR 1.0+
    功能描述：Protobuf 线格式读写（仅覆盖 TableStore 协议所需）

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
using System.IO;
using System.Text;

namespace Wlniao.Aliyun.TableStore
{
    /// <summary>
    /// Protobuf 线格式写入器
    /// </summary>
    internal sealed class ProtoWriter
    {
        private readonly MemoryStream ms = new MemoryStream();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void WriteVarint(ulong value)
        {
            while (value >= 0x80)
            {
                ms.WriteByte((byte)(value | 0x80));
                value >>= 7;
            }
            ms.WriteByte((byte)value);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="wire"></param>
        public void WriteTag(int field, int wire)
        {
            WriteVarint((ulong)(field << 3 | wire));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        public void WriteInt32(int field, int value)
        {
            WriteTag(field, 0);
            WriteVarint((ulong)value);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        public void WriteInt64(int field, long value)
        {
            WriteTag(field, 0);
            WriteVarint((ulong)value);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        public void WriteBool(int field, bool value)
        {
            WriteTag(field, 0);
            WriteVarint(value ? 1UL : 0UL);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        public void WriteString(int field, string value)
        {
            WriteBytes(field, Encoding.UTF8.GetBytes(value ?? string.Empty));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        public void WriteBytes(int field, byte[] value)
        {
            value = value ?? new byte[0];
            WriteTag(field, 2);
            WriteVarint((ulong)value.Length);
            ms.Write(value, 0, value.Length);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        public void WriteMessage(int field, ProtoWriter value)
        {
            WriteBytes(field, value.ToArray());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            return ms.ToArray();
        }
    }

    /// <summary>
    /// Protobuf 线格式读取器
    /// </summary>
    internal sealed class ProtoReader
    {
        private readonly byte[] buf;
        private readonly int end;
        private int pos;

        /// <summary>
        /// 当前字段号
        /// </summary>
        public int Field { get; private set; }
        /// <summary>
        /// 当前线类型
        /// </summary>
        public int Wire { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public ProtoReader(byte[] data) : this(data, 0, data == null ? 0 : data.Length)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public ProtoReader(byte[] data, int offset, int count)
        {
            buf = data ?? new byte[0];
            pos = offset;
            end = offset + count;
        }

        /// <summary>
        /// 读取下一个字段标记，已到末尾返回 false
        /// </summary>
        public bool Next()
        {
            if (pos >= end)
            {
                return false;
            }
            var tag = ReadVarint();
            Field = (int)(tag >> 3);
            Wire = (int)(tag & 7);
            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ulong ReadVarint()
        {
            ulong result = 0;
            var shift = 0;
            while (pos < end)
            {
                var b = buf[pos++];
                result |= (ulong)(b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                {
                    return result;
                }
                shift += 7;
                if (shift > 63)
                {
                    throw new OTSException("Invalid varint in protobuf stream");
                }
            }
            throw new OTSException("Unexpected end of protobuf stream");
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ReadBytes()
        {
            var len = (int)ReadVarint();
            if (len < 0 || pos + len > end)
            {
                throw new OTSException("Invalid length in protobuf stream");
            }
            var result = new byte[len];
            Array.Copy(buf, pos, result, 0, len);
            pos += len;
            return result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string ReadString()
        {
            return Encoding.UTF8.GetString(ReadBytes());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public long ReadInt64()
        {
            return (long)ReadVarint();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int ReadInt32()
        {
            return (int)(long)ReadVarint();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool ReadBool()
        {
            return ReadVarint() != 0;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ProtoReader ReadMessage()
        {
            return new ProtoReader(ReadBytes());
        }
        /// <summary>
        /// 跳过当前字段内容
        /// </summary>
        public void Skip()
        {
            switch (Wire)
            {
                case 0:
                    ReadVarint();
                    break;
                case 1:
                    Advance(8);
                    break;
                case 2:
                    Advance((int)ReadVarint());
                    break;
                case 5:
                    Advance(4);
                    break;
                default:
                    throw new OTSException("Unsupported wire type: " + Wire);
            }
        }
        private void Advance(int count)
        {
            if (count < 0 || pos + count > end)
            {
                throw new OTSException("Unexpected end of protobuf stream");
            }
            pos += count;
        }
    }
}
