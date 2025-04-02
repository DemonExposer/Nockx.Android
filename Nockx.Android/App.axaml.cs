using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Nockx.Android.Util;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.OpenSsl;
using System.Diagnostics;

namespace Nockx.Android;

public partial class App : Avalonia.Application {
	private bool _isKeyLoadedSuccessfully, _showKeyDecider;

	public override void Initialize() {
		CheckOrGenerateKeys();
		AvaloniaXamlLoader.Load(this);
	}

	private void CheckOrGenerateKeys() {
		if (!File.Exists(Constants.PrivateKeyFile) && !File.Exists(Constants.PublicKeyFile)) {
			_showKeyDecider = true;
			// Generate RSA key
			Stopwatch sw = Stopwatch.StartNew();
			RsaKeyPairGenerator rsaGenerator = new ();
			rsaGenerator.Init(new KeyGenerationParameters(new SecureRandom(), Constants.KEY_SIZE_BITS));

			AsymmetricCipherKeyPair keyPair = rsaGenerator.GenerateKeyPair();

			sw.Stop();
			Console.WriteLine(sw.ElapsedMilliseconds); // I checked it, it takes 34 seconds. It definitely needs some loading animation

			RsaKeyParameters privateKey = (RsaKeyParameters) keyPair.Private;
			RsaKeyParameters publicKey = (RsaKeyParameters) keyPair.Public;

			// Write private and public keys to files
			using (TextWriter textWriter = new StreamWriter(Constants.PrivateKeyFile)) {
				PemWriter pemWriter = new (textWriter);
				pemWriter.WriteObject(privateKey);
				pemWriter.Writer.Flush();
			}

			using (TextWriter textWriter = new StreamWriter(Constants.PublicKeyFile)) {
				PemWriter pemWriter = new (textWriter);
				pemWriter.WriteObject(publicKey);
				pemWriter.Writer.Flush();
			}

			_isKeyLoadedSuccessfully = true;
		} else if (!File.Exists(Constants.PrivateKeyFile) || !File.Exists(Constants.PublicKeyFile)) {
			_isKeyLoadedSuccessfully = false;
		} else {
			_isKeyLoadedSuccessfully = true;
		}
	}

	public override void OnFrameworkInitializationCompleted() {
		if (ApplicationLifetime is ISingleViewApplicationLifetime singleView) {
		//	if (_showKeyDecider)
			singleView.MainView = new KeyDeciderView();
		}

		base.OnFrameworkInitializationCompleted();
	}
}