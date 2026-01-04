using ModbusSimulator.Views;
using ModbusSimulator.ViewModels;
using Prism.Ioc;
using System.Text;
using System.Windows;

namespace ModbusSimulator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<MainWindow, MainWindowViewModel>();
            containerRegistry.RegisterForNavigation<UcMaster, UcMasterViewModel>();
            containerRegistry.RegisterForNavigation<UcSlave, UcSlaveViewModel>();
        }
    }
}
