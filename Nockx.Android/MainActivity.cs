using Android.Content.PM;
using Avalonia.Android;
using Avalonia.Controls;

namespace Nockx.Android;

[Activity(Label = "@string/app_name", Theme = "@style/MyTheme.NoActionBar", MainLauncher = true, 
	ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
public class MainActivity : AvaloniaMainActivity<App> {
	
}