using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;

namespace Shared.ViewModels
{
    public class DialogFactory
    {
        private static string confirmText = "Ok";
        private static string denyText = "Cancel";
                
        public async void ConfirmDialogAsync(string title)
        {
            ContentDialog dialog = new ContentDialog();
            dialog.Title = title;
            dialog.Height = 50;
            dialog.PrimaryButtonText = confirmText;
            await dialog.ShowAsync();
        }

        public async Task<bool> BooleanDialogAsync(string title)
        {
            ContentDialog dialog = new ContentDialog();
            dialog.Title = title;
            dialog.IsSecondaryButtonEnabled = true;
            dialog.PrimaryButtonText = confirmText;
            dialog.SecondaryButtonText = denyText;
            dialog.Height = 50;
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                return true;
            else
                return false;
        }
    }
}
