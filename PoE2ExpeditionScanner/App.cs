using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;

namespace PoE2ExpeditionScanner;

public class App : Application
{
	private bool _contentLoaded;

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.5.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			base.StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
			Uri resourceLocator = new Uri("/PoE2ExpeditionScanner;component/app.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[STAThread]
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.5.0")]
	public static void Main()
	{
		App app = new App();
		app.InitializeComponent();
		app.Run();
	}
}
