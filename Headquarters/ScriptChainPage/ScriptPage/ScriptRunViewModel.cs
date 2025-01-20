using System;
using System.Collections.Concurrent;
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
    private ConcurrentDictionary<string, object> _sharedDictionary = [];


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

    public ObservableCollection<ScriptParameterInputFieldViewModel> Parameters { get; } = [];
        
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
        
        
        foreach (var scriptParameter in _script.EditableScriptParameters)
        {
            Parameters.Add(new ScriptParameterInputFieldViewModel(
                scriptParameter,
                _script.GetParameterHelp(scriptParameter.Name),
                _ipListViewModel,
                _scriptParameterSet)
            );
        }
        
        // IPListにスクリプトのパラメータ名を通知
        _ipListViewModel.DataGridViewModel.SetScriptParameterNames(Parameters.Select(p => p.Name));

        ScriptName = _script.Name;
        Description = _script.Description;

        OutputFieldViewModel.Clear();
        if (!_script.HasParseError) return;
        
        OutputFieldViewModel.AddOutputUnit(new TextOutput(OutputIcon.Failure, "Script Parse Error", $"{string.Join("\n\n", _script.ParseErrorMessages)}"));
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
                        p => p.GetParameterForScript(ipParams.Get(p.Name))
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
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        using var cancelTokenSource = new CancellationTokenSource();
        _cancelTokenSource = cancelTokenSource;
        
        await RunScriptFunctions(ipAndParameterList, _cancelTokenSource.Token);
            
        _cancelTokenSource = null;
        
        // IsRunningをすぐにfalseにするとUIが更新されないことがある
        var elapsed = stopwatch.Elapsed;
        var remaining = TimeSpan.FromMilliseconds(1000) - elapsed;
        if (remaining > TimeSpan.Zero)
        {
            // ReSharper disable once MethodSupportsCancellation
            await Task.Delay(remaining);
        }
        
        IsRunning = false;
    }

    private async Task RunScriptFunctions(IpAndParameterList ipAndParameterList, CancellationToken cancellationToken)
    {
        // --------------------------------------------------------------------------------
        // BeginTask
        // --------------------------------------------------------------------------------
        if (_script.HasBeginTask)
        {
            await RunScriptFunction(_script.BeginTask, cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;
        }

        // --------------------------------------------------------------------------------
        // IpAddressTask parallel
        // --------------------------------------------------------------------------------
        if (_script.HasIpAddressTask)
        {
            await RunIpAddressTasks(ipAndParameterList, cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;
        }

        // --------------------------------------------------------------------------------
        // EndTask
        // --------------------------------------------------------------------------------
        if (_script.HasEndTask)
        {
            await RunScriptFunction(_script.EndTask, cancellationToken);
        }
    }

    private async Task RunScriptFunction(ScriptFunction scriptFunction, CancellationToken cancellationToken)
    {
        var scriptExecInfo = new ScriptExecutionInfo(scriptFunction.Name);
        
            
        OutputFieldViewModel.AddScriptResult(scriptExecInfo);
                
        var invokeParameter = new PowerShellRunner.InvokeParameter(
            parameters: Parameters.ToDictionary(p => p.Name, p => p.GetParameterForScript()),
            cancellationToken: cancellationToken,
            eventSubscriber: scriptExecInfo.EventSubscriber
        );
                
        scriptExecInfo.Result = await scriptFunction.Run(invokeParameter);
        CheckAndStopIfResultHasError(scriptExecInfo.Result);
    }

    private async Task RunIpAddressTasks(IpAndParameterList ipAndParameterList, CancellationToken cancellationToken)
    {
        var ipAddressTaskParameterList = ipAndParameterList.Select(ipAndParameter =>
            {
                var scriptResult = new ScriptExecutionInfo(ipAndParameter.ipString);

                OutputFieldViewModel.AddScriptResult(scriptResult);

                return new
                {
                    ipAndParameter,
                    scriptResult
                };
            }
        ).ToList();
        
        _sharedDictionary.Clear();
        
        // 自前のSemaphoreSlimで各Taskが実行可能になってから実行することでMaxTaskCount通りの同時実行数にする
        // RunspacePool任せの並列実行だとすべてRunning状態になってからRunspacePool待ちになる
        using var semaphore = new SemaphoreSlim(MaxTaskCount);
        
        // _runningTasksが空でない場合は別のタスクが実行中っぽそうでまずい
        if (_runningTasks.Count > 0)
        {
            throw new InvalidOperationException("RunningTasks is not empty");
        }
        
        try
        {
            foreach (var paramSet in ipAddressTaskParameterList)
            {
                 await semaphore.WaitAsync(cancellationToken);
                 var task = RunProcess(
                        paramSet.ipAndParameter.ipString,
                        paramSet.ipAndParameter.parameters,
                        paramSet.scriptResult,
                        cancellationToken,
                        _sharedDictionary
                    ).ContinueWith(_ => semaphore.Release(), cancellationToken);
                
                _runningTasks.Add(task);
                
                // 同時実行だと幅優先的な挙動になりなかなか最初のタスクが終わらない
                // 少し待ってから次のタスクを実行することで深さ優先っぽい挙動になる
                await Task.Delay(1, cancellationToken);
            }
            
            await Task.WhenAll(_runningTasks);
        }
        catch (OperationCanceledException)
        {
            foreach(var paramSet in ipAddressTaskParameterList)
            {
                paramSet.scriptResult.SetCancelledIfNoResult();
            }
        }
        finally
        {
            _runningTasks.Clear();
        }
    }
        
    private async Task RunProcess(string ip, Dictionary<string, object> parameters,
        ScriptExecutionInfo scriptExecutionInfo, CancellationToken cancelToken, ConcurrentDictionary<string, object> sharedDictionary)
    {
        var param = new PowerShellRunner.InvokeParameter(
            parameters: parameters,
            cancellationToken: cancelToken,
            eventSubscriber: scriptExecutionInfo.EventSubscriber
        );

        scriptExecutionInfo.Result = await _script.IpAddressTask.Run(ip, param, sharedDictionary);
        CheckAndStopIfResultHasError(scriptExecutionInfo.Result);
    }

    private void CheckAndStopIfResultHasError(PowerShellRunner.Result? result)
    {
        if (IsStopOnError && (result is { hasError: true }))
        {
            Stop();
        }
    }

    private void Stop()
    {
        if (_cancelTokenSource == null) return;
        
        Task.Run(() =>
        {
            var tmp = _cancelTokenSource;
            _cancelTokenSource = null;
            tmp?.Cancel();
        });
    }
}