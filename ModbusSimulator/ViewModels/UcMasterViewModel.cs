using HandyControl.Data;
using HandyControl.Tools.Extension;
using ModbusKit.Enums;
using ModbusKit.Utils;
using ModbusSimulator.Views;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MessageBox = HandyControl.Controls.MessageBox;
using DataType = ModbusKit.Enums.DataType;
using ModbusSimulator.Models;

namespace ModbusSimulator.ViewModels
{
    public class UcMasterViewModel : BindableBase, IDialogAware
    {
        CancellationTokenSource _continueCTS = new CancellationTokenSource();
        ModbusKitMaster _master;


        private string type;
        public string Type
        {
            get { return type; }
            set { SetProperty(ref type, value); }
        }

        private string color = "OrangeRed";
        public string Color
        {
            get { return color; }
            set { SetProperty(ref color, value); }
        }

        private bool isContinue;
        public bool IsContinue
        {
            get { return isContinue; }
            set { SetProperty(ref isContinue, value); }
        }

        private EndianOrder? selectedEndian;
        public EndianOrder? SelectedEndian
        {
            get { return selectedEndian; }
            set { SetProperty(ref selectedEndian, value); }
        }

        #region Serial

        private string[] ports;
        public string[] Ports
        {
            get { return ports; }
            set { SetProperty(ref ports, value); }
        }

        private string selectedPort;
        public string SelectedPort
        {
            get { return selectedPort; }
            set { SetProperty(ref selectedPort, value); }
        }

        private int selectedBaudRate;
        public int SelectedBaudRate
        {
            get { return selectedBaudRate; }
            set { SetProperty(ref selectedBaudRate, value); }
        }

        private string selectedParity;
        public string SelectedParity
        {
            get { return selectedParity; }
            set { SetProperty(ref selectedParity, value); }
        }

        private string selectedDataBits;
        public string SelectedDataBits
        {
            get { return selectedDataBits; }
            set { SetProperty(ref selectedDataBits, value); }
        }

        private string selectedStopBits;
        public string SelectedStopBits
        {
            get { return selectedStopBits; }
            set { SetProperty(ref selectedStopBits, value); }
        }

        #endregion

        #region Tcp

        private string ip = "127.0.0.1";
        public string IP
        {
            get { return ip; }
            set { SetProperty(ref ip, value); }
        }

        private int? port;
        public int? Port
        {
            get { return port; }
            set { SetProperty(ref port, value); }
        }

        #endregion

        private string selectedMode;
        public string SelectedMode
        {
            get { return selectedMode; }
            set { SetProperty(ref selectedMode, value, InputChanged); }
        }

        private byte? slaveId = 1;
        public byte? SlaveId
        {
            get { return slaveId; }
            set { SetProperty(ref slaveId, value); }
        }

        private ushort? startAddress;
        public ushort? StartAddress
        {
            get { return startAddress; }
            set { SetProperty(ref startAddress, value, InputChanged); }
        }

        public Array DataTypeValues => Enum.GetValues(typeof(DataType));

        private DataType? selectedType;
        public DataType? SelectedType
        {
            get { return selectedType; }
            set { SetProperty(ref selectedType, value, InputChanged); }
        }

        private ushort? numRegisters;
        public ushort? NumRegisters
        {
            get { return numRegisters; }
            set { SetProperty(ref numRegisters, value, InputChanged); }
        }

        private ObservableCollection<ItemModel> items;
        public ObservableCollection<ItemModel> Items
        {
            get { return items; }
            set { SetProperty(ref items, value); }
        }

        private ObservableCollection<string> logCollection = new ObservableCollection<string>();
        public ObservableCollection<string> LogCollection
        {
            get { return logCollection; }
            set { SetProperty(ref logCollection, value); }
        }


        public DelegateCommand RefreshCMD => new DelegateCommand(OnRefresh);
        public DelegateCommand ConnectCMD => new DelegateCommand(OnConnect);
        public DelegateCommand DisconnectCMD => new DelegateCommand(OnDisconnect);
        public DelegateCommand ExecuteCMD => new DelegateCommand(async () => await OnExecute());


        public UcMasterViewModel()
        {
            Ports = SerialPort.GetPortNames();

            Task.Run(ContinueExecute);
        }

