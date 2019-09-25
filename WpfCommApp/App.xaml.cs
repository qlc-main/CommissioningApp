using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Hellang.MessageBus;
using System.Windows.Threading;

namespace WpfCommApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // Initialize MessageBus Using Dispatcher
            Action<Action> uiThreadMarshaller =
                action => Dispatcher.Invoke(DispatcherPriority.Normal, action);
            Current.Properties["MessageBus"] =
                    new MessageBus(uiThreadMarshaller);
            Current.Properties["serial"] = new SerialComm();
            Current.Properties["pageState"] = new Dictionary<string, IPageViewModel>();
        }
    }
}
