using System.Windows;
using SveshofReff.Data;

namespace SveshofReff;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DatabaseInitializer.Initialize();
    }
}
