using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;

namespace Shared.ViewModels
{
    public class DialogFactory
    {

        public async Task<string> InputTextDialogAsync(string title)
        {
            TextBox inputTextBox = new TextBox();
            inputTextBox.AcceptsReturn = false;
            inputTextBox.Height = 32;
            ContentDialog dialog = new ContentDialog();
            dialog.Content = inputTextBox;
            dialog.Title = title;
            dialog.IsSecondaryButtonEnabled = true;
            dialog.PrimaryButtonText = "Ok";
            dialog.SecondaryButtonText = "Cancel";
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                return inputTextBox.Text;
            }
            else
            {
                //Operation cancelled
                OperationCancelledDialogAsync("Save Cancelled");
                return null;
            }

        }

        public async Task<StorageFolder> ChooseFolderDialogAsync(string projectName)
        {
            ContentDialog dialog = new ContentDialog();
            dialog.Title = "Choose a Folder";
            dialog.IsSecondaryButtonEnabled = true;
            dialog.PrimaryButtonText = "Choose Folder";
            dialog.SecondaryButtonText = "Cancel";
            dialog.Height = 50;
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                return await PickFolder(projectName);
            }
            else
            {
                //Operation cancelled
                OperationCancelledDialogAsync("Save Cancelled");
                return null;

            }
        }

        
        private async Task<StorageFolder> PickFolder(string projectName)
        {
            //Let the user pick a folder
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                // Application now has read/write access to all contents in the picked folder(including other sub-folder contents)
                StorageFolder storageFolder = await folder.CreateFolderAsync(projectName, CreationCollisionOption.ReplaceExisting);
                return storageFolder;
            }
            else
            {
                //Operation cancelled
                OperationCancelledDialogAsync("Save Cancelled");
                return null;
            }
        }

        private async void OperationCancelledDialogAsync(string title)
        {
            ContentDialog dialog = new ContentDialog();
            dialog.Title = title;
            dialog.Height = 50;
            dialog.PrimaryButtonText = "Ok";
            await dialog.ShowAsync();

        }
    }
}
