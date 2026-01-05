using ModbusKit.Enums;
using NModbus;
using NModbus.Serial;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ModbusKit.Utils
{
    public class ModbusKitMaster : IDisposable
    {
        SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);


        int _delay = 20;
        EndianOrder _endian = EndianOrder.BigEndian;

        IModbusMaster _master;



        public ModbusKitMaster()
        {

        }


        public static ModbusKitMaster CreateSerialMaster(string port, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var masterSerialPort = new SerialPort(port, baudRate, parity, dataBits, stopBits)
            {
                Handshake = Handshake.None,
                ReadTimeout = 500,
                WriteTimeout = 500,
                Encoding = Encoding.ASCII
            };
            masterSerialPort.Open();

            var factory = new ModbusFactory();
            var master = factory.CreateRtuMaster(masterSerialPort);

            var modbusKitMaster = new ModbusKitMaster();
            modbusKitMaster._master = master;

            return modbusKitMaster;
        }


        public static ModbusKitMaster CreateTcpMaster(string ip, int port)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var client = new TcpClient(ip, port);

            var factory = new ModbusFactory();
            var master = factory.CreateMaster(client);

            var modbusKitMaster = new ModbusKitMaster();
            modbusKitMaster._master = master;

            return modbusKitMaster;
        }


        public static ModbusKitMaster CreateUdpMaster(IPAddress ip, int port)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var client = new UdpClient();
            client.Connect(ip, port);

            var factory = new ModbusFactory();
            var master = factory.CreateMaster(client);

            var modbusKitMaster = new ModbusKitMaster();
            modbusKitMaster._master = master;

            return modbusKitMaster;
        }


        public void SetEndian(EndianOrder endian)
        {
            _endian = endian;
        }


        public void SetDelay(int delay)
        {
            _delay = delay;
        }


        public void Dispose()
        {
            _master?.Dispose();
            _master = null;
            _semaphoreSlim?.Dispose();
            _semaphoreSlim = null;
        }






        private async Task<ushort[]> SafeReadMultipleRegistersAsync(byte slaveId, ushort startAddress, ushort numberOfPoints)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                // 执行受保护的读取操作
                var result = await _master.ReadHoldingRegistersAsync(slaveId, startAddress, numberOfPoints);
                // 读取完成后等待间隔
                await Task.Delay(_delay);
                return result;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private async Task SafeWriteMultipleRegistersAsync(byte slaveId, ushort startAddress, ushort[] data)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                // 执行受保护的写入操作
                await _master.WriteMultipleRegistersAsync(slaveId, startAddress, data);
                // 写入完成后等待间隔
                await Task.Delay(_delay);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }


        #region 封装层

        public async Task Write_Single_Value_ToHoldingRegisters(byte slaveId, ushort startAddress, DataType type, string value)
        {
            var dic = new Dictionary<int, byte[]>();

            var bytes = ModbusDataHelper.ConvertValueToBytes(value, type);

            if (type == DataType.ASCII && bytes.Length > 2)
            {
                throw new Exception($"数据格式有误");
            }
            else if (type == DataType.GBK && bytes.Length != 2)
            {
                throw new Exception($"数据格式有误");
            }
            else if (type == DataType.BINARY && bytes.Length > 2)
            {
                throw new Exception($"数据格式有误");
            }

            dic.Add(0, bytes);

            ushort[] allRegisters = ModbusDataHelper.ConvertBytesToRegisters(type, dic, _endian);

            await SafeWriteMultipleRegistersAsync(slaveId, startAddress, allRegisters);
        }

        public async Task Write_Multi_Value_ToHoldingRegisters(byte slaveId, ushort startAddress, DataType type, List<string> values)
        {
            var dic = new Dictionary<int, byte[]>();

            for (int i = 0; i < values.Count; i++)
            {
                var bytes = ModbusDataHelper.ConvertValueToBytes(values[i], type);

                if (type == DataType.ASCII && bytes.Length > 2)
                {
                    throw new Exception($"数据格式有误");
                }
                else if (type == DataType.GBK && bytes.Length != 2)
                {
                    throw new Exception($"数据格式有误");
                }
                else if (type == DataType.BINARY && bytes.Length > 2)
                {
                    throw new Exception($"数据格式有误");
                }

                dic.Add(i, bytes);
            }

            ushort[] allRegisters = ModbusDataHelper.ConvertBytesToRegisters(type, dic, _endian);

            await SafeWriteMultipleRegistersAsync(slaveId, startAddress, allRegisters);
        }

        public async Task<string> Read_Single_Value_FromHoldingRegisters(byte slaveId, ushort startAddress, DataType type)
        {
            ushort size = ModbusDataHelper.GetTypeSize(type);

            ushort[] allRegisters = await SafeReadMultipleRegistersAsync(slaveId, startAddress, size);

            var dic = ModbusDataHelper.ConvertRegistersToBytes(type, allRegisters, _endian);

            return ModbusDataHelper.ConvertBytesToValue(dic[0], type);
        }

        public async Task<List<string>> Read_Multi_Value_FromHoldingRegisters(byte slaveId, ushort startAddress, DataType type, ushort num)
        {
            ushort size = ModbusDataHelper.GetTypeSize(type);

            ushort[] allRegisters = await SafeReadMultipleRegistersAsync(slaveId, startAddress, (ushort)(num * size));

            var dic = ModbusDataHelper.ConvertRegistersToBytes(type, allRegisters, _endian);

            var list = new List<string>();
            for (ushort i = 0; i < num; i++)
            {
                list.Add(ModbusDataHelper.ConvertBytesToValue(dic[i], type));
            }

            return list;
        }

        public async Task<T> Read_Single_Value_FromHoldingRegisters<T>(byte slaveId, ushort startAddress, DataType type)
        {
            ushort size = ModbusDataHelper.GetTypeSize(type);

            ushort[] allRegisters = await SafeReadMultipleRegistersAsync(slaveId, startAddress, size);

            var dic = ModbusDataHelper.ConvertRegistersToBytes(type, allRegisters, _endian);

            return ModbusDataHelper.ConvertBytesToValue<T>(dic[0], type);
        }

        public async Task<List<T>> Read_Multi_Value_FromHoldingRegisters<T>(byte slaveId, ushort startAddress, DataType type, ushort num)
        {
            ushort size = ModbusDataHelper.GetTypeSize(type);

            ushort[] allRegisters = await SafeReadMultipleRegistersAsync(slaveId, startAddress, (ushort)(num * size));

            var dic = ModbusDataHelper.ConvertRegistersToBytes(type, allRegisters, _endian);

            var list = new List<T>();
            for (ushort i = 0; i < num; i++)
            {
                list.Add(ModbusDataHelper.ConvertBytesToValue<T>(dic[i], type));
            }

            return list;
        }


        public async Task Write_Multi_ASCII_ToHoldingRegisters(byte slaveId, ushort startAddress, string ascii)
        {
            var result = Enumerable.Range(0, ascii.Length / 2 + ascii.Length % 2).Select(i => ascii.Substring(i * 2, Math.Min(2, ascii.Length - i * 2))).ToList();
            await Write_Multi_Value_ToHoldingRegisters(slaveId, startAddress, DataType.ASCII, result);
        }

        public async Task<string> Read_Multi_ASCII_FromHoldingRegisters(byte slaveId, ushort startAddress, ushort num)
        {
            var result = await Read_Multi_Value_FromHoldingRegisters<string>(slaveId, startAddress, DataType.ASCII, num);
            return string.Join("", result);
        }

        public async Task Write_Multi_GBK_ToHoldingRegisters(byte slaveId, ushort startAddress, string gbk)
        {
            var result = Enumerable.Range(0, gbk.Length).Select(i => gbk.Substring(i, 1)).ToList();
            await Write_Multi_Value_ToHoldingRegisters(slaveId, startAddress, DataType.GBK, result);
        }

        public async Task<string> Read_Multi_GBK_FromHoldingRegisters(byte slaveId, ushort startAddress, ushort num)
        {
            var result = await Read_Multi_Value_FromHoldingRegisters<string>(slaveId, startAddress, DataType.GBK, num);
            return string.Join("", result);
        }


        public async Task Write_Single_Uint16_ToHoldingRegisters(byte slaveId, ushort startAddress, string uint16) => await Write_Single_Value_ToHoldingRegisters(slaveId, startAddress, DataType.UINT16, uint16);

        public async Task Write_Multi_Uint16_ToHoldingRegisters(byte slaveId, ushort startAddress, List<string> uint16s) => await Write_Multi_Value_ToHoldingRegisters(slaveId, startAddress, DataType.UINT16, uint16s);

        public async Task<ushort> Read_Single_Uint16_FromHoldingRegisters(byte slaveId, ushort startAddress) => await Read_Single_Value_FromHoldingRegisters<ushort>(slaveId, startAddress, DataType.UINT16);

        public async Task<List<ushort>> Read_Multi_Uint16_FromHoldingRegisters(byte slaveId, ushort startAddress, ushort num) => await Read_Multi_Value_FromHoldingRegisters<ushort>(slaveId, startAddress, DataType.UINT16, num);


        public async Task Write_Single_Int16_ToHoldingRegisters(byte slaveId, ushort startAddress, string int16) => await Write_Single_Value_ToHoldingRegisters(slaveId, startAddress, DataType.INT16, int16);

        public async Task Write_Multi_Int16_ToHoldingRegisters(byte slaveId, ushort startAddress, List<string> int16s) => await Write_Multi_Value_ToHoldingRegisters(slaveId, startAddress, DataType.INT16, int16s);

        public async Task<short> Read_Single_Int16_FromHoldingRegisters(byte slaveId, ushort startAddress) => await Read_Single_Value_FromHoldingRegisters<short>(slaveId, startAddress, DataType.INT16);

        public async Task<List<short>> Read_Multi_Int16_FromHoldingRegisters(byte slaveId, ushort startAddress, ushort num) => await Read_Multi_Value_FromHoldingRegisters<short>(slaveId, startAddress, DataType.INT16, num);


        public async Task Write_Single_Uint32_ToHoldingRegisters(byte slaveId, ushort startAddress, string uint32) => await Write_Single_Value_ToHoldingRegisters(slaveId, startAddress, DataType.UINT32, uint32);

        public async Task Write_Multi_Uint32_ToHoldingRegisters(byte slaveId, ushort startAddress, List<string> uint32s) => await Write_Multi_Value_ToHoldingRegisters(slaveId, startAddress, DataType.UINT32, uint32s);

        public async Task<uint> Read_Single_Uint32_FromHoldingRegisters(byte slaveId, ushort startAddress) => await Read_Single_Value_FromHoldingRegisters<uint>(slaveId, startAddress, DataType.UINT32);

        public async Task<List<uint>> Read_Multi_Uint32_FromHoldingRegisters(byte slaveId, ushort startAddress, ushort num) => await Read_Multi_Value_FromHoldingRegisters<uint>(slaveId, startAddress, DataType.UINT32, num);


        public async Task Write_Single_Int32_ToHoldingRegisters(byte slaveId, ushort startAddress, string int32) => await Write_Single_Value_ToHoldingRegisters(slaveId, startAddress, DataType.INT32, int32);

        public async Task Write_Multi_Int32_ToHoldingRegisters(byte slaveId, ushort startAddress, List<string> int32s) => await Write_Multi_Value_ToHoldingRegisters(slaveId, startAddress, DataType.INT32, int32s);

        public async Task<int> Read_Single_Int32_FromHoldingRegisters(byte slaveId, ushort startAddress) => await Read_Single_Value_FromHoldingRegisters<int>(slaveId, startAddress, DataType.INT32);

        public async Task<List<int>> Read_Multi_Int32_FromHoldingRegisters(byte slaveId, ushort startAddress, ushort num) => await Read_Multi_Value_FromHoldingRegisters<int>(slaveId, startAddress, DataType.INT32, num);


        public async Task Write_Single_Uint64_ToHoldingRegisters(byte slaveId, ushort startAddress, string uint64) => await Write_Single_Value_ToHoldingRegisters(slaveId, startAddress, DataType.UINT64, uint64);

        public async Task Write_Multi_Uint64_ToHoldingRegisters(byte slaveId, ushort startAddress, List<string> uint64s) => await Write_Multi_Value_ToHoldingRegisters(slaveId, startAddress, DataType.UINT64, uint64s);

        public async Task<ulong> Read_Single_Uint64_FromHoldingRegisters(byte slaveId, ushort startAddress) => await Read_Single_Value_FromHoldingRegisters<ulong>(slaveId, startAddress, DataType.UINT64);

        public async Task<List<ulong>> Read_Multi_Uint64_FromHoldingRegisters(byte slaveId, ushort startAddress, ushort num) => await Read_Multi_Value_FromHoldingRegisters<ulong>(slaveId, startAddress, DataType.UINT64, num);


        public async Task Write_Single_Int64_ToHoldingRegisters(byte slaveId, ushort startAddress, string int64) => await Write_Single_Value_ToHoldingRegisters(slaveId, startAddress, DataType.INT64, int64);

        public async Task Write_Multi_Int64_ToHoldingRegisters(byte slaveId, ushort startAddress, List<string> int64s) => await Write_Multi_Value_ToHoldingRegisters(slaveId, startAddress, DataType.INT64, int64s);

        public async Task<long> Read_Single_Int64_FromHoldingRegisters(byte slaveId, ushort startAddress) => await Read_Single_Value_FromHoldingRegisters<long>(slaveId, startAddress, DataType.INT64);

        public async Task<List<long>> Read_Multi_Int64_FromHoldingRegisters(byte slaveId, ushort startAddress, ushort num) => await Read_Multi_Value_FromHoldingRegisters<long>(slaveId, startAddress, DataType.INT64, num);


        public async Task Write_Single_Float_ToHoldingRegisters(byte slaveId, ushort startAddress, string floatStr) => await Write_Single_Value_ToHoldingRegisters(slaveId, startAddress, DataType.FLOAT, floatStr);

        public async Task Write_Multi_Float_ToHoldingRegisters(byte slaveId, ushort startAddress, List<string> floatStrs) => await Write_Multi_Value_ToHoldingRegisters(slaveId, startAddress, DataType.FLOAT, floatStrs);

        public async Task<float> Read_Single_Float_FromHoldingRegisters(byte slaveId, ushort startAddress) => await Read_Single_Value_FromHoldingRegisters<float>(slaveId, startAddress, DataType.FLOAT);

        public async Task<List<float>> Read_Multi_Float_FromHoldingRegisters(byte slaveId, ushort startAddress, ushort num) => await Read_Multi_Value_FromHoldingRegisters<float>(slaveId, startAddress, DataType.FLOAT, num);


        public async Task Write_Single_Double_ToHoldingRegisters(byte slaveId, ushort startAddress, string doubleStr) => await Write_Single_Value_ToHoldingRegisters(slaveId, startAddress, DataType.DOUBLE, doubleStr);

        public async Task Write_Multi_Double_ToHoldingRegisters(byte slaveId, ushort startAddress, List<string> doubleStrs) => await Write_Multi_Value_ToHoldingRegisters(slaveId, startAddress, DataType.DOUBLE, doubleStrs);

        public async Task<double> Read_Single_Double_FromHoldingRegisters(byte slaveId, ushort startAddress) => await Read_Single_Value_FromHoldingRegisters<double>(slaveId, startAddress, DataType.DOUBLE);

        public async Task<List<double>> Read_Multi_Double_FromHoldingRegisters(byte slaveId, ushort startAddress, ushort num) => await Read_Multi_Value_FromHoldingRegisters<double>(slaveId, startAddress, DataType.DOUBLE, num);

        #endregion
    }
}
