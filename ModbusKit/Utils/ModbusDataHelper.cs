using ModbusKit.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusKit.Utils
{
    public static class ModbusDataHelper
    {
        public static ushort GetTypeSize(DataType type)
        {
            switch (type)
            {
                case DataType.BINARY:
                case DataType.GBK:
                case DataType.ASCII:
                case DataType.UINT16:
                case DataType.INT16:
                    return 1;
                case DataType.UINT32:
                case DataType.INT32:
                case DataType.FLOAT:
                    return 2;
                case DataType.UINT64:
                case DataType.INT64:
                case DataType.DOUBLE:
                    return 4;
                default:
                    throw new ArgumentException($"Invalid Type: {type}");
            }
        }

        public static string ConvertBytesToValue(byte[] bytes, DataType type)
        {
            switch (type)
            {
                case DataType.BINARY:
                    return string.Join(" ", bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
                case DataType.GBK:
                    return Encoding.GetEncoding("GBK").GetString(bytes).Trim(new char[] { '\0' });
                case DataType.ASCII:
                    return Encoding.ASCII.GetString(bytes).Trim(new char[] { '\0' });
                case DataType.UINT16:
                    return BitConverter.ToUInt16(bytes, 0).ToString();
                case DataType.INT16:
                    return BitConverter.ToInt16(bytes, 0).ToString();
                case DataType.UINT32:
                    return BitConverter.ToUInt32(bytes, 0).ToString();
                case DataType.INT32:
                    return BitConverter.ToInt32(bytes, 0).ToString();
                case DataType.UINT64:
                    return BitConverter.ToUInt64(bytes, 0).ToString();
                case DataType.INT64:
                    return BitConverter.ToInt64(bytes, 0).ToString();
                case DataType.FLOAT:
                    return BitConverter.ToSingle(bytes, 0).ToString();
                case DataType.DOUBLE:
                    return BitConverter.ToDouble(bytes, 0).ToString();
                default:
                    throw new ArgumentException($"Invalid Type: {type}");
            }
        }

        public static T ConvertBytesToValue<T>(byte[] bytes, DataType type)
        {
            var valStr = ConvertBytesToValue(bytes, type);
            return (T)Convert.ChangeType(valStr, typeof(T));
        }

        public static byte[] ConvertValueToBytes(string value, DataType type)
        {
            switch (type)
            {
                case DataType.BINARY:
                    return value.Split(' ').Select(v => Convert.ToByte(v, 2)).ToArray();
                case DataType.GBK:
                    return Encoding.GetEncoding("GBK").GetBytes(value);
                case DataType.ASCII:
                    return Encoding.ASCII.GetBytes(value);
                case DataType.UINT16:
                    return BitConverter.GetBytes(ushort.Parse(value));
                case DataType.INT16:
                    return BitConverter.GetBytes(short.Parse(value));
                case DataType.UINT32:
                    return BitConverter.GetBytes(uint.Parse(value));
                case DataType.INT32:
                    return BitConverter.GetBytes(int.Parse(value));
                case DataType.UINT64:
                    return BitConverter.GetBytes(ulong.Parse(value));
                case DataType.INT64:
                    return BitConverter.GetBytes(long.Parse(value));
                case DataType.FLOAT:
                    return BitConverter.GetBytes(float.Parse(value));
                case DataType.DOUBLE:
                    return BitConverter.GetBytes(double.Parse(value));
                default:
                    throw new ArgumentException($"Invalid valueType: {type}");
            }
        }

        //public static byte[] ConvertValueToBytes(object value, DataType type)
        //{
        //    var valStr = Convert.ToString(value);
        //    return ConvertValueToBytes(valStr, type);
        //}

        public static Dictionary<int, byte[]> ConvertRegistersToBytes(DataType type, ushort[] allRegisters, EndianOrder order)
        {
            ushort size = GetTypeSize(type);
            var dic = new Dictionary<int, byte[]>();

            for (ushort i = 0; i < allRegisters.Length; i += size)
            {
                var bytes = new byte[size * 2];
                var registers = allRegisters.Skip(i).Take(size).ToArray();

                if (registers.Length == 1)
                {
                    bytes[0] = (byte)(registers[0] >> 8);
                    bytes[1] = (byte)(registers[0] & 0xFF);
                }
                else if (registers.Length == 2)
                {
                    switch (order)
                    {
                        case EndianOrder.BigEndian:// ABCD
                                                     // 大端序：寄存器0是高位，寄存器1是低位
                            bytes[0] = (byte)(registers[0] >> 8);
                            bytes[1] = (byte)(registers[0] & 0xFF);
                            bytes[2] = (byte)(registers[1] >> 8);
                            bytes[3] = (byte)(registers[1] & 0xFF);
                            break;

                        case EndianOrder.LittleEndian://DCBA
                                                        // 小端序：寄存器0是低位，寄存器1是高位
                            bytes[0] = (byte)(registers[1] & 0xFF);
                            bytes[1] = (byte)(registers[1] >> 8);
                            bytes[2] = (byte)(registers[0] & 0xFF);
                            bytes[3] = (byte)(registers[0] >> 8);
                            break;

                        case EndianOrder.BigEndianByteSwap://BADC
                                                             // 大端序但字节交换：每个寄存器内字节顺序交换
                            bytes[0] = (byte)(registers[0] & 0xFF);
                            bytes[1] = (byte)(registers[0] >> 8);
                            bytes[2] = (byte)(registers[1] & 0xFF);
                            bytes[3] = (byte)(registers[1] >> 8);
                            break;

                        case EndianOrder.LittleEndianByteSwap://CDAB
                                                                // 小端序但字节交换：每个寄存器内字节顺序交换
                            bytes[0] = (byte)(registers[1] >> 8);
                            bytes[1] = (byte)(registers[1] & 0xFF);
                            bytes[2] = (byte)(registers[0] >> 8);
                            bytes[3] = (byte)(registers[0] & 0xFF);
                            break;
                    }
                }
                else if (registers.Length == 4)
                {
                    switch (order)
                    {
                        case EndianOrder.BigEndian:// ABCDEFGH
                                                     // 大端序：寄存器0是高位，寄存器1是低位
                            bytes[0] = (byte)(registers[0] >> 8);
                            bytes[1] = (byte)(registers[0] & 0xFF);
                            bytes[2] = (byte)(registers[1] >> 8);
                            bytes[3] = (byte)(registers[1] & 0xFF);
                            bytes[4] = (byte)(registers[2] >> 8);
                            bytes[5] = (byte)(registers[2] & 0xFF);
                            bytes[6] = (byte)(registers[3] >> 8);
                            bytes[7] = (byte)(registers[3] & 0xFF);
                            break;

                        case EndianOrder.LittleEndian://HGFECDBA
                                                        // 小端序：寄存器0是低位，寄存器1是高位
                            bytes[0] = (byte)(registers[3] & 0xFF);
                            bytes[1] = (byte)(registers[3] >> 8);
                            bytes[2] = (byte)(registers[2] & 0xFF);
                            bytes[3] = (byte)(registers[2] >> 8);
                            bytes[4] = (byte)(registers[1] & 0xFF);
                            bytes[5] = (byte)(registers[1] >> 8);
                            bytes[6] = (byte)(registers[0] & 0xFF);
                            bytes[7] = (byte)(registers[0] >> 8);
                            break;

                        case EndianOrder.BigEndianByteSwap://BADCFEHG
                                                             // 大端序但字节交换：每个寄存器内字节顺序交换
                            bytes[0] = (byte)(registers[0] & 0xFF);
                            bytes[1] = (byte)(registers[0] >> 8);
                            bytes[2] = (byte)(registers[1] & 0xFF);
                            bytes[3] = (byte)(registers[1] >> 8);
                            bytes[4] = (byte)(registers[2] & 0xFF);
                            bytes[5] = (byte)(registers[2] >> 8);
                            bytes[6] = (byte)(registers[3] & 0xFF);
                            bytes[7] = (byte)(registers[3] >> 8);
                            break;

                        case EndianOrder.LittleEndianByteSwap://GHEFCDAB
                                                                // 小端序但字节交换：每个寄存器内字节顺序交换
                            bytes[0] = (byte)(registers[3] >> 8);
                            bytes[1] = (byte)(registers[3] & 0xFF);
                            bytes[2] = (byte)(registers[2] >> 8);
                            bytes[3] = (byte)(registers[2] & 0xFF);
                            bytes[4] = (byte)(registers[1] >> 8);
                            bytes[5] = (byte)(registers[1] & 0xFF);
                            bytes[6] = (byte)(registers[0] >> 8);
                            bytes[7] = (byte)(registers[0] & 0xFF);
                            break;
                    }
                }

                // 如果系统是小端序，需要反转字节顺序
                if (BitConverter.IsLittleEndian && type != DataType.ASCII && type != DataType.GBK && type != DataType.BINARY)
                {
                    Array.Reverse(bytes);
                }

                dic.Add(i / size, bytes);
            }

            return dic;
        }

        public static ushort[] ConvertBytesToRegisters(DataType type, Dictionary<int, byte[]> dic, EndianOrder order)
        {
            ushort size = GetTypeSize(type);
            var allRegisters = new List<ushort>();

            for (ushort i = 0; i < dic.Count; i++)
            {
                var bytes = dic[i];

                // 如果系统是小端序，需要反转字节顺序
                if (BitConverter.IsLittleEndian && type != DataType.ASCII && type != DataType.GBK && type != DataType.BINARY)
                {
                    Array.Reverse(bytes);
                }

                if (bytes.Length == 1)
                {
                    allRegisters.Add(bytes[0]);
                }
                else if (bytes.Length == 2)
                {
                    allRegisters.Add((ushort)((bytes[0] << 8) | bytes[1]));
                }
                else if (bytes.Length == 4)
                {
                    switch (order)
                    {
                        case EndianOrder.BigEndian:// ABCD
                                                     // 大端序：寄存器0是高位，寄存器1是低位
                            allRegisters.Add((ushort)((bytes[0] << 8) | bytes[1]));
                            allRegisters.Add((ushort)((bytes[2] << 8) | bytes[3]));
                            break;
                        case EndianOrder.LittleEndian://DCBA
                                                        // 小端序：寄存器0是低位，寄存器1是高位
                            allRegisters.Add((ushort)(bytes[2] | bytes[3] << 8));
                            allRegisters.Add((ushort)(bytes[0] | bytes[1] << 8));
                            break;
                        case EndianOrder.BigEndianByteSwap://BADC
                                                             // 大端序但字节交换：每个寄存器内字节顺序交换
                            allRegisters.Add((ushort)(bytes[0] | bytes[1] << 8));
                            allRegisters.Add((ushort)(bytes[2] | bytes[3] << 8));
                            break;
                        case EndianOrder.LittleEndianByteSwap://CDAB
                                                                // 小端序但字节交换：每个寄存器内字节顺序交换
                            allRegisters.Add((ushort)((bytes[2] << 8) | bytes[3]));
                            allRegisters.Add((ushort)((bytes[0] << 8) | bytes[1]));
                            break;
                    }
                }
                else if (bytes.Length == 8)
                {
                    switch (order)
                    {
                        case EndianOrder.BigEndian:// ABCDEFGH
                                                     // 大端序：寄存器0是高位，寄存器1是低位
                            allRegisters.Add((ushort)((bytes[0] << 8) | bytes[1]));
                            allRegisters.Add((ushort)((bytes[2] << 8) | bytes[3]));
                            allRegisters.Add((ushort)((bytes[4] << 8) | bytes[5]));
                            allRegisters.Add((ushort)((bytes[6] << 8) | bytes[7]));
                            break;
                        case EndianOrder.LittleEndian://HGFECDBA
                                                        // 小端序：寄存器0是低位，寄存器1是高位
                            allRegisters.Add((ushort)(bytes[6] | bytes[7] << 8));
                            allRegisters.Add((ushort)(bytes[4] | bytes[5] << 8));
                            allRegisters.Add((ushort)(bytes[2] | bytes[3] << 8));
                            allRegisters.Add((ushort)(bytes[0] | bytes[1] << 8));
                            break;
                        case EndianOrder.BigEndianByteSwap://BADCFEHG
                                                             // 大端序但字节交换：每个寄存器内字节顺序交换
                            allRegisters.Add((ushort)(bytes[0] | bytes[1] << 8));
                            allRegisters.Add((ushort)(bytes[2] | bytes[3] << 8));
                            allRegisters.Add((ushort)(bytes[4] | bytes[5] << 8));
                            allRegisters.Add((ushort)(bytes[6] | bytes[7] << 8));
                            break;
                        case EndianOrder.LittleEndianByteSwap://GHEFCDAB
                                                                // 小端序但字节交换：每个寄存器内字节顺序交换
                            allRegisters.Add((ushort)((bytes[6] << 8) | bytes[7]));
                            allRegisters.Add((ushort)((bytes[4] << 8) | bytes[5]));
                            allRegisters.Add((ushort)((bytes[2] << 8) | bytes[3]));
                            allRegisters.Add((ushort)((bytes[0] << 8) | bytes[1]));
                            break;
                    }
                }
            }

            return allRegisters.ToArray();
        }

        public static T ConvertRegistersToValue<T>(DataType type, ushort[] allRegisters, EndianOrder order)
        {
            var dic = ConvertRegistersToBytes(type, allRegisters, order);

            return ConvertBytesToValue<T>(dic[0], type);
        }

        public static ushort[] ConvertValueToRegisters(string value, DataType type, EndianOrder order)
        {
            var bytes = ConvertValueToBytes(value, type);

            var dic = new Dictionary<int, byte[]> { { 0, bytes } };

            return ConvertBytesToRegisters(type, dic, order);
        }

    }
}
