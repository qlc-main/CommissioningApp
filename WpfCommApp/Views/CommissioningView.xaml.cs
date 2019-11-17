using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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

        /// <summary>
        /// Function that overrides the phase set feature 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Override(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var control = sender as LedControl;
            var channel = control.DataContext as Channel;
            BindingExpression be = BindingOperations.GetBindingExpression((FrameworkElement) control, ((DependencyProperty) CheckBox.IsCheckedProperty));
            var path = be.ParentBinding.Path.Path;

            if (path == "Phase1")
            {
                channel.Forced[0] = true;
                if (channel.Phase1 == null)
                    channel.Phase1 = true;
                else if (channel.Phase1 == true)
                    channel.Phase1 = null;
            }
            else if (path == "Phase2")
            {
                channel.Forced[1] = true;
                if (channel.Phase2 == null)
                    channel.Phase2 = true;
                else if (channel.Phase2 == true)
                    channel.Phase2 = null;
            }
            else
            {
            }
        }

        /// <summary>
        /// Determines if user can change the phase through a mouse click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var control = sender as LedControl;
            var channel = control.DataContext as Channel;
            BindingExpression be = BindingOperations.GetBindingExpression((FrameworkElement) control, ((DependencyProperty) CheckBox.IsCheckedProperty));
            string path = be.ParentBinding.Path.Path;
            CommissioningViewModel cv = DataContext as CommissioningViewModel;
            if (channel != null)
                cv.ModifyPhase.Execute(new object[] { path, channel });
            e.Handled = true;
        }

    }
}
