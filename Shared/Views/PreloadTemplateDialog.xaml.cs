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

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Shared.Views
{
    public sealed partial class PreloadTemplateDialog : ContentDialog
    {
        public PreloadTemplateDialog()
        {
            this.InitializeComponent();
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (none.IsChecked == true)
            {

            }
            else if (mobile.IsChecked == true)
            {

            }
            else if (browser.IsChecked == true)
            {

            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            preloadTemplateDialog.Hide();
        }
    }
}
