using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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

}
