using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
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
            var assembly = Assembly.GetExecutingAssembly();
            
            // InformationalVersionは0.0.0[-alpha.0][+<commit hash>]の形式
            // ビルドメタデータ（+<commit hash>の部分）だけ省いて表示
            // > Build metadata is only included in the assembly informational version.
            // https://github.com/adamralph/minver#versionin
            var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion
                .Split('+').First();
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