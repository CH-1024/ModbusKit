ModbusKit 是一个基于 .NET Standard 2.0 的 Modbus 协议工具库，主要功能和要求如下：
要求：
•	依赖 NModbus 和 NModbus.Serial（3.x 版本）
•	支持 .NET Standard 2.0，可用于 .NET Core、.NET Framework 及 .NET 5/6/7/8 等平台
•	需要 System.IO.Ports 和 System.Text.Encoding.CodePages 支持串口和编码
主要功能：
•	支持 Modbus RTU、TCP、UDP 主站（Master）和从站（Slave）通信
•	提供串口和网络（TCP/UDP）方式的主站/从站创建方法
•	支持大端/小端字节序设置
•	支持读写线圈、离散量、输入寄存器、保持寄存器等常用 Modbus 功能
•	支持异步操作和线程安全
•	提供事件回调，便于监控寄存器请求
适合用于工业自动化、设备通信等场景的 Modbus 协议开发。