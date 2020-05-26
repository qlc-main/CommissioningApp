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

        private void DataGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Code to shrink columns when window is resized smaller
            if (e.NewSize.Width < e.PreviousSize.Width || e.NewSize.Height < e.PreviousSize.Height)
            {
                var dg = (sender as DataGrid);
                for (int i = 0; i < dg.Columns.Count; i++)
                    dg.Columns[i].Width = 0;
                dg.UpdateLayout();
                for (int i = 0; i < dg.Columns.Count; i++)
                    dg.Columns[i].Width = DataGridLength.Auto;
            }
        }
    }
}
