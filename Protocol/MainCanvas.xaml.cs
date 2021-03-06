﻿using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;
using Shared.Models;
using Shared.Utils;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using System;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Shared.Views;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;

namespace Protocol
{
    /// <summary>
    /// The main page holding the canvases and toolbar
    /// </summary>
    public sealed partial class MainCanvas : Page
    {
        private MainCanvasViewModel viewModel;

        private InkSynchronizer _inkSynchronizer;

        private enum GridType { None, Fine, Medium, Large };
        private GridType currentGridType = GridType.None;

        Symbol ToShapeIcon = (Symbol)0xE97B;
        Symbol TouchWritingIcon = (Symbol)0xED5F;
        Symbol ExportIcon = (Symbol)0xEB9F;
        Symbol SaveIcon = (Symbol)0xE105;
        Symbol OpenIcon = (Symbol)0xED43;
        Symbol NewIcon = (Symbol)0xE78B;
        Symbol SettingsIcon = (Symbol)0xE713;
        Symbol MobileIcon = (Symbol)0xE8EA;
        Symbol WebIcon = (Symbol)0xE774;
        Symbol HomeIcon = (Symbol)0xE80F;
        Symbol ImportFileIcon = (Symbol)0xE8E5;

        private TemplateChoice templateChoice = TemplateChoice.None;
        private WriteableBitmap backgroundImage = null;

        public MainCanvas()
        {
            this.InitializeComponent();

            // Set up listeners
            inkToolbar.Loading += InkToolbar_Loading;
            Loaded += MainCanvas_Loaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var parameters = e.Parameter as MainCanvasParams;
            // Get viewmodel from parameters and set up all necessary listeners
            viewModel = new MainCanvasViewModel(parameters);
            viewModel.DrawCanvasInvalidated += Invalidate_DrawingCanvas;
            viewModel.ShowFlyoutAboveToolbar += ShowFlyout;
            viewModel.AddShapeToCanvas += AddShapeToRecognitionCanvas;
            viewModel.RemoveShapeFromCanvas += RemoveShapeFromRecognitionCanvas;

            // Load the template if there is one
            loadTemplate(parameters.template);
        }

        private void MainCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            var inkPresenter = inkCanvas.InkPresenter;

            // Add Touch to input types
            inkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen;

            // Turn on multi pointer input
            inkPresenter.ActivateCustomDrying();
            inkPresenter.SetPredefinedConfiguration(InkPresenterPredefinedConfiguration.SimpleMultiplePointer);

            _inkSynchronizer = inkPresenter.ActivateCustomDrying();
            inkPresenter.StrokesCollected += InkPresenter_StrokesCollected;

            // Handle whether input is pen eraser
            inkPresenter.UnprocessedInput.PointerPressed += UnprocessedInput_PointerPressed;

            SetUpEraseAll();
        }

        private void SetUpEraseAll() {
            var eraser = inkToolbar.GetToolButton(InkToolbarTool.Eraser) as InkToolbarEraserButton;

            // Handle erase all strokes
            var flyout = FlyoutBase.GetAttachedFlyout(eraser) as Flyout;

            if (flyout != null)
            {
                // Find Erase all button and set up listener
                var content = flyout.Content as StackPanel;
                
                var button = content.Children.ElementAt(3) as InkToolbarFlyoutItem;

                if (button != null)
                {
                    button.Click += EraseAllInk;
                }
            }
        }

        private void InkPresenter_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            // Custom drying
            var strokes = _inkSynchronizer.BeginDry();

            var container = new InkStrokeContainer();
            var clonedStrokes = from item in strokes select item.Clone();
            container.AddStrokes(clonedStrokes);
            viewModel.AddStroke(container);

            _inkSynchronizer.EndDry();
            drawingCanvas.Invalidate();

