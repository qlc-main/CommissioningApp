﻿using System;
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
using System.Windows.Shapes;

namespace WpfCommApp
{
    /// <summary>
    /// Interaction logic for SerialDisconnect.xaml
    /// </summary>
    public partial class SerialDisconnectView : Window
    {
        public SerialDisconnectView()
        {
            InitializeComponent();
        }

        private void CloseCommand(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var sdvm = (this.DataContext as SerialDisconnectViewModel);
            if (sdvm.UserClosedWindow)
            {
                sdvm.StopPolling();
            }
        }
    }
}