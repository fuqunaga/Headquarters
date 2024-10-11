using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        #endregion


        private IpListViewModel _ipListViewModel = new();
        private CancellationTokenSource? _cancelTokenSource;
        private readonly List<ScriptResult> _scriptResults = [];
        
        
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
        public ICommand StopCommand { get; }

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



        private Script Script { get; set; } = Script.Empty;


        string ToOwnParamName(string name) => Script.Name + "." + name;

        // parameter by script
        object GetOwnParam(string paramName) => ParameterManager.Instance.Get(ToOwnParamName(paramName));

        void SetOwnParam(string paramName, object value) =>
            ParameterManager.Instance.Set(ToOwnParamName(paramName), value);



        public ScriptRunViewModel()
        {
            RunCommand = new DelegateCommand(RunCommandExecute);
            StopCommand = new DelegateCommand(_ => Stop());
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
            Parameters = new ObservableCollection<Parameter>(Script.ParameterNames.Select(p => new Parameter(p, _ipListViewModel)));

            OnPropertyChanged(nameof(ScriptName));
        }

        private void RunCommandExecute(object? _)
        {
            Run(_ipListViewModel.DataGridViewModel.SelectedParams.ToList());
        }

        private void ClearOutput()
        {
            _scriptResults.Clear();
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
                            outputData = new ScriptResult { name = ipString }
                        }
                    );
                })
                .ToList();

            //　別スレッドでAddするとまずいのであらかじめAddしておく
            _scriptResults.AddRange(taskParameterSet.Select(paramSet => paramSet.outputData));
            
            
            using var cancelTokenSource = new CancellationTokenSource();
            _cancelTokenSource = cancelTokenSource;
            
            var cancelToken = _cancelTokenSource.Token;
            var semaphore = new SemaphoreSlim(MaxTaskNum);

            await Task.WhenAll(
                taskParameterSet.Select(async paramSet =>
                {
                    await semaphore.WaitAsync(cancelToken);
                    await RunTask(paramSet.ip, paramSet.paramDictionary, paramSet.outputData, cancelToken);
                    semaphore.Release();
                })
            );
            
            _cancelTokenSource = null;
        }

        private async Task RunTask(string ip, IReadOnlyDictionary<string, object> parameters,
             ScriptResult scriptResult, CancellationToken cancelToken)
        {
            var param = new PowerShellRunner.InvokeParameter
            {
                parameters = parameters,
                cancelToken = cancelToken,
                invocationStateChanged = (_, e) =>
                {
                    lock (_scriptResults)
                    {
                        scriptResult.info = e.InvocationStateInfo;
                        UpdateResults();
                    }
                }
            };

            var result = await Script.Run(ip, param);
            lock (_scriptResults)
            {
                scriptResult.result = result;
                UpdateResults();
            }
        }

        private void Stop()
        {
            _cancelTokenSource?.Cancel();
        }

        private void UpdateResults()
        {
            var str = string.Join("\n", _scriptResults.Select(data => data.ToString()));
            ResultText = str;
        }
    }
}