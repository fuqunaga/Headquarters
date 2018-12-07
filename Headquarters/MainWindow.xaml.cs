using MaterialDesignThemes.Wpf.Transitions;
using NetTools;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Headquarters
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        IPList ipList;
        ScriptsViewModel scriptVM;

        public MainWindow()
        {
            InitializeComponent();

            var paramManager = ParameterManager.Instance;
            paramManager.Load(".\\param.json");
            tbUserName.DataContext = new Parameter(ParameterManager.SpecialParamName.UserName);
            pbUserPassword.Password = paramManager.userPassword;
            pbUserPassword.PasswordChanged += (o, e) => paramManager.userPassword = pbUserPassword.Password;
            
            ipList = new IPList(".\\iplist.csv");
            ipList.Bind(dgIPList);

            scriptVM = new ScriptsViewModel(".");
            tsScripts.DataContext = scriptVM;
            

            ScriptButtons.DataContext = scriptVM;
            
        }

        private void OnClickSelectScript(object sender, RoutedEventArgs e)
        {
            /*
            tsScripts.SetCurrentValue(Selector.SelectedIndexProperty, tsScripts.SelectedIndex + 1);
            scriptVM.Current = e.Parameter as ScriptViewModel;
            */
            Debug.WriteLine(sender);
            tsScripts.SelectedIndex += 1;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            RunCurrentScript();
        }

        void RunCurrentScript()
        {
            var script = scriptVM.Current?.script;
            script.Load();

            tbError.Text = "";
            ipList.selectedParams.ToList()
                .ForEach(param =>
                {
                    IPAddressRange range;
                    IPAddressRange.TryParse(param.ipStr, out range);

                    var ipStrList = range?.AsEnumerable().Select(ip => ip.ToString()).ToList() ?? (new[] { param.ipStr }).ToList();

                    var paramIsValid = true;
                    if (ipStrList.Count > 100)
                    {
                        var result = MessageBox.Show($"IP[{param.ipStr}] means {ipStrList.Count} targets.\n Continue?", "Many targets", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                        paramIsValid = (result == MessageBoxResult.OK);
                    }

                    if (paramIsValid)
                    {
                        ipStrList.ForEach(ipStr =>
                        {
                            Task.Run(() =>
                            {
                                var result = script.Run(ipStr);
                                tbError.Dispatcher.Invoke(() => tbError.Text += $"{ipStr}:\n{string.Join("\n", result.objs)}\n{string.Join("\n", result.errors)}\n\n");
                            });
                        });
                    }
                });
        }
    }
}
