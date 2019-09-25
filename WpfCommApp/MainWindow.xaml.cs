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
                (DataContext as MainViewModel).ForwardEnabled = true;
            }));
        }
    }
}
