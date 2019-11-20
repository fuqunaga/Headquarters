using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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

            scriptsVM = new ScriptsViewModel(".", @".\Scripts");
            ScriptButtons.DataContext = scriptsVM;

            ipList = IPListViewModel.Instance;
            ipList.Load(".\\iplist.csv");
            ipList.Bind(dgIPList);

            ipList.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == ipList.selectedPropertyName) UpdateRunButton();

                if (e.PropertyName == nameof(IPListViewModel.Items))
                {
                    OnChangeIPList();
                }
            };

            var pb = Resources["TopPasswordBox"] as PasswordBox;
            pb.Password = ParameterManager.UserPassword.Value;

            UpdateRunButton();
            OnChangeIPList();
        }

        void OnChangeIPList()
        {
            tsScripts.DataContext = null;
            tsScripts.DataContext = scriptsVM;

            tbUserName.DataContext = ParameterManager.UserName;
            UserPassword.DataContext = ParameterManager.UserPassword;
        }

        private void OnClickSelectScript(object sender, RoutedEventArgs e)
        {
            scriptsVM.SetCurrent(((Button)sender).Content.ToString());
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



        protected override void OnClosed(EventArgs e)
        {
            ipList.Save();
            ParameterManager.Instance.Save();

            base.OnClosed(e);
        }


        #region IPList Context Menu

        private void OnHeaderContextMenuOpen(object sender, System.Windows.Controls.ContextMenuEventArgs e)
        {
            ipList.OnHeaderContextMenuOpen(sender);
        }

        private void OnClickAddColumn(object sender, RoutedEventArgs e)
        {
            ipList.AddColumn(sender);
        }

        private void OnClickDeleteColumn(object sender, RoutedEventArgs e)
        {
            ipList.DeleteColumn(sender);
        }

        private void OnClickRenameColumn(object sender, RoutedEventArgs e)
        {
            ipList.RenameColumn(sender);
        }

        #endregion

        private void OnTopPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (UserPassword.DataContext is Parameter p)
            {
                p.Value = ((PasswordBox)sender).Password;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.ToString());
        }
    }
}