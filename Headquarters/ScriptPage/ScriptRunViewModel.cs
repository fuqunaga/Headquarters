﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NetTools;
using IpAndParameterList = System.Collections.Generic.List<(string ipString, System.Collections.Generic.Dictionary<string, object> parameters)>;

namespace Headquarters;

public class ScriptRunViewModel : ViewModelBase, IDisposable
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
    
    private string _scriptName = "";
    private string _description = "";
    private bool _isLocked;
    private bool _isRunning;
    private bool _isAnyIpSelected;
    private bool _isStopOnError = true;
    private readonly List<Task> _runningTasks = [];
    private CancellationTokenSource? _cancelTokenSource;


    #region Binding Properties

    public string ScriptName
    {
        get => _scriptName;
        private set => SetProperty(ref _scriptName, value);
    }

    public string Description
    {
        get => _description;
        private set => SetProperty(ref _description, value);
    }

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

    public ObservableCollection<ScriptParameterViewModel> Parameters { get; } = [];
        
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
        _script.onUpdate += OnUpdateScript;
        
        _scriptParameterSet = scriptParameterSet;
        _ipListViewModel = ipListViewModel;
            
        OnUpdateScript();
        SubscribeIpListViewModel();
    }
    
    public void Dispose()
    {
        _script.onUpdate -= OnUpdateScript;
    }


    private void OnUpdateScript()
    {
        Parameters.Clear();
        foreach (var parameterName in _script.EditableParameterNames)
        {
            Parameters.Add(new ScriptParameterViewModel(
                parameterName,
                _script.GetParameterHelp(parameterName),
                _ipListViewModel,
                _scriptParameterSet)
            );
        }

        ScriptName = _script.Name;
        Description = _script.Description;

        OutputFieldViewModel.Clear();
        if (!_script.HasParseError) return;
        
        OutputFieldViewModel.AddOutputUnit(new TextOutput(OutputIcon.Failure, "Script Parse Error", $"{string.Join("\n\n", _script.ParseErrorMessages)}"));
        OutputFieldViewModel.UpdateOutput();
    }
        
    private void SubscribeIpListViewModel()
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
        var task = Run(_ipListViewModel.DataGridViewModel.SelectedParams.ToList());
    }

    private async Task Run(IEnumerable<IpParameterSet> ipParamsList)
    {
        if ( IsRunning)
        {
            return;
        }
        
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

        IsRunning = true;

        using var cancelTokenSource = new CancellationTokenSource();
        _cancelTokenSource = cancelTokenSource;
        
        await RunScriptFunctions(ipAndParameterList, _cancelTokenSource.Token);
            
        _cancelTokenSource = null;
        IsRunning = false;
    }

    private async Task RunScriptFunctions(IpAndParameterList ipAndParameterList, CancellationToken cancellationToken)
    {
        using var runspacePool = RunspaceFactory.CreateRunspacePool(1, MaxTaskCount);
        runspacePool.Open();
        
        // --------------------------------------------------------------------------------
        // PreProcess
        // --------------------------------------------------------------------------------
        if (_script.HasPreProcess)
        {
            await RunScriptFunction(_script.PreProcess, cancellationToken, runspacePool);
            if (cancellationToken.IsCancellationRequested) return;
        }

        // --------------------------------------------------------------------------------
        // IpAddressProcess parallel
        // --------------------------------------------------------------------------------
        await RunIpAddressProcesses(ipAndParameterList, cancellationToken, runspacePool);
        if (cancellationToken.IsCancellationRequested) return;

        // --------------------------------------------------------------------------------
        // PostProcess
        // --------------------------------------------------------------------------------
        if (_script.HasPostProcess)
        {
            await RunScriptFunction(_script.PostProcess, cancellationToken, runspacePool);
        }
    }

    private async Task RunScriptFunction(ScriptFunction scriptFunction, CancellationToken cancellationToken, RunspacePool runspacePool)
    {
        var scriptResult = new ScriptResult(scriptFunction.Name);
        scriptResult.onPropertyChanged += OutputFieldViewModel.UpdateOutput;
            
        OutputFieldViewModel.AddScriptResult(scriptResult);
                
        var invokeParameter = new PowerShellRunner.InvokeParameter(
            parameters: Parameters.ToDictionary(p => p.Name, object (p) => p.Value),
            cancellationToken: cancellationToken,
            runspacePool: runspacePool,
            invocationStateChanged: (_, args) => scriptResult.Info = args.InvocationStateInfo
        );
                
        scriptResult.Result = await scriptFunction.Run(invokeParameter);
        CheckAndStopIfResultHasError(scriptResult.Result);
    }

    private async Task RunIpAddressProcesses(IpAndParameterList ipAndParameterList, CancellationToken cancellationToken, RunspacePool runspacePool)
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
        
        // 自前のSemaphoreSlimで各Taskが実行可能になってから実行することでMaxTaskCount通りの同時実行数にする
        // RunspacePool任せの並列実行だとすべてRunning状態になってからRunspacePool待ちになる
        var semaphore = new SemaphoreSlim(MaxTaskCount);
        
        // _runningTasksが空でない場合は別のタスクが実行中っぽそうでまずい
        if (_runningTasks.Count > 0)
        {
            throw new InvalidOperationException("RunningTasks is not empty");
        }
        
        try
        {
            foreach (var paramSet in ipProcessParameterList)
            {
                 await semaphore.WaitAsync(cancellationToken);
                 var task = RunProcess(
                        paramSet.ipAndParameter.ipString,
                        paramSet.ipAndParameter.parameters,
                        paramSet.scriptResult,
                        cancellationToken,
                        runspacePool
                    ).ContinueWith(_ => semaphore.Release(), cancellationToken);
                
                _runningTasks.Add(task);
                
                // 同時実行だと幅優先的な挙動になりなかなか最初のタスクが終わらない
                // 少し待ってから次のタスクを実行することで深さ優先っぽい挙動になる
                await Task.Delay(1, cancellationToken);
            }
            
            await Task.WhenAll(_runningTasks);
        }
        catch (OperationCanceledException _)
        {
            foreach(var paramSet in ipProcessParameterList)
            {
                paramSet.scriptResult.SetCancelledIfNoResult();
            }
        }
        finally
        {
            semaphore.Dispose();
            _runningTasks.Clear();
        }
    }
        
    private async Task RunProcess(string ip, Dictionary<string, object> parameters,
        ScriptResult scriptResult, CancellationToken cancelToken, RunspacePool runspacePool)
    {
        var param = new PowerShellRunner.InvokeParameter(
            parameters: parameters,
            cancellationToken: cancelToken,
            runspacePool: runspacePool,
            invocationStateChanged: (_, e) => scriptResult.Info = e.InvocationStateInfo
        );

        scriptResult.Result = await _script.IpAddressProcess.Run(ip, param);
        CheckAndStopIfResultHasError(scriptResult.Result);
    }

    private void CheckAndStopIfResultHasError(PowerShellRunner.Result? result)
    {
        if (IsStopOnError && (result is { HasError: true }))
        {
            Stop();
        }
    }

    private void Stop()
    {
        _cancelTokenSource?.Cancel();
    }
}