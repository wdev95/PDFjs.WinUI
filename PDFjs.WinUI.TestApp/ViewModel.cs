using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PDFjs.WinUI.TestApp
{
	public class ViewModel : Bindable
	{
		public ViewModel()
		{

		}

		public string Theme { get => Get("Light"); set { Set(value); RequestedTheme = (ElementTheme)Enum.Parse(typeof(ElementTheme), value); } }
		public ElementTheme RequestedTheme { 
			get => Get((ElementTheme)Enum.Parse(typeof(ElementTheme),Theme)); 
			set => Set(value); 
		}

		public bool IsInternalViewerActive { get => Get(true); set => Set(value); }
		public string[] ThemeOptions { get => new[] { "Default", "Dark", "Light" }; }
		public string PDFPath { get => Get("ms-appx:///Assets/Test.pdf"); set => Set(value); }
		public int Page { get => Get(1); set => Set(value); }

		public bool IsDefaultContextMenuEnabled { get => Get(true); set => Set(value); }
		public bool IsDevToolsEnabled { get => Get(true); set => Set(value); }

	}

	public class Bindable : INotifyPropertyChanged
	{
		private Dictionary<string, object> _properties = new Dictionary<string, object>();

		public event PropertyChangedEventHandler PropertyChanged;

		protected T Get<T>(T defaultVal = default, [CallerMemberName] string name = null)
		{
			if (!_properties.TryGetValue(name, out object value))
			{
				value = _properties[name] = defaultVal;
			}
			return (T)value;
		}

		protected void Set<T>(T value, [CallerMemberName] string name = null)
		{
			if (Equals(value, Get<T>(value, name)))
				return;
			_properties[name] = value;
			OnPropertyChanged(name);
		}

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
