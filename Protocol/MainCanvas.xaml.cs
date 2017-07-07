using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;
using Shared.ViewModels;

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

        public MainCanvas()
		{
			this.InitializeComponent();
			inkToolbar.Loading += InkToolbar_Loading;
			Loaded += MainCanvas_Loaded;						
		}

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            viewModel = new MainCanvasViewModel(e.Parameter as MainCanvasParams);
            viewModel.DrawCanvasInvalidated += Invalidate_DrawingCanvas;
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel.SaveProject();
        }

    }
}
