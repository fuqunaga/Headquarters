using System.Linq;
using System.Windows;

namespace Headquarters
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var viewModel = new MainWindowViewModel();
            DataContext = viewModel;
            Closing += viewModel.OnClosing;

            viewModel.GetOrderedTabsFunc = () => MainTabControl.GetOrderedHeaders()
                .Select(item => item.DataContext).OfType<MainTabViewModel>();
        }
    }
}