using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Data;

namespace WpfCommApp
{
    /// <summary>
    /// Interaction logic for ConnectView.xaml
    /// </summary>
    public partial class ConnectView : UserControl
    {
        public ConnectView()
        {
            InitializeComponent();
        }

        private void coms_click(object sender, RoutedEventArgs e)
        {
            // Add ports to the ComPorts object before displaying context menu
            string[] ports = System.IO.Ports.SerialPort.GetPortNames();
            var vm = (DataContext as ConnectViewModel);
            if (vm.ComPorts.Count != ports.Length)
            {
                foreach (string s in ports)
                {
                    if (!vm.ComPorts.Any(p => p.Name == s))
                        vm.ComPorts.Add(new Serial(s, false));
                }
            }

            var button = sender as Button;
            button.ContextMenu.IsOpen = !button.ContextMenu.IsOpen;

            if (button.ContextMenu.IsOpen)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                button.ContextMenu.IsEnabled = true;
            }
            else
            {
                button.ContextMenu.IsEnabled = false;
            }
        }

        private void CollectionViewSource_Filter(object sender, System.Windows.Data.FilterEventArgs e)
        {
            if ((e.Item as Serial).Used || (e.Item as Serial).Default)
                e.Accepted = false;
        }
    }
}
