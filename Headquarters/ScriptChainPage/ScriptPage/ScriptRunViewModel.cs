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
    private readonly List<Task> _runningTasks = [];
    private CancellationTokenSource? _cancelTokenSource;
    private readonly ConcurrentDictionary<string, object> _sharedDictionary = [];


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
    
    public ICommand StopCommand { get; }

    public ObservableCollection<ScriptParameterInputFieldViewModel> Parameters { get; } = [];
        
    public OutputFieldViewModel OutputFieldViewModel { get; } = new();

    #endregion


    public ScriptRunViewModel() : this(Script.Empty, new IpListViewModel(), new ParameterSet(new Dictionary<string, string>()))
    {
    }

    public ScriptRunViewModel(Script script, IpListViewModel ipListViewModel, ParameterSet scriptParameterSet)
    {
        StopCommand = new DelegateCommand(_ => Stop());
            
        _script = script;
        _script.onUpdate += OnUpdateScript;
        
        _scriptParameterSet = scriptParameterSet;
        _ipListViewModel = ipListViewModel;
            
        OnUpdateScript(false);
    }
    
    public void Dispose()
    {
        _script.onUpdate -= OnUpdateScript;
    }

    private void OnUpdateScript() => OnUpdateScript(true);

    private void OnUpdateScript(bool outputInformation)
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
        
        ScriptName = _script.Name;
        Description = _script.Description;

        if (outputInformation)
        {
            AddOutputInformation("スクリプトファイルを読み込みました");
        }

        if (_script.HasParseError)
        {
            AddOutput(OutputIcon.Failure, "Script Parse Error", $"{string.Join("\n\n", _script.ParseErrorMessages)}");
        }
    }

    public async Task<PowerShellRunner.Result> Run(int maxTaskCount, bool isStopOnError)
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
                return PowerShellRunner.Result.Canceled;
            }
        }
            
            
        AddOutputInformation($"スクリプトを開始します");;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        using var cancelTokenSource = new CancellationTokenSource();
        _cancelTokenSource = cancelTokenSource;

        try
        {
            var result = await RunScriptFunctions(ipAndParameterList, _cancelTokenSource.Token, maxTaskCount, isStopOnError);

            var message = $"実行時間 {stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            if(result.IsSucceed)
            {
                AddOutputInformationSuccessColor($"スクリプトが完了しました - {message}");
            }
            else
            {
                var header = result.canceled ? "スクリプトがキャンセルされました" : "スクリプトがエラーで停止しました";
                AddOutputInformationFailureColor($"{header} - {message}");
            }
            
            
            return result;
        }
        finally
        {
            _cancelTokenSource = null;
        }
    }

    private async Task<PowerShellRunner.Result> RunScriptFunctions(IpAndParameterList ipAndParameterList, CancellationToken cancellationToken, int maxTaskCount, bool isStopOnError)
    {
        var result = new PowerShellRunner.Result();
        // --------------------------------------------------------------------------------
        // BeginTask
        // --------------------------------------------------------------------------------
        if (_script.HasBeginTask)
        {
            result = await RunScriptFunction(_script.BeginTask, cancellationToken);
            if (ShouldReturn(cancellationToken, result, isStopOnError))
            {
                return result;
            }
        }

        // --------------------------------------------------------------------------------
        // IpAddressTask parallel
        // --------------------------------------------------------------------------------
        if (_script.HasIpAddressTask)
        {
            result = await RunIpAddressTasks(ipAndParameterList, cancellationToken, maxTaskCount, isStopOnError);
            if (ShouldReturn(cancellationToken, result, isStopOnError))
            {
                return result;
            }
        }

        // --------------------------------------------------------------------------------
        // EndTask
        // --------------------------------------------------------------------------------
        if (_script.HasEndTask)
        {
            result = await RunScriptFunction(_script.EndTask, cancellationToken);
        }

        return result;
        
        
        static bool ShouldReturn(CancellationToken cancellationToken, PowerShellRunner.Result result, bool isStopOnError)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                result.canceled = true;
                return true;
            }

            return isStopOnError && result.hasError;
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

    private async Task<PowerShellRunner.Result> RunIpAddressTasks(IpAndParameterList ipAndParameterList, CancellationToken cancellationToken, int maxTaskCount, bool isStopOnError)
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
        using var semaphore = new SemaphoreSlim(maxTaskCount);
        
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
                        paramSet.ScriptExecutionInfo,
                        cancellationToken,
                        _sharedDictionary,
                        isStopOnError
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
                paramSet.ScriptExecutionInfo.SetCancelledIfNoResult();
            }
        }
        finally
        {
            _runningTasks.Clear();
        }

        var result = new PowerShellRunner.Result
        {
            canceled = ipAddressTaskParameterList.Select(paramSet => paramSet.ScriptExecutionInfo.Result).Any(result => result == null || result.canceled),
            hasError = ipAddressTaskParameterList.Select(paramSet => paramSet.ScriptExecutionInfo.Result).Any(result => result?.hasError == true)
        };

        return result;
    }
        
    private async Task RunProcess(
        string ip, 
        Dictionary<string, object> parameters,
        ScriptExecutionInfo scriptExecutionInfo, 
        CancellationToken cancelToken, 
        ConcurrentDictionary<string, object> sharedDictionary,
        bool isStopOnError)
    {
        var param = new PowerShellRunner.InvokeParameter(
            parameters: parameters,
            cancellationToken: cancelToken,
            eventSubscriber: scriptExecutionInfo.EventSubscriber
        );

        scriptExecutionInfo.Result = await _script.IpAddressTask.Run(ip, param, sharedDictionary);
        if (isStopOnError)
        {
            StopIfResultHasError(scriptExecutionInfo.Result);
        }
    }

    private void StopIfResultHasError(PowerShellRunner.Result? result)
    {
        if (result is { hasError: true })
        {
            Stop();
        }
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