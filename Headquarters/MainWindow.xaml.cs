using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
            var version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
            Title = $"Headquarters {version}";

            var viewModel = new MainWindowViewModel(this)
            {
                GetOrderedTabsFunc = () => MainTabControl.GetOrderedHeaders()
                    .Select(item => item.DataContext).OfType<MainTabViewModel>()
            };

            DataContext = viewModel;
        }
    }
}