using Avalonia;
using Avalonia.Markup.Xaml;

namespace A2Ui.Avalonia.Composer;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
