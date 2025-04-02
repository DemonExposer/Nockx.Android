namespace Nockx.Android.Util;

public static class Constants {
	public static readonly string PrivateKeyFile = Path.Combine(Application.Context.FilesDir!.AbsolutePath, "private_key.pem");
	public static readonly string PublicKeyFile = Path.Combine(Application.Context.FilesDir!.AbsolutePath, "public_key.pem");
	public static readonly string ChatsFile = Path.Combine(Application.Context.FilesDir!.AbsolutePath, "chats.json");
	public static readonly string SettingsFile = Path.Combine(Application.Context.FilesDir!.AbsolutePath, "settings.json");

	public const int KEY_SIZE_BITS = 2048;
	public const int KEY_SIZE_BYTES = KEY_SIZE_BITS / 8;
}