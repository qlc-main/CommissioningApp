using System.Windows;
using System.Windows.Controls;

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
    }
}
