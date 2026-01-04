using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Tools.Extension;
using ImTools;
using ModbusSimulator.Models;
using ModbusSimulator.Views;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using MessageBox = HandyControl.Controls.MessageBox;

namespace ModbusSimulator.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        IDialogService _dialogService;


        public DelegateCommand<string> OpenWindowCMD => new DelegateCommand<string>(OnOpenWindow);


        public MainWindowViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        private void OnOpenWindow(string type)
        {
            string window = null;
            IDialogParameters parameters = null;
            switch (type)
            {
                case "SerialMaster":
                    {
                        window = "UcMaster";
                        parameters = new DialogParameters()
                        {
                            { "Type", "Serial"}
                        };
                        break;
                    }
                case "TcpMaster":
                    {
                        window = "UcMaster";
                        parameters = new DialogParameters()
                        {
                            { "Type", "Tcp"}
                        };
                        break;
                    }

                case "SerialSlave":
                    {
                        window = "UcSlave";
                        parameters = new DialogParameters()
                        {
                            { "Type", "Serial"}
                        };
                        break;
                    }
                case "TcpSlave":
                    {
                        window = "UcSlave";
                        parameters = new DialogParameters()
                        {
                            { "Type", "Tcp"}
                        };
                        break;
                    }

            }
            _dialogService.Show(window, parameters, null);
        }


    }
}
