using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Avalonia.Android;

namespace Nockx.Android;

[Activity(Label = "@string/app_name", Theme = "@style/MyTheme.NoActionBar", MainLauncher = true, 
	ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
public class MainActivity : AvaloniaMainActivity<App> {
	public static MainActivity? Instance { get; private set; }

	private const int RequestCode = 1001;
	private TaskCompletionSource<string?>? _resultTaskCompletionSource;

	protected override void OnCreate(Bundle? savedInstanceState) {
		base.OnCreate(savedInstanceState);
		Instance = this;
	}

	public Task<string?> ScanQrCode() {
		_resultTaskCompletionSource = new TaskCompletionSource<string?>();
		Intent intent = new (this, typeof(QrActivity));
		StartActivityForResult(intent, RequestCode);
		return _resultTaskCompletionSource.Task;
	}

	protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent? data) {
		base.OnActivityResult(requestCode, resultCode, data);

		if (requestCode == RequestCode && resultCode == Result.Ok && data != null) {
			string? result = data.GetStringExtra("resultData");
			_resultTaskCompletionSource?.SetResult(result);
		} else {
			_resultTaskCompletionSource?.TrySetResult(null);
		}
	}
}