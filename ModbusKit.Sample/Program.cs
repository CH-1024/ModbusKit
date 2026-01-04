using ModbusKit.Enums;
using ModbusKit.Utils;
using System.IO.Ports;
using System.Threading.Tasks;

namespace ModbusKit.Sample
{
    internal class Program
    {
        static ModbusKitSlave _slave;
        static ModbusKitMaster _master;

        static async Task Main(string[] args)
        {
            var endian = EndianOrder.BigEndian;
            var slaveId = (byte)1;
            var startingAddress = (ushort)10;

            var s_port = "COM55";
            var s_baudRate = 9600;
            var s_parity = Parity.None;
            var s_dataBits = 8;
            var s_stopBits = StopBits.One;
            _slave = ModbusKitSlave.CreateSerialSlave(slaveId, s_port, s_baudRate, s_parity, s_dataBits, s_stopBits);

            _slave.SetEndian(endian);

            _slave.StartListen();

            _slave.OnHoldingRegisterRequestReceived += OnHoldingRegisterRequestReceived;


            var m_port = "COM56";
            var m_baudRate = 9600;
            var m_parity = Parity.None;
            var m_dataBits = 8;
            var m_stopBits = StopBits.One;
            _master = ModbusKitMaster.CreateSerialMaster(m_port, m_baudRate, m_parity, m_dataBits, m_stopBits);

            _master.SetEndian(endian);

            await _master.Write_Single_Double_ToHoldingRegisters(slaveId, startingAddress, "123456");

            var masterRead = await _master.Read_Single_Double_FromHoldingRegisters(slaveId, startingAddress);

            await _slave.Write_Single_Double_ToHoldingRegisters(10, "654321");

            var slaveRead = await _slave.Read_Single_Double_FromHoldingRegisters(startingAddress);

        }

        private static void OnHoldingRegisterRequestReceived(StorageEventArgs<ushort> args)
        {
            Console.WriteLine($"Holding Register Request Received: Operation={args.Operation}, StartingAddress={args.StartingAddress}, Points=[{string.Join(", ", args.Points)}]");
        }
    }
}
