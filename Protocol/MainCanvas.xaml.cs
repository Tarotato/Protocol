using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Protocol
{
	/// <summary>
	/// The main page holding the canvas and toolbar
	/// </summary>
	public sealed partial class MainCanvas : Page
	{
		private MainCanvasViewModel viewModel;

		private InkSynchronizer _inkSynchronizer;

		public MainCanvas()
		{
			Symbol SelectIcon = (Symbol)0xEF20;
			this.InitializeComponent();
			inkToolbar.Loading += inkToolbar_Loading;

			Loaded += MainCanvas_Loaded;			

			viewModel = new MainCanvasViewModel();
			viewModel.DrawCanvasInvalidated += Invalidate_DrawingCanvas;
		}

		// Code for erasing dry ink from https://blogs.msdn.microsoft.com/synergist/2016/08/26/using-the-inktoolbar-with-custom-dry-ink-in-windows-anniversary-edition/
		private void MainCanvas_Loaded(object sender, RoutedEventArgs e)
		{
			var inkPresenter = inkCanvas.InkPresenter;

			// Add Touch to input types
			inkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Touch;

			// Turn on multi pointer input
			inkPresenter.ActivateCustomDrying();
			inkPresenter.SetPredefinedConfiguration(Windows.UI.Input.Inking.InkPresenterPredefinedConfiguration.SimpleMultiplePointer);

			_inkSynchronizer = inkPresenter.ActivateCustomDrying();
			inkPresenter.StrokesCollected += InkPresenter_StrokesCollected;


			// Handle checked and unchecked events for eraser (for erasing dry ink)
			var eraser = inkToolbar.GetToolButton(InkToolbarTool.Eraser) as InkToolbarEraserButton;

			if (eraser != null)
			{
				eraser.Checked += Eraser_Checked;
				eraser.Unchecked += Eraser_Unchecked;
			}

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

		private void Eraser_Checked(object sender, RoutedEventArgs e)
		{
			var unprocessedInput = inkCanvas.InkPresenter.UnprocessedInput;
			viewModel.AddListeners(unprocessedInput);
			inkCanvas.InkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.None;
		}

		private void Eraser_Unchecked(object sender, RoutedEventArgs e)
		{
			var unprocessedInput = inkCanvas.InkPresenter.UnprocessedInput;
			viewModel.RemoveListeners(unprocessedInput);
			inkCanvas.InkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.Inking;
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

		private void inkToolbar_Loading(FrameworkElement sender, object args)
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

		private void addShapeToolButton_Click(object sender, RoutedEventArgs e)
		{

		}

	}
}