            if (inkToShapeButton.IsChecked.Value) // Store a copy of the strokes for recognition if button is checked
            {
                viewModel.RecognizeStrokes(clonedStrokes);
            }
            else
            {
                viewModel.StopRecognizingStrokes();
            }
        }

        private void AddShapeToRecognitionCanvas(Shape shape)
        {
            // Draw the recognised shapes and invalidate canvas to remove the corresponding strokes
            recognitionCanvas.Children.Add(shape);
            drawingCanvas.Invalidate();
        }

        private void RemoveShapeFromRecognitionCanvas(Shape shape)
        {
            recognitionCanvas.Children.Remove(shape);
        }

        private void DrawCanvas(CanvasControl sender, CanvasDrawEventArgs args)
        {
            // Redraws all the strokes in _strokes on canvas control
            viewModel.DrawInk(args.DrawingSession);
        }

        private void UnprocessedInput_PointerPressed(InkUnprocessedInput sender, PointerEventArgs args)
        {
            // Check if its an erasing stroke
            if (args.CurrentPoint.Properties.IsEraser || inkToolbar.GetToolButton(InkToolbarTool.Eraser).IsChecked.Value)
            {
                // Start erasing both shapes and strokes
                viewModel.AddListeners(inkCanvas.InkPresenter.UnprocessedInput);
                viewModel.StartErasing(args.CurrentPoint.Position);
            }
            else
            {
                // Stop listening to the user input
                viewModel.RemoveListeners(inkCanvas.InkPresenter.UnprocessedInput);
            }
            args.Handled = true;
        }

        private void EraseAllInk(object sender, RoutedEventArgs e)
        {
            // Delete all strokes and shapes on the canvases
            viewModel.ClearStokes();
            drawingCanvas.Invalidate();
            recognitionCanvas.Children.Clear();
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
            InkToolbarStencilButton ruler = new InkToolbarStencilButton();
            inkToolbar.Children.Add(eraser);
            inkToolbar.Children.Add(ballpoint);
            inkToolbar.Children.Add(pencil);
            inkToolbar.Children.Add(ruler);
        }

        private void ToggleTouch_Click(object sender, RoutedEventArgs e)
        {
            // Toggle between the two input types, Pen and Touch
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
            viewModel.SaveAsImage(inkCanvas.ActualWidth, inkCanvas.ActualHeight, new ProjectMetaData(bgTemplate.Visibility, templateChoice, bgImage.Visibility, backgroundImage), recognitionCanvas);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await viewModel.SaveProject(new ProjectMetaData(bgTemplate.Visibility, templateChoice));
        }

        private async void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            var parameters = await viewModel.OpenExistingProject(new ProjectMetaData(bgTemplate.Visibility, templateChoice));
            if (parameters != null)
            {
                this.Frame.Navigate(typeof(MainCanvas), parameters);
            }
        }

        private async void NewButton_Click(object sender, RoutedEventArgs e)
        {
            if (await viewModel.OpenNewProject(new ProjectMetaData(bgTemplate.Visibility, templateChoice)) != ContentDialogResult.None)
            {
                this.Frame.Navigate(typeof(MainCanvas), new MainCanvasParams(new List<InkStrokeContainer>(), null, TemplateChoice.None, new List<CanvasComponent>()));
            }
        }

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationPane.IsPaneOpen = !NavigationPane.IsPaneOpen;
        }

        private void ShowFlyout(string message)
        {
            TextBlock t = new TextBlock();
            t.Text = message;
            Flyout f = new Flyout();
            f.Content = t;
            try
            {
                f.ShowAt(inkToolbar);
            }
            catch (ArgumentException ex)
            {
                if (ex.Message.StartsWith("The parameter is incorrect."))
                {
                    //Do nothing
                }
                else
                {
                    throw ex;
                }
            }
        }

        private void GridButton_Click(object sender, RoutedEventArgs e)
        {
            //Open menu to choose grid granularity
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private void FineGridItem_Click(object sender, RoutedEventArgs e)
        {
            BitmapImage bm = new BitmapImage(new Uri("ms-appx:///Assets/grid_fine.png", UriKind.Absolute));
            LoadGrid(GridType.Fine, bm);
        }

        private void MediumGridItem_Click(object sender, RoutedEventArgs e)
        {
            BitmapImage bm = new BitmapImage(new Uri("ms-appx:///Assets/grid_medium.png", UriKind.Absolute));
            LoadGrid(GridType.Medium, bm);
        }

        private void LargeGridItem_Click(object sender, RoutedEventArgs e)
        {
            BitmapImage bm = new BitmapImage(new Uri("ms-appx:///Assets/grid_large.png", UriKind.Absolute));
            LoadGrid(GridType.Large, bm);
        }

        private void LoadGrid(GridType type, BitmapImage bm)
        {
            if (currentGridType == type)
            {
                bgGrid.Visibility = Visibility.Collapsed;
                currentGridType = GridType.None;
            }
            else
            {
                bgGrid.Source = bm;
                currentGridType = type;
                bgGrid.Visibility = Visibility.Visible;
            }
        }

        private void BrowserTemplate_Click(object sender, RoutedEventArgs e)
        {
            BitmapImage bm = new BitmapImage(new Uri("ms-appx:///Assets/browser.png", UriKind.Absolute));
            bgTemplate.Source = bm;

            //Update template
            templateChoice = TemplateChoice.Browser;
        }

        private async void MobileTemplate_Click(object sender, RoutedEventArgs e)
        {
            TemplateDialog td = new TemplateDialog();
            await td.ShowAsync();
            BitmapImage bm = new BitmapImage(new Uri($"ms-appx:///Assets/{td.templateChoice.ToString()}.png", UriKind.Absolute));
            bgTemplate.Source = bm;

            //Update template
            templateChoice = td.templateChoice;
        }

        private void loadTemplate(TemplateChoice templateChoice)
        {
            if (templateChoice == TemplateChoice.Browser)
            {
                templateToggle.IsOn = true;
                BitmapImage bm = new BitmapImage(new Uri("ms-appx:///Assets/browser.png", UriKind.Absolute));
                bgTemplate.Source = bm;
                bgTemplate.Visibility = Visibility.Visible;
            }
            else if (templateChoice == TemplateChoice.MobYY)
            {
                templateToggle.IsOn = true;
                BitmapImage bm = new BitmapImage(new Uri($"ms-appx:///Assets/mobYY.png", UriKind.Absolute));
                bgTemplate.Source = bm;
                bgTemplate.Visibility = Visibility.Visible;
            }
            else if (templateChoice == TemplateChoice.MobYX)
            {
                templateToggle.IsOn = true;
                BitmapImage bm = new BitmapImage(new Uri($"ms-appx:///Assets/mobYX.png", UriKind.Absolute));
                bgTemplate.Source = bm;
                bgTemplate.Visibility = Visibility.Visible;
            }
            else if (templateChoice == TemplateChoice.MobXX)
            {
                templateToggle.IsOn = true;
                BitmapImage bm = new BitmapImage(new Uri($"ms-appx:///Assets/mobXX.png", UriKind.Absolute));
                bgTemplate.Source = bm;
                bgTemplate.Visibility = Visibility.Visible;
            }
        }

        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (bgTemplate.Visibility == Visibility.Visible)
            {
                bgTemplate.Visibility = Visibility.Collapsed;
                //Change the colour of the template buttons
                browserTemplate1.IsEnabled = false;
                browserTemplate2.IsEnabled = false;
                browserTemplateText.Foreground = new SolidColorBrush(Colors.DarkGray);
                mobileTemplate1.IsEnabled = false;
                mobileTemplate2.IsEnabled = false;
                mobileTemplateText.Foreground = new SolidColorBrush(Colors.DarkGray);
            }
            else
            {
                bgTemplate.Visibility = Visibility.Visible;
                //Change the colour of the template buttons
                browserTemplate1.IsEnabled = true;
                browserTemplate2.IsEnabled = true;
                browserTemplateText.Foreground = new SolidColorBrush(Colors.WhiteSmoke);
                mobileTemplate1.IsEnabled = true;
                mobileTemplate2.IsEnabled = true;
                mobileTemplateText.Foreground = new SolidColorBrush(Colors.WhiteSmoke);
            }
            if (templateChoice == TemplateChoice.None)
            {
                templateChoice = TemplateChoice.Browser;
            }
        }

        private void BackgroundToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (bgImage.Visibility == Visibility.Visible)
            {
                bgImage.Visibility = Visibility.Collapsed;
            }
            else
            {
                bgImage.Visibility = Visibility.Visible;
            }
        }

        private async void ImportBackground_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker open = new FileOpenPicker();
            open.FileTypeFilter.Add(".jpg");
            open.FileTypeFilter.Add(".jpeg");
            open.FileTypeFilter.Add(".png");

            // Open a stream for the selected file 
            StorageFile file = await open.PickSingleFileAsync();

            // Ensure a file was selected 
            if (file != null)
            {
                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
                {
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                    WriteableBitmap bmp = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                    bmp.SetSource(stream);

                    // show the image in the UI if you want.
                    bgImage.Source = bmp;
                    backgroundImage = bmp;                    
                }
            }
        }

        private async void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            if (await viewModel.OpenNewProject(new ProjectMetaData(bgTemplate.Visibility, templateChoice)) != ContentDialogResult.None)
            {
                this.Frame.Navigate(typeof(StartPage));
            }
        }
    }
}
