using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Nockx.Android;

public partial class MainView : Panel {
	public MainView() {
		AvaloniaXamlLoader.Load(this);
		InitializeComponent();
	}
}
