using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Protocol
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class StartPage : Page
	{
		public StartPage()
		{
			this.InitializeComponent();
		}

		private void OnNewProjectClick(object sender, RoutedEventArgs e)
		{
			ProjectButtons.Visibility = Visibility.Collapsed;
			CanvasSizeButtons.Visibility = Visibility.Visible;
			Title.Text = "Select Desired Platform of Prototype";
		}

		private void OnOpenProjectClick(object sender, RoutedEventArgs e)
		{
			// implement later
		}

		private void OnBackClick(object sender, RoutedEventArgs e)
		{
			ProjectButtons.Visibility = Visibility.Visible;
			CanvasSizeButtons.Visibility = Visibility.Collapsed;
			Title.Text = "Welcome to Protocol";
		}

		private void OnMobileSizeClick(object sender, RoutedEventArgs e)
		{

		}

		private void OnDesktopSizeClick(object sender, RoutedEventArgs e)
		{

		}

		private void OnHubSizeClick(object sender, RoutedEventArgs e)
		{
			this.Frame.Navigate(typeof(MainCanvas));
		}
	}
}
