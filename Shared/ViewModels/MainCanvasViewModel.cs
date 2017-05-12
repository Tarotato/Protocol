using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;

namespace Protocol
{
	public class MainCanvasViewModel
	{
		public delegate void ChangedEventHandler();
		public event ChangedEventHandler DrawCanvasInvalidated;

		// fields for dry erasing
		private bool _isErasing;
		private Point _lastPoint;
		private List<InkStrokeContainer> _strokes = new List<InkStrokeContainer>();

		internal void AddStroke(InkStrokeContainer container)
		{
			_strokes.Add(container);
		}

		internal void ClearStokes()
		{
			_strokes.Clear();
		}

		internal void DrawInk(CanvasDrawingSession session)
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

		internal void AddListeners(InkUnprocessedInput unprocessedInput)
		{
			unprocessedInput.PointerPressed += UnprocessedInput_PointerPressed;
			unprocessedInput.PointerMoved += UnprocessedInput_PointerMoved;
			unprocessedInput.PointerReleased += UnprocessedInput_PointerReleased;
			unprocessedInput.PointerExited += UnprocessedInput_PointerExited;
			unprocessedInput.PointerLost += UnprocessedInput_PointerLost;
		}

		internal void RemoveListeners(InkUnprocessedInput unprocessedInput)
		{
			unprocessedInput.PointerPressed -= UnprocessedInput_PointerPressed;
			unprocessedInput.PointerMoved -= UnprocessedInput_PointerMoved;
			unprocessedInput.PointerReleased -= UnprocessedInput_PointerReleased;
			unprocessedInput.PointerExited -= UnprocessedInput_PointerExited;
			unprocessedInput.PointerLost -= UnprocessedInput_PointerLost;
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
				if (DrawCanvasInvalidated != null)
					DrawCanvasInvalidated();
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
	}
}
