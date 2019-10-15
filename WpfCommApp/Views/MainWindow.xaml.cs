using System;
using System.Collections.Generic;
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

namespace WpfCommApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IHandle<ScreenComplete>
    {
        public static MainWindow Current { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = new MainViewModel();

            (Application.Current.Properties["MessageBus"] as MessageBus).Subscribe(this);

            Current = this;
        }

        public void Handle(ScreenComplete message)
        {
            Current.Dispatcher.Invoke((Action)(() =>
            {
                MainViewModel m = (DataContext as MainViewModel);

                if (message.Command == "cont")
                    m.ForwardPage.Execute(null);
                else if (message.Command == "disable")
                    m.ForwardEnabled = false;
                else if (message.Command == "switch")
                {
                    string serialNo = m.Meters.Last().ID;
                    m.Tabs.Add(new ContentTab(m.Tabs.Count - 1, serialNo));
                    m.TabIndex = m.Tabs.Count - 1;
                    m.BackwardEnabled = true;
                    m.ForwardEnabled = true;
                }
                else
                    m.ForwardEnabled = true;
            }));
        }

        private void Meter_Filter(object sender, System.Windows.Data.FilterEventArgs e)
        {
            if ((e.Item as Meter).Viewing == false)
                e.Accepted = false;
        }
    }
}
