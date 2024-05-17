using RxRealm.Pages;

namespace RxRealm;

public partial class App
{
    public App()
    {
        InitializeComponent();

        MainPage = new TabbedPage
        {
            Children =
            {
                new NavigationPage(new PaginatedProductsPage()) { Title = "Paginated" },
                new NavigationPage(new VirtualizedProductsPage()) { Title = "Virtualized" },
                new NavigationPage(new WrapperCollectionProductsPage()) { Title = "Wrapper Collection" },
            }
        };
    }
}
