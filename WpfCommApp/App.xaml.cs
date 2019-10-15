using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Hellang.MessageBus;
using System.Windows.Threading;
using System.Collections.ObjectModel;

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
            Current.Properties["serial"] = new ObservableCollection<SerialComm>() { new SerialComm() };
            Current.Properties["meters"] = new ObservableCollection<Meter>() { new Meter() };
        }
    }

    public class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        public object Data
        {
            get { return (object) GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Data.
        // This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object),
            typeof(BindingProxy), new UIPropertyMetadata(null));
    }

    
}
