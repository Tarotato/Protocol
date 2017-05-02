using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
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
		public MainCanvas()
		{
			this.InitializeComponent();

			// Add handlers for InkToolbar events.
			inkToolbar.Loading += inkToolbar_Loading;

			// Add Touch to input types
			inkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Touch;

			// Turn on multi pointer input
			inkCanvas.InkPresenter.ActivateCustomDrying();
			inkCanvas.InkPresenter.SetPredefinedConfiguration(Windows.UI.Input.Inking.InkPresenterPredefinedConfiguration.SimpleMultiplePointer);
		}

		private void inkToolbar_Loading(FrameworkElement sender, object args)
		{
			// Clear all built-in buttons from the InkToolbar.
			inkToolbar.InitialControls = InkToolbarInitialControls.None;

			// Add only the ballpoint pen, pencil, and eraser.
			// Note that the buttons are added to the toolbar in the order
			// defined by the framework, not the order we specify here.
			InkToolbarBallpointPenButton ballpoint = new InkToolbarBallpointPenButton();
			InkToolbarPencilButton pencil = new InkToolbarPencilButton();
			InkToolbarEraserButton eraser = new InkToolbarEraserButton();
			inkToolbar.Children.Add(eraser);
			inkToolbar.Children.Add(ballpoint);
			inkToolbar.Children.Add(pencil);
		}
	}
}
