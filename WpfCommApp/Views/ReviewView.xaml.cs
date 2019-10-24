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
    /// <summary>
    /// Interaction logic for ReviewView.xaml
    /// </summary>
    public partial class ReviewView : UserControl
    {
        public ReviewView()
        {
            InitializeComponent();
        }

        private void UserControl_DCChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue == null)
                (e.NewValue as ReviewViewModel).Monitor.Execute(null);
            else
                (e.OldValue as ReviewViewModel).Stop.Execute(null);
        }
    }
}
