using RxRealm.Pages;

namespace RxRealm;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new NavigationPage(new ProductsPage());
    }
}