using System;
using System.Collections.Generic;
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

            // These are the 3 application wide variables, a message bus for sending messages back to the MainWindow
            Current.Properties["MessageBus"] =
                    new MessageBus(uiThreadMarshaller);
            // Collection of active serial objects
            Current.Properties["serial"] = new ObservableCollection<SerialComm>() { new SerialComm() };
            // Collection of meter objects that have been imported or created during program execution
            Current.Properties["meters"] = new Dictionary<string, Meter>();
            // CT Types available for commissioning
            Current.Properties["cttypes"] = new string[3] { "flex", "solid", "split" };
            // Dispositions options available to the user
            Current.Properties["dispositions"] = new Dictionary<string, int>()
                                                    { { "No Problem Found", 10},
                                                      { "Wrong Site Wiring", 12},
                                                      { "Follow Up Required", 75},
                                                      { "Follow Up Resolved", 113},
                                                      { "No Meter Communication", 41},
                                                      { "Reversed CT(s)", 78},
                                                      { "Reversed Phase(s)", 79}
                                                    };
        }
    }

    /// <summary>
    /// This class is used to create bindings to specific elements in the XAML
    /// </summary>
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
