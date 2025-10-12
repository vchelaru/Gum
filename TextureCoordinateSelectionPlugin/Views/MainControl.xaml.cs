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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TextureCoordinateSelectionPlugin.Views
{
    /// <summary>
    /// Interaction logic for MainControl.xaml
    /// </summary>
    public partial class MainControl : UserControl
    {
        public event EventHandler<KeyEventArgs> ImageRegionKeyDown;

        public MainControl()
        {
            InitializeComponent();

            // we are going to do our own handling of events
            InnerControl.DisableHotkeyPanning();
        }

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            ImageRegionKeyDown?.Invoke(null, e);
        }
    }
}
