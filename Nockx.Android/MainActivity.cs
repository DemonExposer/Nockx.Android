using Android.Content.PM;
using Avalonia.Android;
using Avalonia.Controls;
using Xamarin.Essentials;
using ZXing.Mobile;
using Result = ZXing.Result;

namespace Nockx.Android;

[Activity(Label = "@string/app_name", Theme = "@style/MyTheme.NoActionBar", MainLauncher = true, 
	ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
public class MainActivity : Activity {
	protected override async void OnCreate(Bundle? bundle) {
		base.OnCreate(bundle);
		Platform.Init(this, bundle);
		
		MobileBarcodeScanner scanner = new ();
		Result result = await scanner.Scan();

		if (result != null) {
			Console.WriteLine("code: {0}", result.Text);
		}
	}
}