        #region 弹窗
        public string Title { get; set; }
        public event Action<IDialogResult> RequestClose;
        public bool CanCloseDialog()
        {
            return true;
        }
        public void OnDialogClosed()
        {
            OnDisconnect();
            _continueCTS.Cancel();
        }
        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.ContainsKey("Type"))
            {
                Type = parameters.GetValue<string>("Type");
                Title = $"{Type} Master";
            }
        }
        private void RaiseRequestClose(IDialogResult dialogResult)
        {
            RequestClose?.Invoke(dialogResult);
        }
        #endregion






        private void InputChanged()
        {
            if (!SelectedMode.IsNullOrEmpty() && SelectedType.HasValue && NumRegisters.HasValue && StartAddress.HasValue)
            {
                var size = GetTypeSize(SelectedType.Value);
                var list = new string[NumRegisters.Value];
                Items = new ObservableCollection<ItemModel>(list.Select((v, i) => new ItemModel((int)(i * size + StartAddress), v)));
            }
            else
            {
                Items = null;
            }
        }







        private void OnRefresh()
        {
            Ports = SerialPort.GetPortNames();
        }

        private void OnConnect()
        {
            if (!SlaveId.HasValue || !SelectedEndian.HasValue)
            {
                var info = new MessageBoxInfo
                {
                    Message = "请填写 从站地址 和 大小端",
                    Caption = "错误",
                    Button = MessageBoxButton.OK,
                    Icon = null,
                };
                MessageBox.Show(info);
                return;
            }
            if (_master != null)
            {
                var info = new MessageBoxInfo
                {
                    Message = "已连接",
                    Caption = "错误",
                    Button = MessageBoxButton.OK,
                    Icon = null,
                };
                MessageBox.Show(info);
                return;
            }

            OnDisconnect();

            try
            {
                if (Type == "Serial")
                {
                    var port = selectedPort;
                    var baudRate = SelectedBaudRate;
                    var parity = (Parity)Enum.Parse(typeof(Parity), SelectedParity);
                    var dataBits = int.Parse(SelectedDataBits);
                    var stopBits = (StopBits)Enum.Parse(typeof(StopBits), SelectedStopBits);
                    _master = ModbusKitMaster.CreateSerialMaster(port, baudRate, parity, dataBits, stopBits);
                }

                if (Type == "Tcp")
                {
                    var ip = IP;
                    var port = Port.Value;
                    _master = ModbusKitMaster.CreateTcpMaster(ip, port);
                }

                _master.SetEndian(SelectedEndian.Value);
            }
            catch (Exception e)
            {
                var info = new MessageBoxInfo
                {
                    Message = e.Message,
                    Caption = "错误",
                    Button = MessageBoxButton.OK,
                    Icon = null,
                };
                MessageBox.Show(info);
                return;
            }

            Color = "LimeGreen";
        }

        private void OnDisconnect()
        {
            _master?.Dispose();
            _master = null;
            Color = "OrangeRed";
        }

        private async void ContinueExecute()
        {
            try
            {
                while (true)
                {
                    _continueCTS.Token.ThrowIfCancellationRequested();

                    if (IsContinue)
                    {
                        if (CanExecute())
                        {
                            try
                            {
                                await OnExecute();
                            }
                            catch (Exception)
                            {
                                IsContinue = false;
                            }
                        }
                        else
                        {
                            IsContinue = false;
                        }
                    }

                    await Task.Delay(100, _continueCTS.Token);
                }
            }
            catch (Exception)
            {
            }
        }

        private async Task OnExecute()
        {
            if (!CanExecute()) return;

            try
            {
                if (SelectedMode == "读")
                {
                    var values = await _master.Read_Multi_Value_FromHoldingRegisters(SlaveId.Value, StartAddress.Value, SelectedType.Value, NumRegisters.Value);

                    for (int i = 0; i < Items.Count; i++) Items[i].Value = values[i];
                }
                else if (SelectedMode == "写")
                {
                    var values = Items.Select(i => i.Value).ToList();

                    await _master.Write_Multi_Value_ToHoldingRegisters(SlaveId.Value, StartAddress.Value, SelectedType.Value, values);
                }
            }
            catch (Exception e)
            {
                var info = new MessageBoxInfo
                {
                    Message = e.Message,
                    Caption = "错误",
                    Button = MessageBoxButton.OK,
                    Icon = null,
                };
                MessageBox.Show(info);

                if (IsContinue) IsContinue = false;
            }
        }








        private void Log(string msg)
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                // 如果在UI线程
                LogCollection.Insert(0, msg);
                if (LogCollection.Count > 500)
                {
                    LogCollection.RemoveAt(500);
                }
            }
            else
            {
                // 如果在后台线程
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    LogCollection.Insert(0, msg);
                    if (LogCollection.Count > 500)
                    {
                        LogCollection.RemoveAt(500);
                    }
                }));
            }
        }

        private bool CanExecute()
        {
            if (_master == null)
            {
                var info = new MessageBoxInfo
                {
                    Message = "请先连接",
                    Caption = "提示",
                    Button = MessageBoxButton.OK,
                    Icon = null,
                };
                MessageBox.Show(info);
                return false;
            }
            if (SelectedMode == null || SlaveId == null || StartAddress == null || SelectedType == null || NumRegisters == null)
            {
                var info = new MessageBoxInfo
                {
                    Message = "请填写参数",
                    Caption = "提示",
                    Button = MessageBoxButton.OK,
                    Icon = null,
                };
                MessageBox.Show(info);
                return false;
            }

            return true;
        }

        private ushort GetTypeSize(DataType type)
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

    }
}
