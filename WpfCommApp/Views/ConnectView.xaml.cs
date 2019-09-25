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

namespace WpfCommApp
{

    public class LedControl : CheckBox
    {
        static LedControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LedControl), new FrameworkPropertyMetadata(typeof(LedControl)));
        }

        public static readonly DependencyProperty OnColorProperty =
            DependencyProperty.Register("OnColor", typeof(Brush), typeof(LedControl), new PropertyMetadata(Brushes.Green));

        public Brush OnColor
        {
            get { return (Brush)GetValue(OnColorProperty); }
            set { SetValue(OnColorProperty, value); }
        }

        public static readonly DependencyProperty OffColorProperty =
            DependencyProperty.Register("OffColor", typeof(Brush), typeof(LedControl), new PropertyMetadata(Brushes.Red));

        public Brush OffColor
        {
            get { return (Brush)GetValue(OffColorProperty); }
            set { SetValue(OffColorProperty, value); }
        }

        
        public static readonly DependencyProperty NoColorProperty =
            DependencyProperty.Register("NoColor", typeof(Brush), typeof(LedControl), new PropertyMetadata(Brushes.Gray));

        public Brush NoColor
        {
            get { return (Brush)GetValue(NoColorProperty); }
            set { SetValue(NoColorProperty, value); }
        }
    }

    /// <summary>
    /// Interaction logic for ConnectView.xaml
    /// </summary>
    public partial class ConnectView : UserControl
    {
        public ConnectView()
        {
            InitializeComponent();

            Dictionary<string, IPageViewModel> state = (Application.Current.Properties["pageState"] as Dictionary<string, IPageViewModel>);
            if (state.ContainsKey("connect"))
                DataContext = state["connect"];
            else
            {
                DataContext = new ConnectViewModel();
                state.Add("connect", DataContext as ConnectViewModel);
            }
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
