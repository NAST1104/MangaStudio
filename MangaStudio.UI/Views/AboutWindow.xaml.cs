using System.Reflection;
using System.Windows;

namespace MangaStudio.UI.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        PopulateVersionInfo();
    }

    private void PopulateVersionInfo()
    {
        // App version from assembly
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        AppVersionText.Text = version is not null
            ? $"{version.Major}.{version.Minor}.{version.Build}"
            : "1.0.0";

        // .NET runtime version
        DotNetVersionText.Text = System.Runtime.InteropServices.RuntimeInformation
            .FrameworkDescription;

        // libvips version — safe try/catch in case native lib is not loaded
        try
        {
            int major = NetVips.NetVips.Version(0);
            int minor = NetVips.NetVips.Version(1);
            int patch = NetVips.NetVips.Version(2);
            VipsVersionText.Text = $"{major}.{minor}.{patch}";
        }
        catch
        {
            VipsVersionText.Text = "Not available";
        }

        // ImageSharp version from assembly
        var sharpAsm = typeof(SixLabors.ImageSharp.Image).Assembly.GetName().Version;
        SharpVersionText.Text = sharpAsm is not null
            ? $"{sharpAsm.Major}.{sharpAsm.Minor}.{sharpAsm.Build}"
            : "Unknown";
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}