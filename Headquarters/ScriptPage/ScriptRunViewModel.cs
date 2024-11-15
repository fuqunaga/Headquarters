using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NetTools;
using IpAndParameterList = System.Collections.Generic.List<(string ipString, System.Collections.Generic.Dictionary<string, object> parameters)>;

namespace Headquarters;

public class ScriptRunViewModel : ViewModelBase
{
    
    
    #region Type Define

    public static class ReservedParameterName
    {
        public const string MaxTaskCount = "MaxTaskCount";
    }

    #endregion


    private readonly IpListViewModel _ipListViewModel;
    private readonly Script _script;
    private readonly ParameterSet _scriptParameterSet;
    
    private bool _isLocked;
    private bool _isRunning;
    private bool _isAnyIpSelected;
    private bool _isStopOnError = true;
    private ObservableCollection<ScriptParameterViewModel> _parameterViewModels = [];
    private CancellationTokenSource? _cancelTokenSource;


    #region Binding Properties

    public string ScriptName => _script.Name;
        
    public string Description => _script.Description;

    public bool IsLocked 
    {
        get => _isLocked;
        set => SetProperty(ref _isLocked, value);
    }
    
    public bool IsRunning
    {
        get => _isRunning;
        private set => SetProperty(ref _isRunning, value);
    }
    
    public bool IsAnyIpSelected
    {
        get => _isAnyIpSelected;
        private set => SetProperty(ref _isAnyIpSelected, value);
    }
    
        
    public ICommand RunCommand { get; }
    public ICommand StopCommand { get; }

    public bool IsStopOnError
    {
        get => _isStopOnError;
        set => SetProperty(ref _isStopOnError, value);
    }

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
            OutputFieldViewModel.AddOutputUnit(new TextOutput(OutputIcon.Failure, "Script Parse Error", $"{string.Join("\n\n", _script.ParseErrors.Select(e => e.ToString()))}"));
            OutputFieldViewModel.UpdateOutput();
        }
    }
        
    private void InitializeIpListViewModel()
    {
        _ipListViewModel.DataGridViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(IpListDataGridViewModel.IsAllItemSelected))
            {
                UpdateIsAnyIpSelected();
            }
        };

        UpdateIsAnyIpSelected();
        return;
        
        void UpdateIsAnyIpSelected()
        {
            IsAnyIpSelected = _ipListViewModel.DataGridViewModel.IsAllItemSelected ?? true;
        }
    }

      

    private void RunCommandExecute(object? _)
    {
        Task.Run(() => Run(_ipListViewModel.DataGridViewModel.SelectedParams.ToList()));
    }

    private async Task Run(IReadOnlyList<IpParameterSet> ipParamsList)
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

        IsRunning = true;

        using var cancelTokenSource = new CancellationTokenSource();
        _cancelTokenSource = cancelTokenSource;
        
        await RunScriptFunctions(ipAndParameterList, _cancelTokenSource.Token);
            
        _cancelTokenSource = null;
        IsRunning = false;
    }

    private async Task RunScriptFunctions(IpAndParameterList ipAndParameterList, CancellationToken cancellationToken)
    {
        // --------------------------------------------------------------------------------
        // PreProcess
        // --------------------------------------------------------------------------------
        if (_script.HasPreProcess)
        {
            await RunScriptFunction(_script.PreProcess, cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;
        }

        // --------------------------------------------------------------------------------
        // IpAddressProcess parallel
        // --------------------------------------------------------------------------------
        await RunIpAddressProcesses(ipAndParameterList, cancellationToken);
        if (cancellationToken.IsCancellationRequested) return;

        // --------------------------------------------------------------------------------
        // PostProcess
        // --------------------------------------------------------------------------------
        if (_script.HasPostProcess)
        {
            await RunScriptFunction(_script.PostProcess, cancellationToken);
        }
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
        CheckAndStopIfResultHasError(scriptResult.Result);
    }

    private async Task RunIpAddressProcesses(IpAndParameterList ipAndParameterList, CancellationToken cancelToken)
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
        CheckAndStopIfResultHasError(scriptResult.Result);
    }

    private void CheckAndStopIfResultHasError(PowerShellRunner.Result? result)
    {
        if (IsStopOnError && (result is { HasError: true }))
        {
            _cancelTokenSource?.Cancel();
        }
    }

    private void Stop()
    {
        _cancelTokenSource?.Cancel();
    }
}