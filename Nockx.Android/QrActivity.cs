using Android.Content;
using Android.Content.PM;
using Xamarin.Essentials;
using ZXing.Mobile;

namespace Nockx.Android;

[Activity(Label = "@string/app_name", Theme = "@style/MyTheme.NoActionBar", MainLauncher = true,
	ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
public class QrActivity : Activity {
	protected override async void OnCreate(Bundle? bundle) {
		base.OnCreate(bundle);
		Platform.Init(this, bundle);

		MobileBarcodeScanner scanner = new();
		ZXing.Result result = await scanner.Scan();

		if (result != null) {
			Intent intent = new ();
			intent.PutExtra("resultData", result.Text);
			SetResult(Result.Ok, intent);
			Finish();
		} else {
			Intent intent = new ();
			intent.PutExtra("resultData", (string?) null);
			SetResult(Result.Ok, intent);
			Finish();
		}
	}
}