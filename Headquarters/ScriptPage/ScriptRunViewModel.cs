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
            public PowerShellRunner.Result result;

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
        private string _resultText = "";


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
        
        public string ResultText
        {
            get => _resultText;
            private set => SetProperty(ref _resultText, value);
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


        private IpListViewModel _ipListViewModel = new();

        private Script Script { get; set; } = Script.Empty;


        string ToOwnParamName(string name) => Script.Name + "." + name;

        // parameter by script
        object GetOwnParam(string paramName) => ParameterManager.Instance.Get(ToOwnParamName(paramName));

        void SetOwnParam(string paramName, object value) =>
            ParameterManager.Instance.Set(ToOwnParamName(paramName), value);

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
                if (args.PropertyName == nameof(IpListDataGridViewModel.IsAllItemSelected))
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
            Parameters = new ObservableCollection<Parameter>(Script.ParamNames.Select(p => new Parameter(p)));

            OnPropertyChanged(nameof(ScriptName));
        }

        private void RunCommandExecute(object? _)
        {
            Run(_ipListViewModel.DataGridViewModel.SelectedParams.ToList());
        }

        private void ClearOutput()
        {
            outputDatas.Clear();
            ResultText = "";
        }

        private async void Run(IReadOnlyList<IPParams> ipParamsList)
        {
            var parameters = Parameters.Concat(new[]
                {
                    ParameterManager.UserName,
                    ParameterManager.UserPassword,
                })
                .DistinctBy(p => p.Name);

            // Check so many IP on ipParamsList
            var soManyIp = ipParamsList.Select(ipParams => IPAddressRange.TryParse(ipParams.ipStr, out var range)
                    ? new { ipParams.ipStr, count = range.AsEnumerable().Count() }
                    : null
                )
                .FirstOrDefault(pair => pair != null && pair.count > 100);

            if (soManyIp != null)
            {
                var result = MessageBox.Show($"IP[{soManyIp.ipStr}] means {soManyIp.count} targets.\n Continue?",
                    "Many targets", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (result != MessageBoxResult.OK)
                {
                    return;
                }
            }

            ClearOutput();
            Script.Load();

            var taskParameterSet = ipParamsList.SelectMany(ipParams =>
                {
                    var paramDictionary = parameters.ToDictionary(p => p.Name, object (p) => ipParams.Get(p.Name));

                    var ipStringList = IPAddressRange.TryParse(ipParams.ipStr, out var range)
                        ? range.AsEnumerable().Select(ip => ip.ToString())
                        : [ipParams.ipStr];


                    return ipStringList.Select(ipString =>
                        new
                        {
                            ip = ipString,
                            paramDictionary = (IReadOnlyDictionary<string, object>)paramDictionary,
                            outputData = new OutputData { name = ipString }
                        }
                    );
                })
                .ToList();

            //　別スレッドでAddするとまずいのであらかじめAddしておく
            outputDatas.AddRange(taskParameterSet.Select(paramSet => paramSet.outputData));


            cancelTokenSource = new CancellationTokenSource();
            var cancelToken = cancelTokenSource.Token;

            var semaphore = new SemaphoreSlim(MaxTaskNum);

            await Task.WhenAll(
                taskParameterSet.Select(async paramSet =>
                {
                    await semaphore.WaitAsync(cancelToken);
                    await RunTask(paramSet.ip, paramSet.paramDictionary, paramSet.outputData, cancelToken);
                    semaphore.Release();
                })
            );
        }

        private async Task RunTask(string ip, IReadOnlyDictionary<string, object> parameters,
             OutputData outputData, CancellationToken cancelToken)
        {
            var param = new PowerShellRunner.InvokeParameter
            {
                parameters = parameters,
                cancelToken = cancelToken,
                invocationStateChanged = (_, e) =>
                {
                    lock (outputDatas)
                    {
                        outputData.info = e.InvocationStateInfo;
                        UpdateOutput();
                    }
                }
            };

            var result = await Script.Run(ip, param);
            lock (outputDatas)
            {
                outputData.result = result;
                UpdateOutput();
            }
        }

        internal void Stop()
        {
            cancelTokenSource?.Cancel();
        }

        private void UpdateOutput()
        {
            var str = string.Join("\n", outputDatas.Select(data => data.ToString()));
            ResultText = str;
        }
    }
}