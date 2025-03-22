using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Button = Avalonia.Controls.Button;

namespace Nockx.Android;

public partial class MainView : Panel {
	public MainView() {
		AvaloniaXamlLoader.Load(this);
	}

	public void Button_Click(object? sender, RoutedEventArgs e) {
		Button button = (Button) sender!;
		button.Content += ".";
		Console.WriteLine("button pressed");
	}
}
