using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Hellang.MessageBus;
using WpfCommApp.Helpers;

namespace WpfCommApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IHandle<MessageCenter>
    {
        public static MainWindow Current { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = new MainViewModel();

            (Application.Current.Properties["MessageBus"] as MessageBus).Subscribe(this);

            Current = this;

#if DEBUG
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
#endif
        }

        public void Handle(MessageCenter message)
        {
            Current.Dispatcher.Invoke((Action)(() =>
            {
                MainViewModel m = (DataContext as MainViewModel);

                if (message.Command == "backward")
                    m.BackwardPage.Execute(null);
                else if (message.Command == "disableFwd")
                    m.ForwardEnabled = false;
                else if (message.Command == "newTab")
                    m.CreateTab(message.Args as Tuple<string, string>);
                else if (message.Command == "closeTab")
                    m.CloseTab(message.Args as Tuple<string, string>, true);
                else if (message.Command == "switchMeters")
                    Globals.Tasker.Run(m.SwitchMeters);
                else if (message.Command == "newMeter")
                    m.CreateNewMeter();
                else
                    m.ForwardEnabled = true;
            }));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ((e.Source as MainWindow).DataContext as MainViewModel).ImportMeters.Execute(null);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ((e.Source as MainWindow).DataContext as MainViewModel).ResizeControl.Execute(e);
        }
    }
}
