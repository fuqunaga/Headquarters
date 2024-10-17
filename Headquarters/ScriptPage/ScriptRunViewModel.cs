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


        private readonly IpListViewModel _ipListViewModel;
        private readonly Script _script;
        
        private CancellationTokenSource? _cancelTokenSource;
        private readonly List<ScriptResult> _scriptResults = [];
        
        
        private RunButtonMode _runButtonMode = RunButtonMode.Run;
        private ObservableCollection<ScriptParameterViewModel> _parameters = [];
        private string _resultText = "";


        #region Binding Properties

        public string ScriptName => _script.Name;

        public int RunButtonIndex
        {
            get => (int)_runButtonMode;
            set => SetProperty(ref _runButtonMode, (RunButtonMode)value);
        }

        public ICommand RunCommand { get; }
        public ICommand StopCommand { get; }

        public ObservableCollection<ScriptParameterViewModel> Parameters
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



        
        
        string ToOwnParamName(string name) => _script.Name + "." + name;

        // parameter by script
        object GetOwnParam(string paramName) => ParameterManager.Instance.Get(ToOwnParamName(paramName));

        void SetOwnParam(string paramName, object value) =>
            ParameterManager.Instance.Set(ToOwnParamName(paramName), value);


        public ScriptRunViewModel() : this(Script.Empty, new IpListViewModel(), new ScriptParameterSet(new Dictionary<string, string>()))
        {
        }

        public ScriptRunViewModel(Script script, IpListViewModel ipListViewModel, ScriptParameterSet scriptParameterSet)
        {
            RunCommand = new DelegateCommand(RunCommandExecute);
            StopCommand = new DelegateCommand(_ => Stop());
            
            _script = script;
            _ipListViewModel = ipListViewModel;
            
            ResetScript(scriptParameterSet);
            InitializeIpListViewModel();
        }

        public void ResetScript(ScriptParameterSet scriptParameterSet)
        {
            _script.Load();
            Parameters.Clear();
            foreach (var name in _script.ParameterNames)
            {
                Parameters.Add(new ScriptParameterViewModel(name, _ipListViewModel, scriptParameterSet));
            }

            OnPropertyChanged(nameof(ScriptName));
        }
        
        private void InitializeIpListViewModel()
        {
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
        
      

        private void RunCommandExecute(object? _)
        {
            Run(_ipListViewModel.DataGridViewModel.SelectedParams.ToList());
        }

        private void ClearOutput()
        {
            _scriptResults.Clear();
            ResultText = "";
        }

        private async void Run(IReadOnlyList<IpParameterSet> ipParamsList)
        {
            // var parameters = Parameters.Concat(new[]
            //     {
            //         ParameterManager.UserName,
            //         ParameterManager.UserPassword,
            //     })
            //     .DistinctBy(p => p.Name);

            // Check so many IP on ipParamsList
            var soManyIp = ipParamsList.Select(ipParams => IPAddressRange.TryParse(ipParams.IpString, out var range)
                    ? new { ipStr = ipParams.IpString, count = range.AsEnumerable().Count() }
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
            _script.Load();

            var taskParameterSet = ipParamsList.SelectMany(ipParams =>
                {
                    var paramDictionary = Parameters.ToDictionary(p => p.Name, object (p) => ipParams.Get(p.Name) ?? p.Value);

                    var ipStringList = IPAddressRange.TryParse(ipParams.IpString, out var range)
                        ? range.AsEnumerable().Select(ip => ip.ToString())
                        : [ipParams.IpString];


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

            var result = await _script.Run(ip, param);
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