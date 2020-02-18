using System;
using System.Collections.Generic;
using System.Windows;
using Hellang.MessageBus;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.IO;

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
            Current.Properties["serial"] = new Dictionary<string, SerialComm>();
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
            Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(AppUnhandledException);
        }

        private void AppUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
#if DEBUG
            e.Handled = false;
#else
            HandleException(e);
#endif
        }

        private void HandleException(DispatcherUnhandledExceptionEventArgs e)
        {
            string dir = String.Format("{0}\\ToUpload", Directory.GetCurrentDirectory());
            string logDir = String.Format("{0}\\Logs", Directory.GetCurrentDirectory());
            foreach (Meter m in (Current.Properties["meters"] as Dictionary<string, Meter>).Values)
                m.Save(dir);

            string errorMessage = string.Format("Application error occurred. Here are the details: {0}",
                e.Exception.Message + (e.Exception.InnerException != null ? "\n" +
                e.Exception.InnerException.Message : null));

            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            StreamWriter sw = new StreamWriter(String.Format("{0}\\ApplicationError_{1}", logDir, DateTime.Now));
            sw.Write(errorMessage);
            sw.Close();

            e.Handled = true;
            MessageBox.Show("Application encountered fatal error, data has been saved.\n\nPress OK to exit!", "Application Error", MessageBoxButton.OK);
            Current.Shutdown();
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
