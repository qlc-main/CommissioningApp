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

namespace WpfCommApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IHandle<ScreenComplete>
    {
        public static MainWindow Current { get; private set; }

        private bool _refreshViewTabFilter;
        private bool _refreshMenuTabFilter;

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = new MainViewModel();

            (Application.Current.Properties["MessageBus"] as MessageBus).Subscribe(this);

            Current = this;

            _refreshViewTabFilter = true;
            _refreshMenuTabFilter = true;

#if DEBUG
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
#endif
        }

        public void Handle(ScreenComplete message)
        {
            Current.Dispatcher.Invoke((Action)(() =>
            {
                MainViewModel m = (DataContext as MainViewModel);

                if (message.Command == "cont")
                    m.ForwardPage.Execute(null);
                else if (message.Command == "backward")
                    m.BackwardPage.Execute(null);
                else if (message.Command == "disable")
                    m.ForwardEnabled = false;
                else if (message.Command == "newTab")
                    m.CreateTab(message.Args as Tuple<int, string>);
                else if (message.Command == "switchMeters")
                    m.SwitchMeters();
                else
                    m.ForwardEnabled = true;
            }));
        }

        private void Meter_Filter(object sender, System.Windows.Data.FilterEventArgs e)
        {
            if (((KeyValuePair<string, Meter>) e.Item).Value.Viewing == false)
                e.Accepted = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ((e.Source as MainWindow).DataContext as MainViewModel).ImportMeters.Execute(null);
        }

        private void ViewTabs(object sender, FilterEventArgs e)
        {
            if ((sender as CollectionViewSource).View == null) { }
            else if (_refreshViewTabFilter)
            {
                e.Accepted = false;

                _refreshViewTabFilter = false;
                (sender as CollectionViewSource).View.Refresh();
                _refreshViewTabFilter = true;
            }
            else if (!(e.Item as ContentTab).Visible && !_refreshViewTabFilter)
                e.Accepted = false;
        }

        private void MenuTabs(object sender, FilterEventArgs e)
        {
            if ((sender as CollectionViewSource).View == null) { }
            else if (_refreshMenuTabFilter)
                e.Accepted = false;
            else if ((e.Item as ContentTab).Visible && !_refreshMenuTabFilter)
                e.Accepted = false;

            if (_refreshMenuTabFilter && (sender as CollectionViewSource).View != null)
            {
                _refreshMenuTabFilter = false;
                (sender as CollectionViewSource).View.Refresh();
                _refreshMenuTabFilter = true;
            }
        }
    }
}
