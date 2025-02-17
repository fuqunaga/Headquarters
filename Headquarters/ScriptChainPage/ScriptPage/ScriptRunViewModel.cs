using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetTools;
using IpAndParameterList = System.Collections.Generic.List<(string ipString, System.Collections.Generic.Dictionary<string, object> parameters)>;

namespace Headquarters;

public class ScriptRunViewModel : ViewModelBase, IDisposable
{
    private readonly IpListViewModel _ipListViewModel;
    private readonly ParameterSet _scriptParameterSet;
    
    private string _scriptName = "";
    private string _synopsis = "";
    private string _description = "";
    private bool _isLocked;
    private readonly List<Task> _runningTasks = [];
    private CancellationTokenSource? _cancelTokenSource;
    private readonly ConcurrentDictionary<string, object> _sharedDictionary = [];

    public Script Script { get; }

    
    #region Binding Properties

    public string ScriptName
    {
        get => _scriptName;
        private set => SetProperty(ref _scriptName, value);
    }
    
    public bool HasSynopsis => !string.IsNullOrEmpty(Synopsis);
    public bool HasDescription => !string.IsNullOrEmpty(Description);
    
    public string Synopsis
    {
        get => _synopsis;
        private set
        {
            if (SetProperty(ref _synopsis, value))
            {
                OnPropertyChanged(nameof(HasSynopsis));
            }
        }
    }

    public string Description
    {
        get => _description;
        private set
        {
            if (SetProperty(ref _description, value))
            {
                OnPropertyChanged(nameof(HasDescription));
            }
        }
    }

    public bool IsLocked
    {
        get => _isLocked;
        set => SetProperty(ref _isLocked, value);
    }
    
    public ObservableCollection<ScriptParameterInputFieldViewModel> Parameters { get; } = [];
        
    public OutputFieldViewModel OutputFieldViewModel { get; } = new();

    #endregion


    public ScriptRunViewModel() : this(Script.Empty, new IpListViewModel(), new ParameterSet(new Dictionary<string, string>()))
    {
    }

    public ScriptRunViewModel(Script script, IpListViewModel ipListViewModel, ParameterSet scriptParameterSet)
    {
        Script = script;
        Script.onUpdate += OnUpdateScript;
        
        _scriptParameterSet = scriptParameterSet;
        _ipListViewModel = ipListViewModel;
            
        OnUpdateScript(false);
    }
    
    public void Dispose()
    {
        Script.onUpdate -= OnUpdateScript;
    }

    private void OnUpdateScript() => OnUpdateScript(true);

    private void OnUpdateScript(bool outputInformation)
    {
        Parameters.Clear();
        
        foreach (var scriptParameter in Script.EditableScriptParameterDefinitions)
        {
            Parameters.Add(new ScriptParameterInputFieldViewModel(
                    scriptParameter,
                    Script.GetParameterHelp(scriptParameter.Name),
                    _ipListViewModel,
                    _scriptParameterSet).InitializeValueIfEmpty()
            );
        }
        
        ScriptName = Script.Name;
        Synopsis = Script.Synopsis;
        Description = Script.Description;

        if (outputInformation)
        {
            AddOutputInformation("スクリプトファイルを読み込みました");
        }

        if (Script.HasParseError)
        {
            AddOutput(OutputIcon.Failure, "Script Parse Error", $"{string.Join("\n\n", Script.ParseErrorMessages)}");
        }
    }

