using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Collections.Generic;
using System;
using System.Linq;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;
using Shared.ViewModels;
using Shared.Views;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.Storage.Streams;
using Shared.Models;

namespace Protocol
{
    /// <summary>
    /// The main page holding the canvas and toolbar
    /// </summary>
    public sealed partial class MainCanvas : Page
    {
        private MainCanvasViewModel viewModel;

        private InkSynchronizer _inkSynchronizer;

        Symbol ShapeIcon = (Symbol)0xE15B;
        Symbol TouchWritingIcon = (Symbol)0xED5F;
        Symbol ExportIcon = (Symbol)0xE158;
        Symbol SaveIcon = (Symbol)0xE105;
        Symbol OpenIcon = (Symbol)0xED43;
        Symbol NewIcon = (Symbol)0xE8E5;
        Symbol SettingsIcon = (Symbol)0xE713;

        public MainCanvas()
        {
            this.InitializeComponent();
            inkToolbar.Loading += InkToolbar_Loading;
            Loaded += MainCanvas_Loaded;						
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var parameters = e.Parameter as MainCanvasParams;
            viewModel = new MainCanvasViewModel(parameters);
            viewModel.DrawCanvasInvalidated += Invalidate_DrawingCanvas;

            if (parameters.size == CanvasSize.Mobile)
            {
                leftPanel.Width = new GridLength(1, GridUnitType.Star);
                rightPanel.Width = new GridLength(1, GridUnitType.Star);
                inkCanvas.MinWidth = 607.5;
                drawingCanvas.MinWidth = 607.5;
            }
        }

        // Code for erasing dry ink from https://blogs.msdn.microsoft.com/synergist/2016/08/26/using-the-inktoolbar-with-custom-dry-ink-in-windows-anniversary-edition/
        private void MainCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            var inkPresenter = inkCanvas.InkPresenter;

            // Add Touch to input types
            inkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen;

            // Turn on multi pointer input
            inkPresenter.ActivateCustomDrying();
            inkPresenter.SetPredefinedConfiguration(Windows.UI.Input.Inking.InkPresenterPredefinedConfiguration.SimpleMultiplePointer);

            _inkSynchronizer = inkPresenter.ActivateCustomDrying();
            inkPresenter.StrokesCollected += InkPresenter_StrokesCollected;

            // Handle whether input is pen eraser
            inkPresenter.UnprocessedInput.PointerPressed += UnprocessedInput_PointerPressed;

            var eraser = inkToolbar.GetToolButton(InkToolbarTool.Eraser) as InkToolbarEraserButton;
            SetUpEraseAll(eraser);
        }
        
        private void SetUpEraseAll(InkToolbarEraserButton eraser) {
            // Handle erase all strokes
            var flyout = FlyoutBase.GetAttachedFlyout(eraser) as Flyout;

            if (flyout != null)
            {
                var button = flyout.Content as Button;

                if (button != null)
                {
                    var newButton = new Button();
                    newButton.Style = button.Style;
                    newButton.Content = button.Content;

                    newButton.Click += EraseAllInk;
                    flyout.Content = newButton;
                }
            }
        }

        private void InkPresenter_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            var strokes = _inkSynchronizer.BeginDry();
            var container = new InkStrokeContainer();

            container.AddStrokes(from item in strokes select item.Clone());
            viewModel.AddStroke(container);
            _inkSynchronizer.EndDry();

            drawingCanvas.Invalidate();
        }

        private void DrawCanvas(CanvasControl sender, CanvasDrawEventArgs args)
        {
            viewModel.DrawInk(args.DrawingSession);
        }

        private void UnprocessedInput_PointerPressed(InkUnprocessedInput sender, PointerEventArgs args)
        {
            if (args.CurrentPoint.Properties.IsEraser || inkToolbar.GetToolButton(InkToolbarTool.Eraser).IsChecked.Value)
            {
                viewModel.AddListeners(inkCanvas.InkPresenter.UnprocessedInput);
                viewModel.StartErasing(args.CurrentPoint.Position);
            }
            else
            {
                viewModel.RemoveListeners(inkCanvas.InkPresenter.UnprocessedInput);
            }
            args.Handled = true;
        }

        private void EraseAllInk(object sender, RoutedEventArgs e)
        {
            viewModel.ClearStokes();

            drawingCanvas.Invalidate();
        }
        
        private void Invalidate_DrawingCanvas()
        {
            drawingCanvas.Invalidate();
        }

        private void InkToolbar_Loading(FrameworkElement sender, object args)
        {
            // Clear all built-in buttons from the InkToolbar.
            inkToolbar.InitialControls = InkToolbarInitialControls.None;

            InkToolbarEraserButton eraser = new InkToolbarEraserButton();
            InkToolbarBallpointPenButton ballpoint = new InkToolbarBallpointPenButton();
            InkToolbarPencilButton pencil = new InkToolbarPencilButton();
            InkToolbarRulerButton ruler = new InkToolbarRulerButton();
            inkToolbar.Children.Add(eraser);
            inkToolbar.Children.Add(ballpoint);
            inkToolbar.Children.Add(pencil);
            inkToolbar.Children.Add(ruler);
        }

        private void AddShapeToolButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO 
        }

        private void ToggleTouch_Click(object sender, RoutedEventArgs e)
        {
            if (toggleTouchButton.IsChecked == true)
            {
                inkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Touch;
            }
            else
            {
                inkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen;
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel.SaveAsImage(inkCanvas.ActualWidth, inkCanvas.ActualHeight);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if(await viewModel.SaveProject())
            {
                FlyoutMsg("Project Saved", inkToolbar);
            }
        }

        private void FlyoutMsg(string title, FrameworkElement parent)
        {
            TextBlock t = new TextBlock();
            t.Text = title;
            Flyout f = new Flyout();
            f.Content = t;
            f.ShowAt(parent);
        }

        private async void OpenButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            List<InkStrokeContainer> strokes = new List<InkStrokeContainer>();
            //Let the user pick a project folder to open
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();
                foreach (var f in files)
                {
                    if (f != null && f.FileType.Equals(".gif"))
                    {
                        // Open a file stream for reading.
                        IRandomAccessStream stream = await f.OpenAsync(FileAccessMode.Read);
                        // Read from file.
                        using (var inputStream = stream.GetInputStreamAt(0))
                        {
                            var container = new InkStrokeContainer();
                            await container.LoadAsync(inputStream);
                            //Add the strokes stored in the files
                            strokes.Add(container);
                        }
                        stream.Dispose();
                    }
                }
                ConfirmSave(folder);

            }
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            ConfirmSave(null);
        }

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationPane.IsPaneOpen = !NavigationPane.IsPaneOpen;
        }

        private async void ConfirmSave(StorageFolder storageFolder)
        {
            TernaryButtonDialog t = new TernaryButtonDialog();
            await t.ShowAsync();
            if (t.result == ContentDialogResult.Primary)
            {
                await viewModel.SaveProject();
                // TODO size is hard coded
                this.Frame.Navigate(typeof(MainCanvas), new MainCanvasParams(strokes, folder, CanvasSize.Hub));
            }
            else if (t.result == ContentDialogResult.Secondary)
            {
                // TODO size is hard coded
                this.Frame.Navigate(typeof(MainCanvas), new MainCanvasParams(strokes, folder, CanvasSize.Hub));
            }
        }
    }
}
