﻿using Shared.Models;
using System;
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
    /// <summary>
    /// Dialog letting users choose between 3 different mobile templates
    /// </summary>
    public sealed partial class TemplateDialog : ContentDialog
    {
        
        public TemplateChoice templateChoice = TemplateChoice.None;

        public TemplateDialog()
        {
            this.InitializeComponent();
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (rdYX.IsChecked == true)
            {
                templateChoice = TemplateChoice.MobYX;
            }
            else if (rdXX.IsChecked == true)
            {
                templateChoice = TemplateChoice.MobXX;
            }
            else if (rdYY.IsChecked == true)
            {
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
