using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Dragablz;

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

        // xaml上でShowDefaultAddButtonをFalseにしても、
        //  *  <dragablz:TabablzControl ShowDefaultAddButton="False">
        // タブが一つのとき、クリックしている間だけタブとHeaderSuffixContentとの間にDefaultAddButton分のスペースが出来てしまう
        // 原因がわからずとりあえず無理やり外部からVisibilityを変更する
        private void TabablzControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TabablzControl tabablzControl)
            {
                if (tabablzControl.Template.FindName("DefaultAddButton", tabablzControl) is Button defaultAddButton)
                {
                    defaultAddButton.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}