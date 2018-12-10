using System;
using System.Linq;
using System.Windows;

namespace Headquarters
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        IPListViewModel ipList;
        ScriptsViewModel scriptsVM;

        public MainWindow()
        {
            InitializeComponent();

            var paramManager = ParameterManager.Instance;
            paramManager.Load(".\\param.json");
            tbUserName.DataContext = new Parameter(ParameterManager.SpecialParamName.UserName);
            pbUserPassword.Password = paramManager.userPassword;
            pbUserPassword.PasswordChanged += (o, e) => paramManager.userPassword = pbUserPassword.Password;

            ipList = IPListViewModel.Instance;
            ipList.Load(".\\iplist.csv");
            ipList.Bind(dgIPList);

            scriptsVM = new ScriptsViewModel(".");
            tsScripts.DataContext = scriptsVM;

            ipList.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == ipList.selectedPropertyName) UpdateRunButton();
            };

            UpdateRunButton();

            ScriptButtons.DataContext = scriptsVM;
        }

        private void OnClickSelectScript(object sender, RoutedEventArgs e)
        {
            tsScripts.SelectedIndex += 1;
        }

        private void OnClickRun(object sender, RoutedEventArgs e)
        {
            var task = scriptsVM.Current?.Run(ipList.selectedParams.ToList());
            if (task != null)
            {
                RunButtonSelector.SelectedIndex = 2;

                task.ContinueWith((t) =>
                {
                    UpdateRunButton();
                });
            }
        }

        void UpdateRunButton()
        {
            var selectAny = ipList.IsSelected ?? true;
            RunButtonSelector.Dispatcher.BeginInvoke(new Action(() => RunButtonSelector.SelectedIndex = selectAny ? 1 : 0));
        }

        private void OnClickStop(object sender, RoutedEventArgs e)
        {
            scriptsVM.Current?.Stop();
            RunButtonSelector.SelectedIndex = 1;
        }

        private void OnClickSaveIPList(object sender, RoutedEventArgs e)
        {
            ipList.Save();
        }
    }
}