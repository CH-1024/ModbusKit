
# ModbusKit

ModbusKit 是一个轻量、易用的 .NET 库，支持通过Serial、TCP 和 UDP 协议实现 Modbus 主站（Master）与从站（Slave）通信。它提供了简洁的 API，方便快速集成到各种应用中。

// Master Example
```
public class UcMasterViewModel : BindableBase
{
    ModbusKitMaster _master;
    string Type;

    private void Connect()
    {
        try
        {
            if (Type == "Serial")
            {
                _master = ModbusKitMaster.CreateSerialMaster(port, baudRate, parity, dataBits, stopBits);
            }

            if (Type == "Tcp")
            {
                _master = ModbusKitMaster.CreateTcpMaster(ip, port);
            }

            if (Type == "Udp")
            {
                _master = ModbusKitMaster.CreateUdpMaster(ip, port);
            }

            _master.SetEndian(EndianOrder.BigEndian);
            _master.SetDelay(20);
        }
        catch (Exception e)
        {
        }
    }

    private async Task Send()
    {
        await _master.Write_Single_Int32_ToHoldingRegisters(slaveId, address1, "123");
        int v1 = await _master.Read_Single_Int32_FromHoldingRegisters(slaveId, address1);

        await _master.Write_Multi_Int32_ToHoldingRegisters(slaveId, address2, ["1234", "2345", "3456"]);
        List<int> v2 = await _master.Read_Multi_Int32_FromHoldingRegisters(slaveId, address2, 3);

        await _master.Write_Multi_ASCII_ToHoldingRegisters(slaveId, address3, "ASCII String");
        string str = await _master.Read_Multi_ASCII_FromHoldingRegisters(slaveId, address3, 50);    // "ASCII String"
    }
}
```

// Slave Example
```
public class UcSlaveViewModel : BindableBase
{
    ModbusKitSlave _slave;
    string Type;


    private void OnHoldingRegisterRequestReceived(StorageEventArgs<ushort> args)
    {
    }

    private void OnDisconnect(Exception e)
    {
        if (e != null) Log(e.Message);

        if (_slave != null)
        {
            _slave.OnHoldingRegisterRequestReceived -= OnHoldingRegisterRequestReceived;
            _slave.OnDisconnect -= OnDisconnect;
        }

        _slave?.Dispose();
        _slave = null;
    }

    private void Listen()
    {
        if (_slave?.IsListening == true)
        {
            return;
        }

        try
        {
            if (Type == "Serial")
            {
                _slave = ModbusKitSlave.CreateSerialSlave(slaveId, port, baudRate, parity, dataBits, stopBits);
            }

            if (Type == "Tcp")
            {
                _slave = ModbusKitSlave.CreateTcpSlave(slaveId, ip, port);
            }

            if (Type == "Udp")
            {
                _slave = ModbusKitSlave.CreateUdpSlave(slaveId, port);
            }

            _slave.SetEndian(EndianOrder.BigEndian);
            _slave.SetDelay(0);

            _slave.OnHoldingRegisterRequestReceived += OnHoldingRegisterRequestReceived;
            _slave.OnDisconnect += OnDisconnect;

            _slave.StartListen();
        }
        catch (Exception e)
        {
        }
    }

    private void Close()
    {
        _slave?.Stop();
    }

    private async Task Send()
    {
        await _slave.Write_Single_Int32_ToHoldingRegisters(address1, "123");
        int v1 = await _slave.Read_Single_Int32_FromHoldingRegisters(address1);

        await _slave.Write_Multi_Int32_ToHoldingRegisters(address2, ["1234", "2345", "3456"]);
        List<int> v2 = await _slave.Read_Multi_Int32_FromHoldingRegisters(address2, 3);

        await _slave.Write_Multi_ASCII_ToHoldingRegisters(address3, "ASCII String");
        string str = await _slave.Read_Multi_ASCII_FromHoldingRegisters(address3, 50);    // "ASCII String"
    }
}
```
