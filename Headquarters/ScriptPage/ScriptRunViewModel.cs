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
        
        private RunButtonMode _runButtonMode = RunButtonMode.Run;
        private ObservableCollection<ScriptParameterViewModel> _parameterViewModels = [];


        #region Binding Properties

        public string ScriptName => _script.Name;
        
        public string Description => _script.Description;

        public int RunButtonIndex
        {
            get => (int)_runButtonMode;
            set => SetProperty(ref _runButtonMode, (RunButtonMode)value);
        }
        
        public ICommand RunCommand { get; }
        public ICommand StopCommand { get; }

        public ObservableCollection<ScriptParameterViewModel> Parameters
        {
            get => _parameterViewModels;
            private set => SetProperty(ref _parameterViewModels, value);
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
        
        public OutputFieldViewModel OutputFieldViewModel { get; } = new();

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
            foreach (var parameterName in _script.EditableParameterNames)
            {
                Parameters.Add(new ScriptParameterViewModel(
                    parameterName,
                    _script.GetParameterHelp(parameterName),
                    _ipListViewModel,
                    scriptParameterSet)
                );
            }

            OnPropertyChanged(nameof(ScriptName));
            
            OutputFieldViewModel.Clear();

            if (_script.HasError)
            {
                OutputFieldViewModel.AddOutputUnit(new TextOutput(OutputIcon.Failed, "Script Parse Error", $"{string.Join("\n\n", _script.ParseErrors.Select(e => e.ToString()))}"));
                OutputFieldViewModel.UpdateOutput();
            }
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

        private async void Run(IReadOnlyList<IpParameterSet> ipParamsList)
        {
            var ipAndParameterList = ipParamsList.SelectMany(ipParams =>
                {
                    var ipParamsTable = Parameters
                        .ToDictionary(
                            p => p.Name,
                            object (p) => ipParams.Get(p.Name) ?? p.Value
                        );
                        

                    var ipStringList = IPAddressRange.TryParse(ipParams.IpString, out var range)
                        ? range.AsEnumerable().Select(ip => ip.ToString())
                        : [ipParams.IpString];


                    return ipStringList.Select(ipString =>
                    {
                        var parameters = new Dictionary<string, object>(ipParamsTable, StringComparer.OrdinalIgnoreCase)
                        {
                            { Script.ReservedParameterName.Ip, ipString }
                        };
                        
                        if ( !parameters.ContainsKey(GlobalParameter.UserNameParameterName))
                        {
                            parameters[GlobalParameter.UserNameParameterName] = GlobalParameter.UserName;
                        }
                        if ( !parameters.ContainsKey(GlobalParameter.UserPasswordParameterName))
                        {
                            parameters[GlobalParameter.UserPasswordParameterName] = GlobalParameter.UserPassword;
                        }
                        
                        return (ipString, parameters);
                    });
                })
                .ToList();

            if (ipAndParameterList.Count >= GlobalParameter.ConfirmationProcessCount)
            {
                var ip = string.Join(", ", ipAndParameterList.Select(data => data.ipString));
                
                var result = MessageBox.Show(
                    $"{ipAndParameterList.Count}個のIPアドレスへ実行します\n\n{ip}\n\nよろしいですか？",
                    "確認",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Warning
                );
                
                if (result != MessageBoxResult.OK)
                {
                    return;
                }
            }
            
            
            OutputFieldViewModel.Clear();
            _script.Load();
            

            using var cancelTokenSource = new CancellationTokenSource();
            _cancelTokenSource = cancelTokenSource;
            
            var cancelToken = _cancelTokenSource.Token;
            
            // --------------------------------------------------------------------------------
            // PreProcess
            // --------------------------------------------------------------------------------
            if (_script.HasPreProcess)
            {
                await RunScriptFunction(_script.PreProcess, cancelToken);
            }

            // --------------------------------------------------------------------------------
            // IpAddressProcess parallel
            // --------------------------------------------------------------------------------
            await RunIpAddressProcesses(ipAndParameterList, cancelToken);
            
            // --------------------------------------------------------------------------------
            // PostProcess
            // --------------------------------------------------------------------------------
            if (_script.HasPostProcess)
            {
                await RunScriptFunction(_script.PostProcess, cancelToken);
            }
            
            _cancelTokenSource = null;
        }

        private async Task RunScriptFunction(ScriptFunction scriptFunction, CancellationToken cancellationToken)
        {
            var scriptResult = new ScriptResult(scriptFunction.Name);
            scriptResult.onPropertyChanged += OutputFieldViewModel.UpdateOutput;
            
            OutputFieldViewModel.AddScriptResult(scriptResult);
                
            var invokeParameter = new PowerShellRunner.InvokeParameter(
                parameters: Parameters.ToDictionary(p => p.Name, object (p) => p.Value),
                cancellationToken: cancellationToken,
                invocationStateChanged: (_, args) => scriptResult.Info = args.InvocationStateInfo
            );
                
            scriptResult.Result = await scriptFunction.Run(invokeParameter);
        }

        private async Task RunIpAddressProcesses(List<(string ipString, Dictionary<string, object> parameters)> ipAndParameterList, CancellationToken cancelToken)
        {
            var ipProcessParameterList = ipAndParameterList.Select(ipAndParameter =>
                {
                    var scriptResult = new ScriptResult(ipAndParameter.ipString);
                    scriptResult.onPropertyChanged += OutputFieldViewModel.UpdateOutput;

                    OutputFieldViewModel.AddScriptResult(scriptResult);
                    
                    return new
                    {
                        ipAndParameter,
                        scriptResult
                    };
                }
            ).ToList();
            
            var semaphore = new SemaphoreSlim(MaxTaskCount);

            await Task.WhenAll(
                ipProcessParameterList.Select(async paramSet =>
                {
                    await semaphore.WaitAsync(cancelToken);
                    await RunProcess(
                        paramSet.ipAndParameter.ipString,
                        paramSet.ipAndParameter.parameters, 
                        paramSet.scriptResult, cancelToken);
                    semaphore.Release();
                })
            );
        }
        
        private async Task RunProcess(string ip, Dictionary<string, object> parameters,
             ScriptResult scriptResult, CancellationToken cancelToken)
        {
            var param = new PowerShellRunner.InvokeParameter(
                parameters: parameters,
                cancellationToken: cancelToken,
                invocationStateChanged: (_, e) => scriptResult.Info = e.InvocationStateInfo
            );

            scriptResult.Result = await _script.IpAddressProcess.Run(ip, param);
        }

        private void Stop()
        {
            _cancelTokenSource?.Cancel();
        }
    }
}