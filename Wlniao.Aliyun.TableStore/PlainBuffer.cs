/*-----------------------Copyright 2017 www.wlniao.com---------------------------
    文件名称：Wlniao\Aliyun\TableStore\PlainBuffer.cs
    适用环境：NETCoreCLR 1.0+
    功能描述：TableStore PlainBuffer 行编码/解码

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
using System.IO;
using System.Text;

namespace Wlniao.Aliyun.TableStore
{
    /// <summary>
    /// PlainBuffer 单元格（属性列）
    /// </summary>
    internal sealed class PlainBufferCell
    {
        /// <summary>列名</summary>
        public string Name;
        /// <summary>列值，删除操作时为空</summary>
        public ColumnValue Value;
        /// <summary>版本时间戳</summary>
        public long? Timestamp;
        /// <summary>操作类型标记，写入时为空</summary>
        public byte? CellType;
    }

    /// <summary>
    /// TableStore PlainBuffer 行编码/解码
    /// </summary>
    internal static class PlainBuffer
    {
        /// <summary>行起始标记</summary>
        public const byte TAG_ROW_PK = 1;
        /// <summary>行数据起始标记</summary>
        public const byte TAG_ROW_DATA = 2;
        /// <summary>单元格起始标记</summary>
        public const byte TAG_CELL = 3;
        /// <summary>列名</summary>
        public const byte TAG_CELL_NAME = 4;
        /// <summary>列值</summary>
        public const byte TAG_CELL_VALUE = 5;
        /// <summary>操作类型</summary>
        public const byte TAG_CELL_TYPE = 6;
        /// <summary>时间戳</summary>
        public const byte TAG_CELL_TIMESTAMP = 7;
        /// <summary>删除行标记</summary>
        public const byte TAG_DELETE_ROW_MARKER = 8;
        /// <summary>行校验</summary>
        public const byte TAG_ROW_CHECKSUM = 9;
        /// <summary>单元格校验</summary>
        public const byte TAG_CELL_CHECKSUM = 10;

        /// <summary>删除该列全部版本</summary>
        public const byte DELETE_ALL_VERSION = 1;
        /// <summary>删除该列指定版本</summary>
        public const byte DELETE_ONE_VERSION = 3;
        /// <summary>原子自增</summary>
        public const byte INCREMENT = 4;

        private const byte VT_INTEGER = 0;
        private const byte VT_DOUBLE = 1;
        private const byte VT_BOOLEAN = 2;
        private const byte VT_STRING = 3;
        private const byte VT_BLOB = 7;
        private const byte VT_INF_MIN = 9;
        private const byte VT_INF_MAX = 10;
        private const byte VT_AUTO_INCREMENT = 11;

        /// <summary>
        /// 编码主键（仅主键的行）
        /// </summary>
        public static byte[] WritePrimaryKey(PrimaryKey primaryKey)
        {
            return WriteRow(primaryKey, null, false);
        }

        /// <summary>
        /// 编码行
        /// </summary>
        /// <param name="primaryKey">主键</param>
        /// <param name="cells">属性列，可为空</param>
        /// <param name="deleteMarker">是否为删除行</param>
        public static byte[] WriteRow(PrimaryKey primaryKey, IList<PlainBufferCell> cells, bool deleteMarker)
        {
            using (var ms = new MemoryStream())
            {
                WriteInt32(ms, 117);
                ms.WriteByte(TAG_ROW_PK);
                var crc = (byte)0;
                foreach (var kv in primaryKey)
                {
                    var name = Encoding.UTF8.GetBytes(kv.Key);
                    var cellCrc = Crc8.Compute(0, name);
                    cellCrc = ComputeValueChecksum(cellCrc, kv.Value);
                    WriteCell(ms, name, kv.Value, null, null);
                    crc = Crc8.Compute(crc, cellCrc);
                }
                if (cells != null && cells.Count > 0)
                {
                    ms.WriteByte(TAG_ROW_DATA);
                    foreach (var cell in cells)
                    {
                        var name = Encoding.UTF8.GetBytes(cell.Name);
                        var cellCrc = Crc8.Compute(0, name);
                        if (cell.Value != null)
                        {
                            cellCrc = ComputeValueChecksum(cellCrc, cell.Value);
                        }
                        if (cell.Timestamp.HasValue)
                        {
                            cellCrc = Crc8.Compute(cellCrc, cell.Timestamp.Value);
                        }
                        if (cell.CellType.HasValue)
                        {
                            cellCrc = Crc8.Compute(cellCrc, cell.CellType.Value);
                        }
                        WriteCell(ms, name, cell.Value, cell.Timestamp, cell.CellType);
                        crc = Crc8.Compute(crc, cellCrc);
                    }
                }
                if (deleteMarker)
                {
                    ms.WriteByte(TAG_DELETE_ROW_MARKER);
                }
                crc = Crc8.Compute(crc, (byte)(deleteMarker ? 1 : 0));
                ms.WriteByte(TAG_ROW_CHECKSUM);
                ms.WriteByte(crc);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// 解码单行
        /// </summary>
        /// <param name="data"></param>
        public static Row ReadRow(byte[] data)
        {
            using (var ms = new MemoryStream(data ?? new byte[0]))
            {
                if (ReadInt32(ms) != 117)
                {
                    throw new OTSException("Invalid PlainBuffer header");
                }
                return ReadRow(ms);
            }
        }

        private static Row ReadRow(MemoryStream ms)
        {
            var primaryKey = new PrimaryKey();
            var columns = new AttributeColumns();
            var crc = (byte)0;
            var tag = ReadByte(ms);
            if (tag != TAG_ROW_PK)
            {
                throw new OTSException("Expect TAG_ROW_PK but it was " + tag);
            }
            tag = ReadByte(ms);
            while (tag == TAG_CELL)
            {
                byte checksum;
                var cell = ReadCell(ms, out checksum);
                crc = Crc8.Compute(crc, checksum);
                primaryKey.Add(cell.Name, cell.Value);
                tag = ReadByte(ms);
            }
            if (primaryKey.Count == 0)
            {
                throw new OTSException("The primary key of row is empty");
            }
            if (tag == TAG_ROW_DATA)
            {
                tag = ReadByte(ms);
                while (tag == TAG_CELL)
                {
                    byte checksum;
                    var cell = ReadCell(ms, out checksum);
                    crc = Crc8.Compute(crc, checksum);
                    if (cell.Value != null)
                    {
                        columns.Add(cell.Name, cell.Value);
                    }
                    tag = ReadByte(ms);
                }
            }
            var deleteMarker = tag == TAG_DELETE_ROW_MARKER;
            if (deleteMarker)
            {
                tag = ReadByte(ms);
            }
            if (tag != TAG_ROW_CHECKSUM)
            {
                throw new OTSException("Expect TAG_ROW_CHECKSUM but it was " + tag);
            }
            crc = Crc8.Compute(crc, (byte)(deleteMarker ? 1 : 0));
            if (crc != ReadByte(ms))
            {
                throw new OTSException("Checksum is mismatch in PlainBuffer row");
            }
            return new Row { PrimaryKey = primaryKey, AttributeColumns = columns };
        }

        private static PlainBufferCell ReadCell(MemoryStream ms, out byte checksum)
        {
            var cell = new PlainBufferCell();
            byte[] nameRaw = null;
            var tag = ReadByte(ms);
            if (tag == TAG_CELL_NAME)
            {
                nameRaw = ReadBytes(ms, ReadInt32(ms));
                cell.Name = Encoding.UTF8.GetString(nameRaw);
                tag = ReadByte(ms);
            }
            if (tag == TAG_CELL_VALUE)
            {
                cell.Value = ReadValue(ms);
                tag = ReadByte(ms);
            }
            if (tag == TAG_CELL_TYPE)
            {
                cell.CellType = ReadByte(ms);
                tag = ReadByte(ms);
            }
            if (tag == TAG_CELL_TIMESTAMP)
            {
                cell.Timestamp = ReadInt64(ms);
                tag = ReadByte(ms);
            }
            if (tag != TAG_CELL_CHECKSUM)
            {
                throw new OTSException("Expect TAG_CELL_CHECKSUM but it was " + tag);
            }
            checksum = ComputeCellChecksum(nameRaw ?? new byte[0], cell.Value, cell.Timestamp, cell.CellType);
            if (checksum != ReadByte(ms))
            {
                throw new OTSException("Checksum is mismatch in PlainBuffer cell");
            }
            return cell;
        }

        private static ColumnValue ReadValue(MemoryStream ms)
        {
            ReadInt32(ms);
            var type = ReadByte(ms);
            switch (type)
            {
                case VT_INTEGER:
                    return new ColumnValue(ReadInt64(ms));
                case VT_DOUBLE:
                    return new ColumnValue(BitConverter.Int64BitsToDouble(ReadInt64(ms)));
                case VT_BOOLEAN:
                    return new ColumnValue(ReadByte(ms) != 0);
                case VT_STRING:
                    return new ColumnValue(Encoding.UTF8.GetString(ReadBytes(ms, ReadInt32(ms))));
                case VT_BLOB:
                    return new ColumnValue(ReadBytes(ms, ReadInt32(ms)));
                case VT_INF_MIN:
                    return ColumnValue.INF_MIN;
                case VT_INF_MAX:
                    return ColumnValue.INF_MAX;
                case VT_AUTO_INCREMENT:
                    return ColumnValue.AUTO_INCREMENT;
                default:
                    throw new OTSException("Unsupported column type: " + type);
            }
        }

        private static void WriteCell(MemoryStream ms, byte[] name, ColumnValue value, long? timestamp, byte? cellType)
        {
            ms.WriteByte(TAG_CELL);
            ms.WriteByte(TAG_CELL_NAME);
            WriteInt32(ms, name.Length);
            ms.Write(name, 0, name.Length);
            if (value != null)
            {
                ms.WriteByte(TAG_CELL_VALUE);
                WriteValue(ms, value);
            }
            if (cellType.HasValue)
            {
                ms.WriteByte(TAG_CELL_TYPE);
                ms.WriteByte(cellType.Value);
            }
            if (timestamp.HasValue)
            {
                ms.WriteByte(TAG_CELL_TIMESTAMP);
                WriteInt64(ms, timestamp.Value);
            }
            ms.WriteByte(TAG_CELL_CHECKSUM);
            ms.WriteByte(ComputeCellChecksum(name, value, timestamp, cellType));
        }

        private static void WriteValue(MemoryStream ms, ColumnValue value)
        {
            switch (value.Type)
            {
                case ColumnValueType.Integer:
                    WriteInt32(ms, 9);
                    ms.WriteByte(VT_INTEGER);
                    WriteInt64(ms, value.IntegerValue);
                    break;
                case ColumnValueType.Double:
                    WriteInt32(ms, 9);
                    ms.WriteByte(VT_DOUBLE);
                    WriteInt64(ms, BitConverter.DoubleToInt64Bits(value.DoubleValue));
                    break;
                case ColumnValueType.Boolean:
                    WriteInt32(ms, 2);
                    ms.WriteByte(VT_BOOLEAN);
                    ms.WriteByte((byte)(value.BooleanValue ? 1 : 0));
                    break;
                case ColumnValueType.String:
                    if (value.IsInfMin())
                    {
                        WriteSpecial(ms, VT_INF_MIN);
                    }
                    else if (value.IsInfMax())
                    {
                        WriteSpecial(ms, VT_INF_MAX);
                    }
                    else if (value.IsPlaceHolderForAutoIncr())
                    {
                        WriteSpecial(ms, VT_AUTO_INCREMENT);
                    }
                    else
                    {
                        var str = value.AsStringInBytes();
                        WriteInt32(ms, 5 + str.Length);
                        ms.WriteByte(VT_STRING);
                        WriteInt32(ms, str.Length);
                        ms.Write(str, 0, str.Length);
                    }
                    break;
                case ColumnValueType.Binary:
                    var bin = value.BinaryValue;
                    WriteInt32(ms, 5 + bin.Length);
                    ms.WriteByte(VT_BLOB);
                    WriteInt32(ms, bin.Length);
                    ms.Write(bin, 0, bin.Length);
                    break;
                default:
                    throw new OTSException("Unsupported column type: " + value.Type);
            }
        }

        private static void WriteSpecial(MemoryStream ms, byte type)
        {
            WriteInt32(ms, 1);
            ms.WriteByte(type);
        }

        private static byte ComputeCellChecksum(byte[] name, ColumnValue value, long? timestamp, byte? cellType)
        {
            var crc = Crc8.Compute(0, name);
            if (value != null)
            {
                crc = ComputeValueChecksum(crc, value);
            }
            if (timestamp.HasValue)
            {
                crc = Crc8.Compute(crc, timestamp.Value);
            }
            if (cellType.HasValue)
            {
                crc = Crc8.Compute(crc, cellType.Value);
            }
            return crc;
        }

        private static byte ComputeValueChecksum(byte crc, ColumnValue value)
        {
            if (value.IsInfMin())
            {
                return Crc8.Compute(crc, VT_INF_MIN);
            }
            if (value.IsInfMax())
            {
                return Crc8.Compute(crc, VT_INF_MAX);
            }
            if (value.IsPlaceHolderForAutoIncr())
            {
                return Crc8.Compute(crc, VT_AUTO_INCREMENT);
            }
            switch (value.Type)
            {
                case ColumnValueType.Integer:
                    crc = Crc8.Compute(crc, VT_INTEGER);
                    return Crc8.Compute(crc, value.IntegerValue);
                case ColumnValueType.Double:
                    crc = Crc8.Compute(crc, VT_DOUBLE);
                    return Crc8.Compute(crc, BitConverter.DoubleToInt64Bits(value.DoubleValue));
                case ColumnValueType.Boolean:
                    crc = Crc8.Compute(crc, VT_BOOLEAN);
                    return Crc8.Compute(crc, (byte)(value.BooleanValue ? 1 : 0));
                case ColumnValueType.String:
                    var str = value.AsStringInBytes();
                    crc = Crc8.Compute(crc, VT_STRING);
                    crc = Crc8.Compute(crc, str.Length);
                    return Crc8.Compute(crc, str);
                case ColumnValueType.Binary:
                    var bin = value.BinaryValue;
                    crc = Crc8.Compute(crc, VT_BLOB);
                    crc = Crc8.Compute(crc, bin.Length);
                    return Crc8.Compute(crc, bin);
                default:
                    throw new OTSException("Unsupported column type: " + value.Type);
            }
        }

        private static void WriteInt32(MemoryStream ms, int value)
        {
            ms.WriteByte((byte)value);
            ms.WriteByte((byte)(value >> 8));
            ms.WriteByte((byte)(value >> 16));
            ms.WriteByte((byte)(value >> 24));
        }

        private static void WriteInt64(MemoryStream ms, long value)
        {
            WriteInt32(ms, (int)value);
            WriteInt32(ms, (int)(value >> 32));
        }

        private static byte ReadByte(MemoryStream ms)
        {
            var b = ms.ReadByte();
            if (b < 0)
            {
                throw new OTSException("Unexpected end of PlainBuffer stream");
            }
            return (byte)b;
        }

        private static int ReadInt32(MemoryStream ms)
        {
            return ReadByte(ms) | ReadByte(ms) << 8 | ReadByte(ms) << 16 | ReadByte(ms) << 24;
        }

        private static long ReadInt64(MemoryStream ms)
        {
            return (uint)ReadInt32(ms) | (long)ReadInt32(ms) << 32;
        }

        private static byte[] ReadBytes(MemoryStream ms, int count)
        {
            if (count < 0 || count > ms.Length - ms.Position)
            {
                throw new OTSException("Unexpected end of PlainBuffer stream");
            }
            var result = new byte[count];
            ms.Read(result, 0, count);
            return result;
        }
    }

    /// <summary>
    /// CRC8 校验（多项式 0x07）
    /// </summary>
    internal static class Crc8
    {
        private static readonly byte[] Table = CreateTable();

        private static byte[] CreateTable()
        {
            var table = new byte[256];
            for (var i = 0; i < table.Length; i++)
            {
                var b = (byte)i;
                for (var j = 0; j < 8; j++)
                {
                    b = (byte)((b << 1) ^ ((b & 0x80) != 0 ? 7 : 0));
                }
                table[i] = b;
            }
            return table;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="crc"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte Compute(byte crc, byte data)
        {
            return Table[(crc ^ data) & 0xFF];
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="crc"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte Compute(byte crc, int data)
        {
            for (var i = 0; i < 4; i++)
            {
                crc = Compute(crc, (byte)(data & 0xFF));
                data >>= 8;
            }
            return crc;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="crc"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte Compute(byte crc, long data)
        {
            for (var i = 0; i < 8; i++)
            {
                crc = Compute(crc, (byte)(data & 0xFF));
                data >>= 8;
            }
            return crc;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="crc"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte Compute(byte crc, byte[] data)
        {
            if (data != null)
            {
                for (var i = 0; i < data.Length; i++)
                {
                    crc = Compute(crc, data[i]);
                }
            }
            return crc;
        }
    }
}
