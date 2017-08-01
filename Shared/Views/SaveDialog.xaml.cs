using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Shared.Views
{
    public sealed partial class SaveDialog : ContentDialog
    {
        public ContentDialogResult result { get; set; }
        public StorageFolder folder;
        public RadioButton selected;

        public SaveDialog()
        {
            this.InitializeComponent();
        }
        
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            result = ContentDialogResult.Primary;
            if (!txtProjectName.Text.Equals(""))
            {
                if (folder != null)
                {
                    // Application now has read/write access to all contents in the picked folder(including other sub-folder contents)
                    StorageFolder storageFolder = await folder.CreateFolderAsync(txtProjectName.Text, CreationCollisionOption.ReplaceExisting);
                    folder = storageFolder;
                    //TODO: Rebecca 
                    selected = rdMob;
                    dialog.Hide();
                }
                else
                {
                    FlyoutMsg("Please Choose a Directory", txtDirectory);
                }
            }
            else
            {
                FlyoutMsg("Please Choose a Project Name", txtProjectName);
            }
        }

        private void FlyoutMsg(string title, FrameworkElement parent)
        {
            TextBlock t = new TextBlock();
            t.Text = title;
            Flyout f = new Flyout();
            f.Content = t;
            f.LightDismissOverlayMode = LightDismissOverlayMode.On;
            f.ShowAt(parent);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            result = ContentDialogResult.Secondary;
            dialog.Hide();
        }

        private async void ChooseDirButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            //Let the user pick a folder
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder storageFolder = await folderPicker.PickSingleFolderAsync();
            if (storageFolder != null)
            {
                folder = storageFolder;
                txtDirectory.Text = folder.Path;
            }
            else
            {
                //Operation cancelled
                folder = null;
            }
        }
    }
}
