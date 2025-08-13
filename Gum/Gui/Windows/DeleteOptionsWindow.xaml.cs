﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Gum.Commands;
using Gum.Services;

namespace Gum.Gui.Windows
{
    /// <summary>
    /// Interaction logic for DeleteOptionsWindow.xaml
    /// </summary>
    public partial class DeleteOptionsWindow : System.Windows.Window
    {
        /// <summary>
        /// The stack panel for plugins to add additional controls for options like
        /// whether to delete the XML
        /// </summary>
        public StackPanel MainStackPanel => StackPanelInstance; 

        public string Message
        {
            get => (string)LabelInstance.Content;
            set => LabelInstance.Content = value;
        }

        public Array ObjectsToDelete { get; set; }

        public DeleteOptionsWindow()
        {
            InitializeComponent();
        }

        private void YesButtonClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void NoButtonClick(object sender, RoutedEventArgs e)
        {
            CloseWithResultFalse();
        }

        private void CloseWithResultFalse()
        {
            this.DialogResult = false;

            this.MainStackPanel.Children.Clear();
        }

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter || e.Key == Key.Return || e.Key == Key.Y)
            {
                e.Handled = true;
                this.DialogResult = true;
            }
            else if(e.Key == Key.Escape || e.Key == Key.N)
            {
                e.Handled = true;
                CloseWithResultFalse();
            }
        }
    }
}
