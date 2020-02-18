using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Data;
using System.Management;

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
            var wmi = new ManagementObjectSearcher("root\\cimv2",
                    "SELECT * FROM Win32_PnPEntity WHERE ClassGuid=\"{4d36e978-e325-11ce-bfc1-08002be10318}\"").Get();
            
            var vm = (DataContext as ConnectViewModel);
            //vm.ComPorts.Clear();
            foreach (ManagementObject m in wmi)
            {
                string com = m["Name"] as string;
                int start = com.IndexOf("(") + 1;
                com = com.Substring(start, com.Length - start - 1);

                if (!vm.ComPorts.Any(p => p.Name == com))
                { 
                    if (!(m["Name"] as string).Contains("Virtual"))
                        vm.ComPorts.Add(new Serial(com, false));
                    else
                        vm.ComPorts.Add(new Serial(com, true));
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
            if ((e.Item as Serial).Virtual)
                e.Accepted = false;
        }
    }
}
