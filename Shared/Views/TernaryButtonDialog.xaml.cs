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
    /// Dialog that asks user to confirm whether they want to save the current project when they start navigating away
    /// </summary>
    public sealed partial class TernaryButtonDialog : ContentDialog
    {
        public ContentDialogResult result { get; set; }

        public TernaryButtonDialog()
        {
            this.InitializeComponent();
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            result = ContentDialogResult.Primary;
            dialog.Hide();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            result = ContentDialogResult.Secondary;
            dialog.Hide();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            result = ContentDialogResult.None;
            dialog.Hide();
        }
    }
}
