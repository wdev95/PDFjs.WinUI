using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;


namespace PDFjs.WinUI
{
	public sealed partial class PDFjsViewer : UserControl
	{

		public StorageFile File;
		public event ErrorEventHandler ErrorOccured;
		private bool loaded = false;
		private bool changing = false;

		public event EventHandler<double> SyncTeXRequested;

		private new ElementTheme ActualTheme = ElementTheme.Dark;

		public static readonly DependencyProperty PageProperty = DependencyProperty.Register("Page", typeof(int), typeof(PDFjsViewer), new PropertyMetadata(1, (d, e) => ((PDFjsViewer)d).PropertyChanged(d, e, "page")));
		public int Page
		{
			get => (int)GetValue(PageProperty);
			set => SetValue(PageProperty, value);
		}
		private async void PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e, string name = "")
		{
			if (!changing)
				if (await Get<int>(name) != int.Parse(e.NewValue.ToString()))
					await Set(name, e.NewValue.ToString());
		}

		public static readonly DependencyProperty ThemeProperty = DependencyProperty.Register("Theme", typeof(string), typeof(PDFjsViewer), new PropertyMetadata("Dark", (d, e) => ((PDFjsViewer)d).ThemeChanged(d, e)));
		public string Theme
		{
			get => (string)GetValue(ThemeProperty);
			set => SetValue(ThemeProperty, value);
		}

		public PDFjsViewer()
		{
			this.InitializeComponent();
			Environment.SetEnvironmentVariable("WEBVIEW2_DEFAULT_BACKGROUND_COLOR", "0");
		}

