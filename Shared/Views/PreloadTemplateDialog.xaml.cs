using Shared.Models;
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
    public sealed partial class PreloadTemplateDialog : ContentDialog
    {
        public ContentDialogResult result { get; set; }
        public TemplateChoice template { get; set; }

        public PreloadTemplateDialog()
        {
            this.InitializeComponent();
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            result = ContentDialogResult.Primary;
            if (none.IsChecked == true)
            {
                template = TemplateChoice.None;
            }
            else if (mobile.IsChecked == true)
            {
                template = TemplateChoice.MobYY;
            }
            else if (browser.IsChecked == true)
            {
                template = TemplateChoice.Browser;
            }
            preloadTemplateDialog.Hide();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            result = ContentDialogResult.Secondary;
            preloadTemplateDialog.Hide();
        }
    }
}
