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
using Windows.UI.Xaml.Shapes;
using System;
using Windows.UI.Xaml.Input;

namespace Protocol
{
    /// <summary>
    /// The main page holding the canvas and toolbar
    /// </summary>
    public sealed partial class MainCanvas : Page
    {
        private MainCanvasViewModel viewModel;

        private InkSynchronizer _inkSynchronizer;

        Symbol ToShapeIcon = (Symbol)0xE97B;
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
            viewModel.ShowFlyoutAboveToolbar += ShowFlyout;
            viewModel.AddShapeToCanvas += AddShapeToRecognitionCanvas;
            viewModel.RemoveShapeFromCanvas += RemoveShapeFromRecognitionCanvas;

            if (parameters.size == CanvasSize.Mobile)
            {
                leftPanel.Width = new GridLength(1, GridUnitType.Star);
                rightPanel.Width = new GridLength(1, GridUnitType.Star);
                inkCanvas.MinWidth = 607.5;
                drawingCanvas.MinWidth = 607.5;
            }
        }

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

            SetUpEraseAll();
        }
        
        private void SetUpEraseAll() {
            var eraser = inkToolbar.GetToolButton(InkToolbarTool.Eraser) as InkToolbarEraserButton;

            // Handle erase all strokes
            var flyout = FlyoutBase.GetAttachedFlyout(eraser) as Flyout;

            if (flyout != null)
            {
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
            var strokes = _inkSynchronizer.BeginDry();

            var container = new InkStrokeContainer();
            var clonedStrokes = from item in strokes select item.Clone();
            container.AddStrokes(clonedStrokes);
            viewModel.AddStroke(container);

            _inkSynchronizer.EndDry();
            drawingCanvas.Invalidate();

            if (inkToShapeButton.IsChecked.Value) // store strokes for recognition if button is checked
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
            recognitionCanvas.Children.Add(shape);
        }

        private void RemoveShapeFromRecognitionCanvas(Shape shape)
        {
            recognitionCanvas.Children.Remove(shape);
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
            await viewModel.SaveProject();
        }

        private async void OpenButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            var parameters = await viewModel.OpenExistingProject();
            if (parameters != null)
            {
                // TODO size is hard coded
                viewModel = new MainCanvasViewModel(parameters);
                drawingCanvas.Invalidate();
            }
        }

        private async void NewButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO check if user want to save first
            if (await viewModel.OpenNewProject() != ContentDialogResult.None)
            {
                // TODO size is hard coded
                viewModel = new MainCanvasViewModel(new MainCanvasParams(new List<InkStrokeContainer>(), null, CanvasSize.Hub));
                drawingCanvas.Invalidate();
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
            f.ShowAt(inkToolbar);
        }
    }
}
