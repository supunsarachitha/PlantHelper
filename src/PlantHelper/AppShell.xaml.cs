using PlantHelper.Services;

namespace PlantHelper;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routes.Register();
	}
}
