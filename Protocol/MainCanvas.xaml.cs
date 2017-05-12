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
	/// The main page holding the toolbar and ink canvas
	/// </summary>
	public sealed partial class MainCanvas : Page
	{
		private MainCanvasViewModel viewModel;
		// fields for dry erasing
		private bool _isErasing;
		private Point _lastPoint;
		private List<InkStrokeContainer> _strokes = new List<InkStrokeContainer>();
		private InkSynchronizer _inkSynchronizer;

		public MainCanvas()
		{
			this.InitializeComponent();

            Loaded += MainCanvas_Loaded;

			// Add Touch to input types
			inkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Touch;

			// Turn on multi pointer input
			inkCanvas.InkPresenter.ActivateCustomDrying();
			inkCanvas.InkPresenter.SetPredefinedConfiguration(Windows.UI.Input.Inking.InkPresenterPredefinedConfiguration.SimpleMultiplePointer);
		}

		// Code for erasing dry ink from https://blogs.msdn.microsoft.com/synergist/2016/08/26/using-the-inktoolbar-with-custom-dry-ink-in-windows-anniversary-edition/
		private void MainCanvas_Loaded(object sender, RoutedEventArgs e)
		{
			var inkPresenter = inkCanvas.InkPresenter;

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
			_strokes.Add(container);
			_inkSynchronizer.EndDry();

			drawingCanvas.Invalidate();
		}

		private void DrawCanvas(CanvasControl sender, CanvasDrawEventArgs args)
		{
			DrawInk(args.DrawingSession);
		}

		private void DrawInk(CanvasDrawingSession session)
		{
			foreach (var item in _strokes)
			{
				var strokes = item.GetStrokes();

				using (var list = new CanvasCommandList(session))
				{
					using (var listSession = list.CreateDrawingSession())
					{
						listSession.DrawInk(strokes);
					}
				}

				session.DrawInk(strokes);
			}
		}

		private void Eraser_Checked(object sender, RoutedEventArgs e)
		{
			var unprocessedInput = inkCanvas.InkPresenter.UnprocessedInput;

			unprocessedInput.PointerPressed += UnprocessedInput_PointerPressed;
			unprocessedInput.PointerMoved += UnprocessedInput_PointerMoved;
			unprocessedInput.PointerReleased += UnprocessedInput_PointerReleased;
			unprocessedInput.PointerExited += UnprocessedInput_PointerExited;
			unprocessedInput.PointerLost += UnprocessedInput_PointerLost;

			inkCanvas.InkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.None;
		}

		private void Eraser_Unchecked(object sender, RoutedEventArgs e)
		{
			var unprocessedInput = inkCanvas.InkPresenter.UnprocessedInput;

			unprocessedInput.PointerPressed -= UnprocessedInput_PointerPressed;
			unprocessedInput.PointerMoved -= UnprocessedInput_PointerMoved;
			unprocessedInput.PointerReleased -= UnprocessedInput_PointerReleased;
			unprocessedInput.PointerExited -= UnprocessedInput_PointerExited;
			unprocessedInput.PointerLost -= UnprocessedInput_PointerLost;

			inkCanvas.InkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.Inking;
		}

		private void UnprocessedInput_PointerMoved(InkUnprocessedInput sender, PointerEventArgs args)
		{
			if (!_isErasing)
			{
				return;
			}

			var invalidate = false;

			foreach (var item in _strokes.ToArray())
			{
				var rect = item.SelectWithLine(_lastPoint, args.CurrentPoint.Position);

				if (rect.IsEmpty)
				{
					continue;
				}

				if (rect.Width * rect.Height > 0)
				{
					_strokes.Remove(item);

					invalidate = true;
				}
			}

			_lastPoint = args.CurrentPoint.Position;

			args.Handled = true;

			if (invalidate)
			{
				drawingCanvas.Invalidate();
			}
		}

		private void UnprocessedInput_PointerLost(InkUnprocessedInput sender, PointerEventArgs args)
		{
			if (_isErasing)
			{
				args.Handled = true;
			}

			_isErasing = false;
		}

		private void UnprocessedInput_PointerExited(InkUnprocessedInput sender, PointerEventArgs args)
		{
			if (_isErasing)
			{
				args.Handled = true;
			}

			_isErasing = true;
		}

		private void UnprocessedInput_PointerPressed(InkUnprocessedInput sender, PointerEventArgs args)
		{
			_lastPoint = args.CurrentPoint.Position;

			args.Handled = true;

			_isErasing = true;
		}

		private void UnprocessedInput_PointerReleased(InkUnprocessedInput sender, PointerEventArgs args)
		{
			if (_isErasing)
			{
				args.Handled = true;
			}

			_isErasing = false;
		}

		private void EraseAllInk(object sender, RoutedEventArgs e)
		{
			_strokes.Clear();

			drawingCanvas.Invalidate();
		}
	}
}
