using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Nockx.Android.ClassExtensions;
using Org.BouncyCastle.Crypto.Parameters;
using Header = LessAnnoyingHttp.Header;
using Nockx.Android.Util;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System.Net;
using System.Web;
using Org.BouncyCastle.Asn1.Pkcs;
using LessAnnoyingHttp;

namespace Nockx.Android;

public partial class MainView : Panel {
	private readonly TextBlock _someThing;
	private readonly RsaKeyParameters _privateKey, _personalPublicKey;

	public MainView() {
		AvaloniaXamlLoader.Load(this);

		_someThing = this.FindControl<TextBlock>("SomeThing")!;

		RsaKeyPairGenerator rsaGenerator = new ();
		rsaGenerator.Init(new KeyGenerationParameters(new SecureRandom(), 2048));

		AsymmetricCipherKeyPair keyPair = rsaGenerator.GenerateKeyPair();

		_privateKey = (RsaKeyParameters) keyPair.Private;
		_personalPublicKey = (RsaKeyParameters) keyPair.Public;
	}

	public void Button_Click(object? sender, RoutedEventArgs e) {
		_ = ScanQrCode();
	}

	private async Task ScanQrCode() {
		try {
			string? result = await MainActivity.Instance!.ScanQrCode();
			RsaKeyParameters foreignKey = RsaKeyParametersExtension.FromBase64String(result);
			RequestPrivateKey(foreignKey);

			result = await MainActivity.Instance!.ScanQrCode();
			Console.WriteLine(result);
			byte[] resultBytes = Convert.FromBase64String(result);
			byte[] encryptedAesKey = new byte[256];
			byte[] encryptedPrivateKey = new byte[resultBytes.Length - 256];

			Buffer.BlockCopy(resultBytes, 0, encryptedAesKey, 0, 256);
			Buffer.BlockCopy(resultBytes, 256, encryptedPrivateKey, 0, encryptedPrivateKey.Length);

			byte[] aesKey = Cryptography.DecryptAesKey(encryptedAesKey, _privateKey);
			(byte[] newPrivateKeyBytes, int newPrivateKeyLength) = Cryptography.DecryptWithAes(encryptedPrivateKey, aesKey);
			RsaKeyParameters newPrivateKey = (RsaKeyParameters) PrivateKeyFactory.CreateKey(PrivateKeyInfo.GetInstance(newPrivateKeyBytes[..newPrivateKeyLength]));
			RsaKeyParameters newPublicKey = new (false, newPrivateKey.Modulus, newPrivateKey.Exponent);

			Console.WriteLine(newPublicKey.ToBase64String());
			_someThing.Text = newPublicKey.ToBase64String();
		} catch (Exception e) {
			Console.WriteLine(e);
		}
	}

	public bool RequestPrivateKey(RsaKeyParameters foreignPublicKey) {
		long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		string getVariables = $"requestingUser={HttpUtility.UrlEncode(_personalPublicKey.ToBase64String())}&requestedUser={HttpUtility.UrlEncode(foreignPublicKey.ToBase64String())}&timestamp={timestamp}";
		Response response = Http.Get($"https://...:5000/privateKeyRequest?" + getVariables, [new Header { Name = "Signature", Value = Cryptography.Sign(timestamp.ToString(), _privateKey) }]);
		Console.WriteLine("{0} {1}", response.StatusCode, response.Exception);

		return response.StatusCode == HttpStatusCode.OK;
	}
}
