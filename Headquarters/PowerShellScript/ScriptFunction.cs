﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Threading.Tasks;

namespace Headquarters;

public class ScriptFunction
{
    private readonly string _scriptString;
    private readonly string? _commandString;
    private readonly CommentHelpInfo? _helpInfo;
    
    public string Name { get; }
    
    public IReadOnlyList<ScriptParameterDefinition> Parameters { get; }
    
    public IEnumerable<string> ParameterNames => Parameters.Select(p => p.Name); 
    
    private bool IsTaskContextRequired => ParameterNames.Contains(Script.ReservedParameterName.TaskContext, StringComparer.OrdinalIgnoreCase);
    private bool IsSessionRequired => ParameterNames.Contains(Script.ReservedParameterName.Session, StringComparer.OrdinalIgnoreCase);
    private bool IsIpRequired => ParameterNames.Contains(Script.ReservedParameterName.Ip, StringComparer.OrdinalIgnoreCase);
    
    public ScriptFunction(string scriptName, ScriptBlockAst scriptBlockAst)
    {
        _scriptString = scriptBlockAst.Extent.Text;
        _commandString = null;
        
        Name = scriptName;
        Parameters = GetScriptBlockParameterNames(scriptBlockAst).ToList();

        _helpInfo = scriptBlockAst.GetHelpContent();
    }
    
    public ScriptFunction(FunctionDefinitionAst functionDefinitionAst)
    {
        _scriptString = GetRootAst(functionDefinitionAst).Extent.Text;
        _commandString = functionDefinitionAst.Name;
        
        Name = functionDefinitionAst.Name;
        
        // functionのパラメータは次の２形式ある
        // 1. function myFunc($param1, $param2) { ... }
        // 2. function myFunc{ param($param1, $param2); ... }
        Parameters = functionDefinitionAst.Parameters != null
                ? GetParameterNames(functionDefinitionAst.Parameters).ToList()
                : GetScriptBlockParameterNames(functionDefinitionAst.Body).ToList()
        ;
        
        _helpInfo = functionDefinitionAst.GetHelpContent();
    }
    
    private static IEnumerable<ScriptParameterDefinition> GetScriptBlockParameterNames(ScriptBlockAst ast)
    {
        var paramBlock = ast.ParamBlock;
        return paramBlock == null ? [] : GetParameterNames(paramBlock.Parameters);
    }
    
    private static Ast GetRootAst(Ast ast)
    {
        while (ast.Parent != null)
        {
            ast = ast.Parent;
        }

        return ast;
    }
    
    private static IEnumerable<ScriptParameterDefinition> GetParameterNames(IEnumerable<ParameterAst> parameterAstEnumerable)
    {
        return parameterAstEnumerable.Select(p => new ScriptParameterDefinition(p));
    }

    public async Task<PowerShellRunner.Result> Run(string ipAddress, PowerShellRunner.InvokeParameter param, ConcurrentDictionary<string, object> sharedDictionary)
    {
        // TaskContext作成
        // Session作成時に必要なのでIsSessionRequiredでも作成しておく
        TaskContext? taskContext = null;
        if (IsSessionRequired)
        {
            taskContext ??= CreateTaskContextLocal();
            if(param.Runspace == null)
            {
                var runspace = RunspaceFactory.CreateRunspace();
                param.Runspace = runspace;
            }
            
            var sessionResult = await SessionManager.CreateSession(ipAddress, taskContext.Credential, param);
            var session = sessionResult.objs?.FirstOrDefault()?.BaseObject;
            if (session == null)
            {
                if (sessionResult.IsSucceed)
                {
                    throw new Exception("Session is null but result is succeed.");
                }
                return sessionResult;
            }
                
            param.Parameters[Script.ReservedParameterName.Session] = session;
        }
        
        if (IsTaskContextRequired)
        {
            taskContext ??= CreateTaskContextLocal();
            param.Parameters[Script.ReservedParameterName.TaskContext] = taskContext;
        }

        
        return await Run(param);
        
 
        TaskContext CreateTaskContextLocal() => ScriptFunction.CreateTaskContext(ipAddress, param, sharedDictionary);
    }

    public async Task<PowerShellRunner.Result> Run(PowerShellRunner.InvokeParameter param, ConcurrentDictionary<string, object> sharedDictionary)
    {
        if (IsTaskContextRequired)
        {
            var taskContext = CreateTaskContext("", param, sharedDictionary);
            param.Parameters[Script.ReservedParameterName.TaskContext] = taskContext;
        }
        
        return await Run(param);
    }
    
    private async Task<PowerShellRunner.Result> Run(PowerShellRunner.InvokeParameter param)
    {
        var functionParameters = param.Parameters
            .Where(p => ParameterNames.Contains(p.Key, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(p => p.Key, p => p.Value);
        
        var functionInvokeParameter = new PowerShellRunner.InvokeParameter(
            parameters: functionParameters,
            cancellationToken: param.CancellationToken,
            eventSubscriber: param.EventSubscriber
        );
        
        return await PowerShellRunner.InvokeAsync(_scriptString, functionInvokeParameter, _commandString);
    }
    
    
    private static TaskContext CreateTaskContext(string ipAddress, PowerShellRunner.InvokeParameter param, ConcurrentDictionary<string, object> sharedDictionary)
    {
        var userName = GlobalParameter.UserName;
        if (param.Parameters.TryGetValue(GlobalParameter.UserName, out var userNameObj) && userNameObj is string userNameString)
        {
            userName = userNameString;
        }
        
        var userPassword = GlobalParameter.UserPassword;
        if (param.Parameters.TryGetValue(GlobalParameter.UserPassword, out var userPasswordObj) && userPasswordObj is string userPasswordString)
        {
            userPassword = userPasswordString;
        }
        
        return new TaskContext(ipAddress, userName, userPassword, sharedDictionary);
    }
}