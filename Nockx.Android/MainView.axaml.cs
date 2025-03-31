using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Nockx.Android.ClassExtensions;
using Org.BouncyCastle.Crypto.Parameters;
using static Android.Preferences.PreferenceActivity;
using System.Text.Json;
using LessAnnoyingHttp;
using Header = LessAnnoyingHttp.Header;
using System.Text.Json.Nodes;
using Nockx.Android.Util;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System.Net;
using System.Text;

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
			_someThing.Text = result;
			RsaKeyParameters foreignKey = RsaKeyParametersExtension.FromBase64String(result);
			Console.WriteLine("id: {0}", SendMessage("hoi", foreignKey));
		} catch (Exception e) {
			Console.WriteLine(e);
		}
	}

	public long SendMessage(string message, RsaKeyParameters foreignPublicKey) {
		Message encryptedMessage = Cryptography.Encrypt(message, _personalPublicKey, foreignPublicKey, _privateKey);
		JsonObject body = new() {
			["sender"] = new JsonObject {
				["key"] = _personalPublicKey.ToBase64String(),
				["displayName"] = _personalPublicKey.ToBase64String()
			},
			["receiver"] = new JsonObject {
				["key"] = foreignPublicKey.ToBase64String(),
				["displayName"] = ""
			},
			["text"] = encryptedMessage.Body,
			["senderEncryptedKey"] = encryptedMessage.SenderEncryptedKey,
			["receiverEncryptedKey"] = encryptedMessage.ReceiverEncryptedKey,
			["signature"] = encryptedMessage.Signature,
			["timestamp"] = encryptedMessage.Timestamp
		};

		string bodyString = JsonSerializer.Serialize(body);
		Response response = Post($"https://...:5000/messages", bodyString, [new Header { Name = "Signature", Value = Cryptography.Sign(bodyString, _privateKey) }]);
		Console.WriteLine("{0} {1}", response.StatusCode, response.Exception);
		return long.Parse(response.Body);
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
