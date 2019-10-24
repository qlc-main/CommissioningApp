using System.Windows.Controls;

namespace WpfCommApp
{
    /// <summary>
    /// Interaction logic for CommissioningView.xaml
    /// </summary>
    public partial class CommissioningView : UserControl
    {
        public CommissioningView()
        {
            InitializeComponent();
        }

        private void UserControl_DCChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue == null)
                (e.NewValue as CommissioningViewModel).StartAsync.Execute(null);
            else
                (e.OldValue as CommissioningViewModel).StopAsync.Execute(null);
        }
    }
}