		private void ThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			try
			{
				if ((string)e.NewValue == "Default")
				{
					var backcolor = new Windows.UI.ViewManagement.UISettings().GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
					ActualTheme = backcolor.Equals(Colors.White) ? ElementTheme.Light : ElementTheme.Dark;
				}
				else
				{
					ActualTheme = (ElementTheme)Enum.Parse(typeof(ElementTheme), (string)e.NewValue);
				}

				if (Background is SolidColorBrush colorBrush)
				{
					PDFjsViewerWebView.DefaultBackgroundColor = colorBrush.Color;
				}

				if (PDFjsViewerWebView.CoreWebView2 != null)
				{
					PDFjsViewerWebView.Source = new("https://pdfjs/web/viewer.html");
				}
			}
			catch (Exception ex)
			{
				ErrorOccured?.Invoke(this, new(ex));
			}
		}

		private async void PDFjsViewerWebView_Loaded(object sender, RoutedEventArgs e)
		{

		}

		private async void PDFjsViewerWebView_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
		{
			sender.CoreWebView2.Settings.AreDevToolsEnabled = false;
			sender.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;

			sender.CoreWebView2.Settings.AreHostObjectsAllowed = true;
			sender.CoreWebView2.Settings.IsScriptEnabled = true;
			sender.CoreWebView2.Settings.IsPinchZoomEnabled = false;
			sender.CoreWebView2.Settings.IsStatusBarEnabled = false;
			sender.CoreWebView2.Settings.IsSwipeNavigationEnabled = false;
			sender.CoreWebView2.Settings.IsZoomControlEnabled = false;
			sender.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
			string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
			sender.CoreWebView2.SetVirtualHostNameToFolderMapping("pdfjs", path, CoreWebView2HostResourceAccessKind.DenyCors);
			sender.CoreWebView2.DOMContentLoaded += (a, b) => { updateTheme(); };
			sender.CoreWebView2.NavigationCompleted += (a, b) => { Reload(); };
			sender.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
			PDFjsViewerWebView.Source = new("https://pdfjs/web/viewer.html");
			loaded = true;
		}

		private void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
		{
			string message = args.WebMessageAsJson;

			if (int.TryParse(message, out int page))
			{
				changing = true;
				Page = page;
				changing = false;
			}
			else
			{
				JObject obj = JObject.Parse(message);
				string top = (string)obj["top"];
				if (double.TryParse(top, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double t))
				{
					SyncTeXRequested?.Invoke(this, t);
				}
			}
		}

		private async void updateTheme()
		{
			await PDFjsViewerWebView.EnsureCoreWebView2Async();
			await PDFjsViewerWebView.ExecuteScriptAsync($"setTheme('{ActualTheme.ToString()}')");
		}

		private async void Reload()
		{
			if (File != null)
			{
				await OpenPDF(File);
			}
		}
		public async Task<bool> OpenPDF(StorageFile pdffile)
		{
			try
			{
				File = pdffile;
				Stream stream = await pdffile.OpenStreamForReadAsync();
				byte[] buffer = new byte[stream.Length];
				stream.Read(buffer, 0, (int)stream.Length);
				var asBase64 = Convert.ToBase64String(buffer);
				await PDFjsViewerWebView.ExecuteScriptAsync("window.openPdfAsBase64('" + asBase64 + "')");
				//await Task.Delay(500);
				//int spreadmode = ControlGrid.ActualSize.X < 1000 ? 0 : 1;
				//if (spreadmode != currentSpreadMode)
				//{
				//	await PDFjsViewerWebView.ExecuteScriptAsync($"PDFViewerApplication.pdfViewer.spreadMode = {spreadmode};");
				//	currentSpreadMode = spreadmode;
				//}
				return true;

			}
			catch (Exception ex)
			{
				ErrorOccured?.Invoke(this, new(ex));
				return false;
			}
		}

		private async void PDFjsViewerWebView_Loading(FrameworkElement sender, object args)
		{
			if (Background is SolidColorBrush colorBrush)
			{
				PDFjsViewerWebView.DefaultBackgroundColor = colorBrush.Color;
			}

			Theme = RequestedTheme.ToString();

			await PDFjsViewerWebView.EnsureCoreWebView2Async();
		}

		public async Task<T> Get<T>(string name, string service = "PDFViewerApplication.")
		{
			try
			{
				await PDFjsViewerWebView.EnsureCoreWebView2Async();
				string returnstring = await PDFjsViewerWebView.ExecuteScriptAsync(service + name);
				var converter = TypeDescriptor.GetConverter(typeof(T));
				
				return (T)converter?.ConvertFromString(null, CultureInfo.InvariantCulture, returnstring) ?? default(T);
			}
			catch
			{
				return default(T);
			}
		}

		public async Task<bool> Set(string name, string value, string service = "PDFViewerApplication.")
		{
			try
			{
				await PDFjsViewerWebView.EnsureCoreWebView2Async();
				string returnstring = await PDFjsViewerWebView.ExecuteScriptAsync(service + name + $" = {value ?? string.Empty}");
				return true;
			}
			catch
			{
				return false;
			}
		}

		public Task<int> GetPage { get => Get<int>("page"); }


		public Task<int> LastPage { get => Get<int>("pagesCount"); }
		public Task<double> CurrentScale { get => Get<double>("pdfViewer.currentScale"); }

		public Task<string> Author { get => Get<string>("metadata?.get('dc:creator')"); }
		public Task<string> Title { get => Get<string>("metadata?.get('dc:title')"); }
		public Task<string> Description { get => Get<string>("metadata?.get('dc:description')"); }
		public Task<string> Keywords { get => Get<string>("metadata?.get('pdf:Keywords')"); }
		public Task<string> Producer { get => Get<string>("metadata?.get('pdf:Producer')"); }
		public Task<string> CreatorTool { get => Get<string>("metadata?.get('xmp:CreatorTool')"); }

		public async Task<bool> ScrollToPosition(int page, double yoffset, double depth = 0)
		{
			await PDFjsViewerWebView.EnsureCoreWebView2Async();
			await Set("page", page.ToString());
			await PDFjsViewerWebView.ExecuteScriptAsync($"BaseViewer._resetCurrentPageView();");

			double scale = await CurrentScale;
			double offset = (yoffset - depth - 10) * scale * 2.03;
			await PDFjsViewerWebView.ExecuteScriptAsync($"PDFViewerApplication.pdfViewer.container.scrollTop += {offset:0.000}");
			//	await PDFjsViewerWebView.ExecuteScriptAsync($"scrollToYOffset({yoffset:0.000});");

			return true;
		}

		private int currentSpreadMode = 0;

		private async void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (loaded)
			{
				int spreadmode = e.NewSize.Width < 1000 ? 0 : 1;
				if (spreadmode != currentSpreadMode)
				{
					await PDFjsViewerWebView.ExecuteScriptAsync($"PDFViewerApplication.pdfViewer.spreadMode = {spreadmode};");
					currentSpreadMode = spreadmode;
				}
			}
		}
	}
}
