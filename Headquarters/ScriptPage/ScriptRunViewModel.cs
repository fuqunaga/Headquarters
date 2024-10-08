using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NetTools;

namespace Headquarters
{
    public class ScriptRunViewModel : ViewModelBase
    {
        #region Type Define

        public enum RunButtonMode
        {
            SelectIp = 0,
            Run = 1,
            Stop = 2,
        };
        

        public static class OwnParameter
        {
            public const string MaxTaskNum = "MaxTaskNum";
        }

        public class OutputData
        {
            public string name;
            public PSInvocationStateInfo info;
            public PowerShellScript.Result result;

            public override string ToString()
            {
                var prefix = (result == null) ? "" : (result.IsSucceed ? "✔" : "⚠");
                var label = $"{prefix} {name}: {info?.State}";
                var resultStr = GetResultString();

                var ret = label;
                if (resultStr.Any()) ret += "\n" + resultStr;

                return ret;
            }

            string GetResultString()
            {
                string ToStr<T>(IEnumerable<T> collection)
                {
                    string str = "";
                    if (collection?.Any() ?? false)
                    {
                        str = string.Join("\n ", collection.Select(elem => $" {elem.ToString()}")) + "\n";
                    }
                    return str;
                }

                var ret = "";
                if (result != null)
                {
                    var objStr = ToStr(result.objs);
                    var errStr = ToStr(result.errors);

                    ret = objStr
                        + ((objStr.Any() && errStr.Any()) ? "\n" : "")
                        + errStr;
                }

                return ret;
            }
        }

        #endregion


        private RunButtonMode _runButtonMode = RunButtonMode.Run;
        private ObservableCollection<Parameter> _parameters = [];
        
        
        #region Binding Properties

        public string ScriptName => Script.Name;
        
        public int RunButtonIndex
        {
            get => (int)_runButtonMode;
            set => SetProperty(ref _runButtonMode, (RunButtonMode)value);
        }

        public ICommand RunCommand { get; }

        public ObservableCollection<Parameter> Parameters
        {
            get => _parameters;
            private set => SetProperty(ref _parameters, value);
        }

        string resultText_ = "";
        public string ResultText
        {
            get => resultText_;
            protected set
            {
                resultText_ = value;
                // PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ResultText)));
            }
        }

        public int MaxTaskNum
        {
            get
            {
                var ret = GetOwnParam(OwnParameter.MaxTaskNum);
                return (ret != null) ? Convert.ToInt32(ret) : 100;
            }
            set
            {
                if (MaxTaskNum != value)
                {
                    SetOwnParam(OwnParameter.MaxTaskNum, value);
                    // PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxTaskNum)));
                }
            }
        }

        #endregion

        
        private IpListViewModel _ipListViewModel = new IpListViewModel();

        private Script Script { get; set; } = Script.Empty;
        
        

        string ToOwnParamName(string name) => Script.Name + "." + name;

        // parameter by script
        object GetOwnParam(string paramName) => ParameterManager.Instance.Get(ToOwnParamName(paramName));

        void SetOwnParam(string paramName, object value) => ParameterManager.Instance.Set(ToOwnParamName(paramName), value);

        CancellationTokenSource cancelTokenSource;
        List<OutputData> outputDatas = new List<OutputData>();


        public ScriptRunViewModel()
        {
            RunCommand = new DelegateCommand(RunCommandExecute);
        }
        
        public void SetIpListViewModel(IpListViewModel ipListViewModel)
        {
            _ipListViewModel = ipListViewModel;
            _ipListViewModel.DataGridViewModel.PropertyChanged += (_, args) =>
            {
                if ( args.PropertyName == nameof(IpListDataGridViewModel.IsAllItemSelected) )
                {
                    UpdateRunButtonIndex();
                }
            };

            UpdateRunButtonIndex();
            
            return;

            void UpdateRunButtonIndex()
            {
                var hasAnySelectedIp = _ipListViewModel.DataGridViewModel.IsAllItemSelected ?? true;
                RunButtonIndex = (int)(hasAnySelectedIp
                        ? RunButtonMode.Run
                        : RunButtonMode.SelectIp
                    );

            }
        }
        
        public void SetScript(Script script)
        {
            Script = script;
            Script.Load();
            Parameters = new ObservableCollection<Parameter>(Script.paramNames.Select(p => new Parameter(p)));
            
            OnPropertyChanged(nameof(ScriptName));
        }

        private void RunCommandExecute(object? _)
        {
            
        }

        void ClearOutput()
        {
            outputDatas.Clear();
            ResultText = "";
        }

        public Task Run(IReadOnlyCollection<IPParams> ipParamsList)
        {
            var parameters = Parameters.Concat(new[]
            {
                ParameterManager.UserName,
                ParameterManager.UserPassword,
            })
            .GroupBy(p => p.Name)
            .Select(g => g.First())
            .ToList();

            // Check so many IP on ipParamsList
            var soManyIp = ipParamsList.Select(ipParams => IPAddressRange.TryParse(ipParams.ipStr, out var range)
                ? new { ipParams.ipStr, count = range.AsEnumerable().Count() }
                : null
            )
            .Where(pair => pair != null)
            .FirstOrDefault(pair => pair.count > 100);

            if (soManyIp != null)
            {
                var result = MessageBox.Show($"IP[{soManyIp.ipStr}] means {soManyIp.count} targets.\n Continue?", "Many targets", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (result != MessageBoxResult.OK) return null;
            }


            var ipAndParams = ipParamsList.SelectMany(ipParams =>
            {
                IPAddressRange.TryParse(ipParams.ipStr, out var range);
                var ipStrList = range?.AsEnumerable().Select(ip => ip.ToString()) ?? (new[] { ipParams.ipStr });
                var paramDicOrig = parameters.ToDictionary(p => p.Name, p => (object)p.Get(ipParams));
                return ipStrList.Select(ip => new { ip, paramDic = new Dictionary<string, object>(paramDicOrig) });
            })
            .ToList();


            ClearOutput();
            Script.Load();

            cancelTokenSource = new CancellationTokenSource();
            var cancelToken = cancelTokenSource.Token;

            var count = Math.Min(ipAndParams.Count(), MaxTaskNum);
            var rsp = RunspaceFactory.CreateRunspacePool(1, count);
            rsp.Open();

            var tasks = ipAndParams.Select(ipAndParam =>
            {
                var ip = ipAndParam.ip;
                var data = new OutputData()
                {
                    name = ip
                };

                outputDatas.Add(data);


                return Task.Run(() =>
                {
                    var param = new PowerShellScript.InvokeParameter()
                    {
                        rsp = rsp,
                        parameters = ipAndParam.paramDic,
                        cancelToken = cancelToken,
                        invocationStateChanged = (_, e) =>
                        {
                            data.info = e.InvocationStateInfo;
                            UpdateOutput();
                        }
                    };

                    var result = Script.Run(ipAndParam.ip, param);
                    data.result = result;
                    UpdateOutput();
                });
            });

            return Task.WhenAll(tasks)
                .ContinueWith(_ => rsp.Dispose());
        }

        internal void Stop()
        {
            cancelTokenSource?.Cancel();
        }

        void UpdateOutput()
        {
            var str = string.Join("\n", outputDatas.Select(data => data.ToString()));

            lock (this)
            {
                ResultText = str;
            }
        }
    }
}
