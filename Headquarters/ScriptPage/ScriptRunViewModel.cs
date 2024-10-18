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

        private enum RunButtonMode
        {
            SelectIp = 0,
            Run = 1,
            Stop = 2,
        };


        public static class ReservedParameterName
        {
            public const string MaxTaskCount = "MaxTaskCount";
        }

        #endregion


        private readonly IpListViewModel _ipListViewModel;
        private readonly Script _script;
        private readonly ParameterSet _scriptParameterSet;
        
        private CancellationTokenSource? _cancelTokenSource;
        private readonly List<ScriptResult> _ipAddressProcessResults = [];
        
        
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

        public int MaxTaskCount
        {
            get => int.TryParse(_scriptParameterSet.Get(ReservedParameterName.MaxTaskCount), out var num) ? num : 100;
            set
            {
                var num = value.ToString();
                if( _scriptParameterSet.Set(ReservedParameterName.MaxTaskCount, num) )
                {
                    OnPropertyChanged();
                }
            }
        }

        #endregion


        public ScriptRunViewModel() : this(Script.Empty, new IpListViewModel(), new ParameterSet(new Dictionary<string, string>()))
        {
        }

        public ScriptRunViewModel(Script script, IpListViewModel ipListViewModel, ParameterSet scriptParameterSet)
        {
            RunCommand = new DelegateCommand(RunCommandExecute);
            StopCommand = new DelegateCommand(_ => Stop());
            
            _script = script;
            _scriptParameterSet = scriptParameterSet;
            _ipListViewModel = ipListViewModel;
            
            ResetScript(scriptParameterSet);
            InitializeIpListViewModel();
        }

        public void ResetScript(ParameterSet scriptParameterSet)
        {
            _script.Load();
            Parameters.Clear();
            foreach(var parameterName in _script.ParameterNames)
            {
                Parameters.Add(new ScriptParameterViewModel(parameterName, _ipListViewModel, scriptParameterSet));
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
            ResultText = "";
        }

        private async void Run(IReadOnlyList<IpParameterSet> ipParamsList)
        {
            var prepareSuccess = PrepareRun(ipParamsList);
            if ( !prepareSuccess)
            {
                return;
            }
       

            using var cancelTokenSource = new CancellationTokenSource();
            _cancelTokenSource = cancelTokenSource;
            
            var cancelToken = _cancelTokenSource.Token;

            var resultTextFixed = "";
            
            // --------------------------------------------------------------------------------
            // PreProcess
            // --------------------------------------------------------------------------------
            if (_script.HasPreProcess)
            {
                await RunScriptFunction(_script.PreProcess, resultTextFixed, cancelToken);
                resultTextFixed = ResultText + "\n\n";
            }

            // --------------------------------------------------------------------------------
            // IpAddressProcess parallel
            // --------------------------------------------------------------------------------
            await RunIpAddressProcesses(ipParamsList, resultTextFixed, cancelToken);
            resultTextFixed = ResultText + "\n\n";
            
            // --------------------------------------------------------------------------------
            // PostProcess
            // --------------------------------------------------------------------------------
            if (_script.HasPostProcess)
            {
                await RunScriptFunction(_script.PostProcess, resultTextFixed, cancelToken);
            }
            
            _cancelTokenSource = null;
        }

        private bool PrepareRun(IReadOnlyList<IpParameterSet> ipParamsList)
        {
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
                    return false;
                }
            }

            ClearOutput();
            _script.Load();

            return true;
        }

        private async Task RunScriptFunction(ScriptFunction scriptFunction, string resultTextFixed, CancellationToken cancellationToken)
        {
            var scriptResult = new ScriptResult()
            {
                name = scriptFunction.Name
            };
            scriptResult.onPropertyChanged += UpdateResultText;
                
            var invokeParameter = new PowerShellRunner.InvokeParameter()
            {
                parameters = Parameters.ToDictionary(p => p.Name, object (p) => p.Value),
                cancellationToken = cancellationToken,
                invocationStateChanged = (_, args) => scriptResult.Info = args.InvocationStateInfo
            };
                
            scriptResult.Result = await scriptFunction.Run(invokeParameter);
            return;

            void UpdateResultText()
            {
                ResultText = resultTextFixed + scriptResult;
            }
        }

        private async Task RunIpAddressProcesses(IReadOnlyList<IpParameterSet> ipParamsList, string resultTextFixed, CancellationToken cancelToken)
        {
            _ipAddressProcessResults.Clear();
            
            var ipProcessParameterList = ipParamsList.SelectMany(ipParams =>
                {
                    var paramDictionary =
                        Parameters.ToDictionary(p => p.Name, object (p) => ipParams.Get(p.Name) ?? p.Value);

                    var ipStringList = IPAddressRange.TryParse(ipParams.IpString, out var range)
                        ? range.AsEnumerable().Select(ip => ip.ToString())
                        : [ipParams.IpString];


                    return ipStringList.Select(ipString =>
                        {
                            var scriptResult = new ScriptResult
                            {
                                name = ipString,
                            };
                            scriptResult.onPropertyChanged += UpdateResults;
                            
                            return new
                            {
                                ipString,
                                paramDictionary = (IReadOnlyDictionary<string, object>)paramDictionary,
                                scriptResult
                            };
                        }
                    );
                })
                .ToList();

            //　別スレッドでAddするとまずいのであらかじめAddしておく
            _ipAddressProcessResults.AddRange(ipProcessParameterList.Select(paramSet => paramSet.scriptResult));
            
            
            var semaphore = new SemaphoreSlim(MaxTaskCount);

            await Task.WhenAll(
                ipProcessParameterList.Select(async paramSet =>
                {
                    await semaphore.WaitAsync(cancelToken);
                    await RunProcess(paramSet.ipString, paramSet.paramDictionary, paramSet.scriptResult, cancelToken);
                    semaphore.Release();
                })
            );
            return;

            void UpdateResults()
            {
                var str = string.Join("\n", _ipAddressProcessResults.Select(data => data.ToString()));
                ResultText = resultTextFixed + str;
            }
        }
        
        private async Task RunProcess(string ip, IReadOnlyDictionary<string, object> parameters,
             ScriptResult scriptResult, CancellationToken cancelToken)
        {
            var param = new PowerShellRunner.InvokeParameter
            {
                parameters = parameters,
                cancellationToken = cancelToken,
                invocationStateChanged = (_, e) =>scriptResult.Info = e.InvocationStateInfo
            };

            scriptResult.Result = await _script.IpAddressProcess.Run(ip, param);
        }

        private void Stop()
        {
            _cancelTokenSource?.Cancel();
        }
    }
}