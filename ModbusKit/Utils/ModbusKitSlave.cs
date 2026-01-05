using ModbusKit.Enums;
using NModbus;
using NModbus.Device;
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
    public class ModbusKitSlave : IDisposable
    {
        SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        CancellationTokenSource _cts;
        public bool IsListening { get; private set; }

        int _delay = 0;
        EndianOrder _endian = EndianOrder.BigEndian;

        SlaveStorage _dataStore;
        IModbusSlave _slave;
        IModbusSlaveNetwork _slaveNetwork;

        public Action<StorageEventArgs<bool>> OnCoilDiscreteRequestReceived { get; set; }
        public Action<StorageEventArgs<bool>> OnCoilInputRequestReceived { get; set; }
        public Action<StorageEventArgs<ushort>> OnInputRegisterRequestReceived { get; set; }
        public Action<StorageEventArgs<ushort>> OnHoldingRegisterRequestReceived { get; set; }
        public Action<Exception> OnDisconnect { get; set; }



        public ModbusKitSlave()
        {

        }


        public static ModbusKitSlave CreateSerialSlave(byte slaveId, string port, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var slaveSerialPort = new SerialPort(port, baudRate, parity, dataBits, stopBits)
            {
                Handshake = Handshake.None,
                ReadTimeout = 500,
                WriteTimeout = 500,
                Encoding = Encoding.ASCII
            };
            slaveSerialPort.Open();

            var factory = new ModbusFactory();
            var dataStore = new SlaveStorage();
            var slaveNetwork = factory.CreateRtuSlaveNetwork(slaveSerialPort);
            var slave = factory.CreateSlave(slaveId, dataStore);
            slaveNetwork.AddSlave(slave);

            var modbusKitSlave = new ModbusKitSlave();
            modbusKitSlave._slave = slave;
            modbusKitSlave._slaveNetwork = slaveNetwork;
            modbusKitSlave._dataStore = dataStore;

            return modbusKitSlave;
        }


        public static ModbusKitSlave CreateTcpSlave(byte slaveId, IPAddress ip, int port)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            TcpListener slaveTcpListener = new TcpListener(ip, port);
            slaveTcpListener.Start();

            var factory = new ModbusFactory();
            var dataStore = new SlaveStorage();
            var slaveNetwork = factory.CreateSlaveNetwork(slaveTcpListener);
            var slave = factory.CreateSlave(slaveId, dataStore);
            slaveNetwork.AddSlave(slave);

            var modbusKitSlave = new ModbusKitSlave();
            modbusKitSlave._slave = slave;
            modbusKitSlave._slaveNetwork = slaveNetwork;
            modbusKitSlave._dataStore = dataStore;

            return modbusKitSlave;
        }


        public static ModbusKitSlave CreateUdpSlave(byte slaveId, int port)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var client = new UdpClient(port);

            var factory = new ModbusFactory();
            var dataStore = new SlaveStorage();
            var slaveNetwork = factory.CreateSlaveNetwork(client);
            var slave = factory.CreateSlave(slaveId, dataStore);
            slaveNetwork.AddSlave(slave);

            var modbusKitSlave = new ModbusKitSlave();
            modbusKitSlave._slave = slave;
            modbusKitSlave._slaveNetwork = slaveNetwork;
            modbusKitSlave._dataStore = dataStore;

            return modbusKitSlave;
        }


        public void StartListen()
        {
            IsListening = true;

            Task.Run(async () =>
            {
                Exception err = null;
                _cts = new CancellationTokenSource();
                try
                {
                    _dataStore.CoilDiscretes.StorageOperationOccurred += CoilDiscretesRequestReceived;
                    _dataStore.CoilInputs.StorageOperationOccurred += CoilInputsRequestReceived;
                    _dataStore.InputRegisters.StorageOperationOccurred += InputRegistersRequestReceived;
                    _dataStore.HoldingRegisters.StorageOperationOccurred += HoldingRegistersRequestReceived;

                    await _slaveNetwork.ListenAsync(_cts.Token);
                }
                catch (Exception e)
                {
                    err = e;
                }
                finally
                {
                    _dataStore.CoilDiscretes.StorageOperationOccurred -= CoilDiscretesRequestReceived;
                    _dataStore.CoilInputs.StorageOperationOccurred -= CoilInputsRequestReceived;
                    _dataStore.InputRegisters.StorageOperationOccurred -= InputRegistersRequestReceived;
                    _dataStore.HoldingRegisters.StorageOperationOccurred -= HoldingRegistersRequestReceived;

                    Disconnect(err);
                }
            }).ContinueWith(t => IsListening = false);
        }

        public void Stop()
        {
            _cts.Cancel();
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
            _slaveNetwork?.Dispose();
            _slaveNetwork = null;
            _dataStore = null;
            _slave = null;
            _cts?.Dispose();
            _cts = null;
            _semaphoreSlim?.Dispose();
            _semaphoreSlim = null;
        }





        private void CoilDiscretesRequestReceived(object sender, StorageEventArgs<bool> args)
        {
            OnCoilDiscreteRequestReceived?.Invoke(args);
        }

        private void CoilInputsRequestReceived(object sender, StorageEventArgs<bool> args)
        {
            OnCoilInputRequestReceived?.Invoke(args);
        }

        private void InputRegistersRequestReceived(object sender, StorageEventArgs<ushort> args)
        {
            OnInputRegisterRequestReceived?.Invoke(args);
        }

        private void HoldingRegistersRequestReceived(object sender, StorageEventArgs<ushort> args)
        {
            OnHoldingRegisterRequestReceived?.Invoke(args);
        }

        private void Disconnect(Exception e)
        {
            OnDisconnect?.Invoke(e);
        }












        private async Task<ushort[]> SafeReadMultipleHoldingRegistersAsync(ushort startAddress, ushort numberOfPoints)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                // 执行受保护的读取操作
                //var result = _slave.DataStore.HoldingRegisters.Skip(startAddress + 1).Take(numberOfPoints).ToArray();
                var result = _slave.DataStore.HoldingRegisters.ReadPoints(startAddress, numberOfPoints);
                // 读取完成后等待间隔
                await Task.Delay(_delay);
                return result;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private async Task SafeWriteMultipleHoldingRegistersAsync(ushort startAddress, ushort[] allRegisters)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                // 执行受保护的写入操作
                //for (int i = 0; i < allRegisters.Length; i++) _slave.DataStore.HoldingRegisters[startAddress + 1 + i] = allRegisters[i];
                _slave.DataStore.HoldingRegisters.WritePoints(startAddress, allRegisters);
                // 写入完成后等待间隔
                await Task.Delay(_delay);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }





        #region 封装层

        public async Task Write_Single_Value_ToHoldingRegisters(ushort startAddress, DataType type, string value)
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

            await SafeWriteMultipleHoldingRegistersAsync(startAddress, allRegisters);
        }

        public async Task Write_Multi_Value_ToHoldingRegisters(ushort startAddress, DataType type, List<string> values)
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

            await SafeWriteMultipleHoldingRegistersAsync(startAddress, allRegisters);
        }

        public async Task<string> Read_Single_Value_FromHoldingRegisters(ushort startAddress, DataType type)
        {
            ushort size = ModbusDataHelper.GetTypeSize(type);

            ushort[] allRegisters = await SafeReadMultipleHoldingRegistersAsync(startAddress, size);

            var dic = ModbusDataHelper.ConvertRegistersToBytes(type, allRegisters, _endian);

            return ModbusDataHelper.ConvertBytesToValue(dic[0], type);
        }

        public async Task<List<string>> Read_Multi_Value_FromHoldingRegisters(ushort startAddress, DataType type, ushort num)
        {
            ushort size = ModbusDataHelper.GetTypeSize(type);

            ushort[] allRegisters = await SafeReadMultipleHoldingRegistersAsync(startAddress, (ushort)(size * num));

            var dic = ModbusDataHelper.ConvertRegistersToBytes(type, allRegisters, _endian);

            var list = new List<string>();
            for (ushort i = 0; i < num; i++)
            {
                list.Add(ModbusDataHelper.ConvertBytesToValue(dic[i], type));
            }

            return list;
        }

        public async Task<T> Read_Single_Value_FromHoldingRegisters<T>(ushort startAddress, DataType type)
        {
            ushort size = ModbusDataHelper.GetTypeSize(type);

            ushort[] allRegisters = await SafeReadMultipleHoldingRegistersAsync(startAddress, size);

            var dic = ModbusDataHelper.ConvertRegistersToBytes(type, allRegisters, _endian);

            return ModbusDataHelper.ConvertBytesToValue<T>(dic[0], type);
        }

        public async Task<List<T>> Read_Multi_Value_FromHoldingRegisters<T>(ushort startAddress, DataType type, ushort num)
        {
            ushort size = ModbusDataHelper.GetTypeSize(type);

            ushort[] allRegisters = await SafeReadMultipleHoldingRegistersAsync(startAddress, (ushort)(size * num));

            var dic = ModbusDataHelper.ConvertRegistersToBytes(type, allRegisters, _endian);

            var list = new List<T>();
            for (ushort i = 0; i < num; i++)
            {
                list.Add(ModbusDataHelper.ConvertBytesToValue<T>(dic[i], type));
            }

            return list;
        }


        public async Task Write_Multi_ASCII_ToHoldingRegisters(ushort startAddress, string ascii)
        {
            var result = Enumerable.Range(0, ascii.Length / 2 + ascii.Length % 2).Select(i => ascii.Substring(i * 2, Math.Min(2, ascii.Length - i * 2))).ToList();
            await Write_Multi_Value_ToHoldingRegisters(startAddress, DataType.ASCII, result);
        }

        public async Task<string> Read_Multi_ASCII_FromHoldingRegisters(ushort startAddress, ushort num)
        {
            var result = await Read_Multi_Value_FromHoldingRegisters<string>(startAddress, DataType.ASCII, num);
            return string.Join("", result);
        }

        public async Task Write_Multi_GBK_ToHoldingRegisters(ushort startAddress, string gbk)
        {
            var result = Enumerable.Range(0, gbk.Length).Select(i => gbk.Substring(i, 1)).ToList();
            await Write_Multi_Value_ToHoldingRegisters(startAddress, DataType.GBK, result);
        }

        public async Task<string> Read_Multi_GBK_FromHoldingRegisters(ushort startAddress, ushort num)
        {
            var result = await Read_Multi_Value_FromHoldingRegisters<string>(startAddress, DataType.GBK, num);
            return string.Join("", result);
        }


        public async Task Write_Single_Uint16_ToHoldingRegisters(ushort startAddress, string uint16) => await Write_Single_Value_ToHoldingRegisters(startAddress, DataType.UINT16, uint16);

        public async Task Write_Multi_Uint16_ToHoldingRegisters(ushort startAddress, List<string> uint16s) => await Write_Multi_Value_ToHoldingRegisters(startAddress, DataType.UINT16, uint16s);

        public async Task<ushort> Read_Single_Uint16_FromHoldingRegisters(ushort startAddress) => await Read_Single_Value_FromHoldingRegisters<ushort>(startAddress, DataType.UINT16);

        public async Task<List<ushort>> Read_Multi_Uint16_FromHoldingRegisters(ushort startAddress, ushort num) => await Read_Multi_Value_FromHoldingRegisters<ushort>(startAddress, DataType.UINT16, num);


        public async Task Write_Single_Int16_ToHoldingRegisters(ushort startAddress, string int16) => await Write_Single_Value_ToHoldingRegisters(startAddress, DataType.INT16, int16);

        public async Task Write_Multi_Int16_ToHoldingRegisters(ushort startAddress, List<string> int16s) => await Write_Multi_Value_ToHoldingRegisters(startAddress, DataType.INT16, int16s);

        public async Task<short> Read_Single_Int16_FromHoldingRegisters(ushort startAddress) => await Read_Single_Value_FromHoldingRegisters<short>(startAddress, DataType.INT16);

        public async Task<List<short>> Read_Multi_Int16_FromHoldingRegisters(ushort startAddress, ushort num) => await Read_Multi_Value_FromHoldingRegisters<short>(startAddress, DataType.INT16, num);


        public async Task Write_Single_Uint32_ToHoldingRegisters(ushort startAddress, string uint32) => await Write_Single_Value_ToHoldingRegisters(startAddress, DataType.UINT32, uint32);

        public async Task Write_Multi_Uint32_ToHoldingRegisters(ushort startAddress, List<string> uint32s) => await Write_Multi_Value_ToHoldingRegisters(startAddress, DataType.UINT32, uint32s);

        public async Task<uint> Read_Single_Uint32_FromHoldingRegisters(ushort startAddress) => await Read_Single_Value_FromHoldingRegisters<uint>(startAddress, DataType.UINT32);

        public async Task<List<uint>> Read_Multi_Uint32_FromHoldingRegisters(ushort startAddress, ushort num) => await Read_Multi_Value_FromHoldingRegisters<uint>(startAddress, DataType.UINT32, num);


        public async Task Write_Single_Int32_ToHoldingRegisters(ushort startAddress, string int32) => await Write_Single_Value_ToHoldingRegisters(startAddress, DataType.INT32, int32);

        public async Task Write_Multi_Int32_ToHoldingRegisters(ushort startAddress, List<string> int32s) => await Write_Multi_Value_ToHoldingRegisters(startAddress, DataType.INT32, int32s);

        public async Task<int> Read_Single_Int32_FromHoldingRegisters(ushort startAddress) => await Read_Single_Value_FromHoldingRegisters<int>(startAddress, DataType.INT32);

        public async Task<List<int>> Read_Multi_Int32_FromHoldingRegisters(ushort startAddress, ushort num) => await Read_Multi_Value_FromHoldingRegisters<int>(startAddress, DataType.INT32, num);


        public async Task Write_Single_Uint64_ToHoldingRegisters(ushort startAddress, string uint64) => await Write_Single_Value_ToHoldingRegisters(startAddress, DataType.UINT64, uint64);

        public async Task Write_Multi_Uint64_ToHoldingRegisters(ushort startAddress, List<string> uint64s) => await Write_Multi_Value_ToHoldingRegisters(startAddress, DataType.UINT64, uint64s);

        public async Task<ulong> Read_Single_Uint64_FromHoldingRegisters(ushort startAddress) => await Read_Single_Value_FromHoldingRegisters<ulong>(startAddress, DataType.UINT64);

        public async Task<List<ulong>> Read_Multi_Uint64_FromHoldingRegisters(ushort startAddress, ushort num) => await Read_Multi_Value_FromHoldingRegisters<ulong>(startAddress, DataType.UINT64, num);


        public async Task Write_Single_Int64_ToHoldingRegisters(ushort startAddress, string int64) => await Write_Single_Value_ToHoldingRegisters(startAddress, DataType.INT64, int64);

        public async Task Write_Multi_Int64_ToHoldingRegisters(ushort startAddress, List<string> int64s) => await Write_Multi_Value_ToHoldingRegisters(startAddress, DataType.INT64, int64s);

        public async Task<long> Read_Single_Int64_FromHoldingRegisters(ushort startAddress) => await Read_Single_Value_FromHoldingRegisters<long>(startAddress, DataType.INT64);

        public async Task<List<long>> Read_Multi_Int64_FromHoldingRegisters(ushort startAddress, ushort num) => await Read_Multi_Value_FromHoldingRegisters<long>(startAddress, DataType.INT64, num);


        public async Task Write_Single_Float_ToHoldingRegisters(ushort startAddress, string floatStr) => await Write_Single_Value_ToHoldingRegisters(startAddress, DataType.FLOAT, floatStr);

        public async Task Write_Multi_Float_ToHoldingRegisters(ushort startAddress, List<string> floatStrs) => await Write_Multi_Value_ToHoldingRegisters(startAddress, DataType.FLOAT, floatStrs);

        public async Task<float> Read_Single_Float_FromHoldingRegisters(ushort startAddress) => await Read_Single_Value_FromHoldingRegisters<float>(startAddress, DataType.FLOAT);

        public async Task<List<float>> Read_Multi_Float_FromHoldingRegisters(ushort startAddress, ushort num) => await Read_Multi_Value_FromHoldingRegisters<float>(startAddress, DataType.FLOAT, num);


        public async Task Write_Single_Double_ToHoldingRegisters(ushort startAddress, string doubleStr) => await Write_Single_Value_ToHoldingRegisters(startAddress, DataType.DOUBLE, doubleStr);

        public async Task Write_Multi_Double_ToHoldingRegisters(ushort startAddress, List<string> doubleStrs) => await Write_Multi_Value_ToHoldingRegisters(startAddress, DataType.DOUBLE, doubleStrs);

        public async Task<double> Read_Single_Double_FromHoldingRegisters(ushort startAddress) => await Read_Single_Value_FromHoldingRegisters<double>(startAddress, DataType.DOUBLE);

        public async Task<List<double>> Read_Multi_Double_FromHoldingRegisters(ushort startAddress, ushort num) => await Read_Multi_Value_FromHoldingRegisters<double>(startAddress, DataType.DOUBLE, num);


        #endregion

    }

}
