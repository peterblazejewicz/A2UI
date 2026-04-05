using A2Ui.Avalonia.Controls;
using A2Ui.Avalonia.Gallery.ViewModels;
using Avalonia.Controls;

namespace A2Ui.Avalonia.Gallery.Views;

public partial class GalleryWindow : Window
{
    public GalleryWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is GalleryViewModel vm)
        {
            vm.SurfaceRefreshRequested += (_, _) =>
                this.FindControl<A2UiSurface>("SurfaceHost")?.Refresh();

            A2UiSurface? surface = this.FindControl<A2UiSurface>("SurfaceHost");
            if (surface is not null)
                surface.UserActionFired += (_, args) => vm.LogAction(args);
        }
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is GalleryViewModel vm)
        {
            try
            {
                await vm.InitializeAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Title = $"A2UI Gallery — Error: {ex.Message}";
            }
        }
    }
}
