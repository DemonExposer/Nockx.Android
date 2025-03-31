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
using System.Text;
using System.Web;
using Org.BouncyCastle.Asn1.Pkcs;

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
		Response response = Get($"https://...:5000/privateKeyRequest?" + getVariables, [new Header { Name = "Signature", Value = Cryptography.Sign(timestamp.ToString(), _privateKey) }]);
		Console.WriteLine("{0} {1}", response.StatusCode, response.Exception);

		return response.StatusCode == HttpStatusCode.OK;
	}

	private class Response {
		/// <summary>
		/// The response of the HTTP request (is empty when the request failed)
		/// </summary>
		public string Body { get; internal init; } = "";

		/// <summary>
		/// Specifies whether the request was successful
		/// </summary>
		public bool IsSuccessful { get; internal init; }

		/// <summary>
		/// The HTTP status code returned by the server
		/// </summary>
		public HttpStatusCode StatusCode { get; internal init; }

		/// <summary>
		/// Any exception that was thrown during the sending of the request (is null when no exception was thrown)
		/// </summary>
		public Exception? Exception { get; internal init; }

		internal Response() { }
	}

	private static Response Get(string endpoint, Header[]? headers = null) {
		using HttpClient client = new();
		client.Timeout = TimeSpan.FromSeconds(10000);
		HttpRequestMessage request = new() {
			RequestUri = new Uri(endpoint),
			Method = HttpMethod.Get
		};

		if (headers != null)
			foreach (Header header in headers)
				request.Headers.Add(header.Name, header.Value);

		HttpResponseMessage response;
		try {
			response = client.SendAsync(request).Result;
		} catch (TaskCanceledException) {
			throw new TimeoutException($"Timeout waiting for response for request to {endpoint}");
		} catch (Exception e) {
			return new Response { IsSuccessful = false, Body = "", Exception = e };
		}

		return new Response { IsSuccessful = response.IsSuccessStatusCode, Body = response.Content.ReadAsStringAsync().Result, StatusCode = response.StatusCode };
	}

	private static Response BodyRequest(string endpoint, HttpMethod method, string body, Header[]? headers, string contentType) {
		using HttpClient client = new();
		client.Timeout = TimeSpan.FromSeconds(10000);
		HttpRequestMessage request = new() {
			RequestUri = new Uri(endpoint),
			Method = method,
			Content = new StringContent(body, Encoding.UTF8, contentType)
		};

		if (headers != null)
			foreach (Header header in headers)
				request.Headers.Add(header.Name, header.Value);

		HttpResponseMessage response;
		try {
			response = client.SendAsync(request).Result;
		} catch (TaskCanceledException) {
			throw new TimeoutException($"Timeout waiting for response for request to {endpoint}");
		} catch (Exception e) {
			return new Response { IsSuccessful = false, Body = "", Exception = e };
		}

		return new Response { IsSuccessful = response.IsSuccessStatusCode, Body = response.Content.ReadAsStringAsync().Result, StatusCode = response.StatusCode };
	}

	private static Response Post(string endpoint, string body, Header[]? headers = null, string contentType = "application/json") => BodyRequest(endpoint, HttpMethod.Post, body, headers, contentType);
}
