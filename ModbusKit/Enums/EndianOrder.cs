using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusKit.Enums
{
    public enum EndianOrder
    {
        BigEndian,          // Modbus 标准顺序
        LittleEndian,       // 字节和字都小端序
        BigEndianByteSwap,  // 大端序但每个寄存器内字节交换
        LittleEndianByteSwap // 小端序但每个寄存器内字节交换
    }
}