    public async Task<ScriptRunResult> Run(int maxTaskCount, bool isStopOnError)
    {
        var ipAndParameterList = _ipListViewModel.DataGridViewModel.SelectedParams.SelectMany(ipParams =>
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
            
        AddOutputInformation("スクリプトを開始します");
        var stopwatch = Stopwatch.StartNew();

        using var cancelTokenSource = new CancellationTokenSource();
        _cancelTokenSource = cancelTokenSource;

        try
        {
            var result = await RunScriptFunctions(ipAndParameterList, _cancelTokenSource.Token, maxTaskCount, isStopOnError);

            var message = $"実行時間 {stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            switch (result)
            {
                case ScriptRunResult.None:
                    break;
                case ScriptRunResult.Success:
                    AddOutputInformationSuccessColor($"スクリプトが完了しました - {message}");
                    break;
                case ScriptRunResult.Error:
                    AddOutputInformationFailureColor($"スクリプトがエラーで停止しました - {message}");
                    break;
                case ScriptRunResult.Stop:
                    AddOutputInformationFailureColor($"スクリプトがキャンセルされました - {message}");
                    break;
                case ScriptRunResult.StopDueToTaskError:
                    AddOutputInformationFailureColor($"一部のタスクでエラーが発生したためすべてのタスクをキャンセルしました - {message}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
    
            
            return result;
        }
        finally
        {
            _cancelTokenSource = null;
        }
    }

    private async Task<ScriptRunResult> RunScriptFunctions(IpAndParameterList ipAndParameterList, CancellationToken cancellationToken, int maxTaskCount, bool isStopOnError)
    {
        // --------------------------------------------------------------------------------
        // BeginTask
        // --------------------------------------------------------------------------------
        if (Script.HasBeginTask)
        {
            var result = await RunScriptFunction(Script.BeginTask, cancellationToken);
            if(ShouldReturn(cancellationToken, result, isStopOnError, out var runResult))
            {
                return runResult;
            }
        }

        // --------------------------------------------------------------------------------
        // IpAddressTask parallel
        // --------------------------------------------------------------------------------
        if (Script.HasIpAddressTask)
        {
            var runResult = await RunIpAddressTasks(ipAndParameterList, cancellationToken, maxTaskCount, isStopOnError);
            if(runResult != ScriptRunResult.Success)
            {
                return runResult;
            }
        }

        // --------------------------------------------------------------------------------
        // EndTask
        // --------------------------------------------------------------------------------
        if (Script.HasEndTask)
        {
            var result = await RunScriptFunction(Script.EndTask, cancellationToken);
            if( result.canceled)
            {
                return ScriptRunResult.Stop;
            }
            if( result.hasError)
            {
                return ScriptRunResult.Error;
            }
        }

        return ScriptRunResult.Success;
        
        
        static bool ShouldReturn(CancellationToken cancellationToken, PowerShellRunner.Result result, bool isStopOnError, out ScriptRunResult runResult)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                runResult = ScriptRunResult.Stop;
                return true;
            }

            if( isStopOnError && result.hasError)
            {
                runResult = ScriptRunResult.Error;
                return true;
            }
            
            runResult = ScriptRunResult.None;
            return false;
        }
    }

    private async Task<PowerShellRunner.Result> RunScriptFunction(ScriptFunction scriptFunction, CancellationToken cancellationToken)
    {
        var scriptExecInfo = new ScriptExecutionInfo(scriptFunction.Name);
        OutputFieldViewModel.AddScriptResult(scriptExecInfo);
                
        var invokeParameter = new PowerShellRunner.InvokeParameter(
            parameters: Parameters.ToDictionary(p => p.Name, p => p.GetParameterForScript()),
            cancellationToken: cancellationToken,
            eventSubscriber: scriptExecInfo.EventSubscriber
        );
                
        var result = await scriptFunction.Run(invokeParameter);
        scriptExecInfo.Result = result;
        return result;
    }

    private async Task<ScriptRunResult> RunIpAddressTasks(IpAndParameterList ipAndParameterList, CancellationToken cancellationToken, int maxTaskCount, bool isStopOnError)
    {
        var ipAddressTaskParameterList = ipAndParameterList.Select(ipAndParameter =>
            {
                var scriptExecutionInfo = new ScriptExecutionInfo(ipAndParameter.ipString);

                OutputFieldViewModel.AddScriptResult(scriptExecutionInfo);

                return new
                {
                    ipAndParameter, ScriptExecutionInfo = scriptExecutionInfo
                };
            }
        ).ToList();
        
        _sharedDictionary.Clear();
        
        // 自前のSemaphoreSlimで各Taskが実行可能になってから実行することでMaxTaskCount通りの同時実行数にする
        // RunspacePool任せの並列実行だとすべてRunning状態になってからRunspacePool待ちになる
        var semaphore = new SemaphoreSlim(maxTaskCount);
        
        // _runningTasksが空でない場合は別のタスクが実行中っぽそうでまずい
        if (_runningTasks.Count > 0)
        {
            throw new InvalidOperationException("RunningTasks is not empty");
        }
        
        var isStoppedByTaskError = false;
        
        try
        {
            foreach (var paramSet in ipAddressTaskParameterList)
            {
                 await semaphore.WaitAsync(cancellationToken);

                 var task = Task.Run(async () =>
                 {
                     var result = await RunIpAddressTask(
                         paramSet.ipAndParameter.ipString,
                         paramSet.ipAndParameter.parameters,
                         paramSet.ScriptExecutionInfo,
                         cancellationToken,
                         _sharedDictionary
                     );

                     if (isStopOnError
                         && result.hasError
                         && !cancellationToken.IsCancellationRequested
                        )
                     {
                         Stop();
                         isStoppedByTaskError = true;
                     }

                     semaphore.Release();
                 }, cancellationToken);
                
                _runningTasks.Add(task);
                
                // 同時実行だと幅優先的な挙動になりなかなか最初のタスクが終わらない
                // 少し待ってから次のタスクを実行することで深さ優先っぽい挙動になる
                await Task.Delay(10, cancellationToken);
            }
            
            await Task.WhenAll(_runningTasks);
        }
        catch (OperationCanceledException)
        {
            foreach(var paramSet in ipAddressTaskParameterList)
            {
                paramSet.ScriptExecutionInfo.SetCancelledIfNoResult();
            }
        }
        finally
        {
            _runningTasks.Clear();
        }

        
        if (ipAddressTaskParameterList.Select(paramSet => paramSet.ScriptExecutionInfo.Result).All(result => result?.IsSucceed ?? false))
        {
            return ScriptRunResult.Success;
        }

        if (isStoppedByTaskError)
        {
            return ScriptRunResult.StopDueToTaskError;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return ScriptRunResult.Stop;
        }
        
        return ScriptRunResult.Error;
    }
        
    private async Task<PowerShellRunner.Result> RunIpAddressTask(
        string ip, 
        Dictionary<string, object> parameters,
        ScriptExecutionInfo scriptExecutionInfo, 
        CancellationToken cancelToken, 
        ConcurrentDictionary<string, object> sharedDictionary)
    {
        var param = new PowerShellRunner.InvokeParameter(
            parameters: parameters,
            cancellationToken: cancelToken,
            eventSubscriber: scriptExecutionInfo.EventSubscriber
        );

        scriptExecutionInfo.Result = await Script.IpAddressTask.Run(ip, param, sharedDictionary);
        return scriptExecutionInfo.Result;
    }

    public void Stop()
    {
        if (_cancelTokenSource == null) return;
        
        Task.Run(() =>
        {
            var tmp = _cancelTokenSource;
            _cancelTokenSource = null;
            tmp?.Cancel();
        });
    }

    public void AddOutput(OutputIcon icon, string label, string message, string? textColor = null)
    {
        OutputFieldViewModel.AddOutputUnit(new TextOutput(icon, label, message, textColor));
    }

    public void AddOutputInformation(string label, string message = "", string? textColor = null)
        => AddOutput(OutputIcon.Information, $"[{DateTime.Now:HH:mm:ss}] {label}", message, textColor ?? "#BBBBBB");

    public void AddOutputInformationSuccessColor(string label, string message = "")
        => AddOutputInformation(label, message, "#AAEEAA");

    public void AddOutputInformationFailureColor(string label, string message = "")
        => AddOutputInformation(label, message, "#FF8888");

    public void ClearOutput()
    {
        OutputFieldViewModel.Clear();
    }
}