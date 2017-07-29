﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static Protocol.MainCanvas;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Shared.Views
{
    public sealed partial class TemplateDialog : ContentDialog
    {
        public string name { get; set; }
        public TemplateChoice templateChoice = TemplateChoice.None;

        public TemplateDialog()
        {
            this.InitializeComponent();
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (rdYX.IsChecked == true)
            {
                name = "YX";
                templateChoice = TemplateChoice.MobYX;
            }
            else if (rdXX.IsChecked == true)
            {
                name = "XX";
                templateChoice = TemplateChoice.MobXX;
            }
            else if (rdYY.IsChecked == true)
            {
                name = "YY";
                templateChoice = TemplateChoice.MobYY;
            }
            templateDialog.Hide();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            templateDialog.Hide();
        }
    }
}
