using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Windows;
using System.Windows.Controls;

namespace Headquarters
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IpListBarViewModel _ipListBarViewModel;
        private readonly IpListDataGridViewModel _ipListDataGrid;
        private readonly ScriptsViewModel _scriptsVM;

        public MainWindow()
        {
            InitializeComponent();

            var paramManager = ParameterManager.Instance;
            paramManager.Load(".\\param.json");

            _ipListDataGrid = IpListDataGridViewModel.Instance;
            // _ipList.Bind(dgIPList);

            _ipListDataGrid.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == IpListDataGridViewModel.SelectedPropertyName) UpdateRunButton();

                if (e.PropertyName == nameof(IpListDataGridViewModel.Items))
                {
                    OnChangeIPList();
                }
            };
            
            _ipListBarViewModel = new IpListBarViewModel();
            IpListBar.DataContext = _ipListBarViewModel;
            _ipListBarViewModel.Initialize(_ipListDataGrid);
            
            _scriptsVM = new ScriptsViewModel(".", @".\Scripts");
            ScriptButtons.DataContext = _scriptsVM;


            var pb = Resources["TopPasswordBox"] as PasswordBox;
            pb.Password = ParameterManager.UserPassword.Value;

            UpdateRunButton();
            OnChangeIPList();
        }

        private void OnChangeIPList()
        {
            psScripts.DataContext = null;
            psScripts.DataContext = _scriptsVM;

            tbUserName.DataContext = ParameterManager.UserName;
            UserPassword.DataContext = ParameterManager.UserPassword;
        }

        private void OnClickSelectScript(object sender, RoutedEventArgs e)
        {
            _scriptsVM.SetCurrent(((Button)sender).Content.ToString());
            psScripts.SelectedIndex += 1;
        }

        private void OnClickRun(object sender, RoutedEventArgs e)
        {
            var task = _scriptsVM.Current?.Run(_ipListDataGrid.SelectedParams.ToList());
            if (task != null)
            {
                RunButtonSelector.SelectedIndex = 2;

                task.ContinueWith((t) =>
                {
                    UpdateRunButton();
                });
            }
        }

        private void UpdateRunButton()
        {
            var selectAny = _ipListDataGrid.IsAllItemSelected ?? true;
            RunButtonSelector.Dispatcher.BeginInvoke(new Action(() => RunButtonSelector.SelectedIndex = selectAny ? 1 : 0));
        }

        private void OnClickStop(object sender, RoutedEventArgs e)
        {
            _scriptsVM.Current?.Stop();
            RunButtonSelector.SelectedIndex = 1;
        }



        protected override void OnClosed(EventArgs e)
        {
            // _ipList.Save();
            ParameterManager.Instance.Save();

            base.OnClosed(e);
        }

        private void OnTopPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (UserPassword.DataContext is Parameter p)
            {
                p.Value = ((PasswordBox)sender).Password;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {

            var pi = new ProcessStartInfo()
            {
                FileName = e.Uri.ToString(),
                UseShellExecute = true,
            };

            Process.Start(pi);
        }
    }
}