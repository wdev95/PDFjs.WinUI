using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PDFjs.WinUI.TestApp
{
	/// <summary>
	/// An empty window that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainWindow : Window
	{
		public MainWindow()
		{
			this.InitializeComponent();
			Title = "PDFjs.WinUI.TestApp";
		}

		private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Viewer.Theme = (string)e.AddedItems.First();
		}

		private async  void Btn_Open(object sender, RoutedEventArgs e)
		{
			var file = await StorageFile.GetFileFromApplicationUriAsync(new("ms-appx:///Assets/prd_thesis.pdf"));
			await Viewer.OpenPDF(file);
		}

		private async void Nbx_NumberChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
		{
			if (Viewer!= null)
			await Viewer.Set("page", ((int)sender.Value).ToString());
		}
	}
}
