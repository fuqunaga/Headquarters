using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Threading.Tasks;

namespace Headquarters;

public class ScriptFunction
{
    private readonly string _scriptString;
    private readonly string? _commandString;
    private readonly CommentHelpInfo? _helpInfo;
    
    public string Name { get; }
    public IReadOnlyList<string> ParameterNames { get; }
    
    private bool IsSessionRequired => ParameterNames.Contains(Script.ReservedParameterName.Session, StringComparer.OrdinalIgnoreCase);
    private bool IsIpRequired => ParameterNames.Contains(Script.ReservedParameterName.Ip, StringComparer.OrdinalIgnoreCase);
    private bool IsCredentialRequired => ParameterNames.Contains(Script.ReservedParameterName.Credential, StringComparer.OrdinalIgnoreCase);
    
    
    public ScriptFunction(string scriptName, ScriptBlockAst scriptBlockAst)
    {
        _scriptString = scriptBlockAst.Extent.Text;
        _commandString = null;
        
        Name = scriptName;
        ParameterNames = GetScriptBlockParameterNames(scriptBlockAst).ToList();

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
        ParameterNames = functionDefinitionAst.Parameters != null
                ? GetParameterNames(functionDefinitionAst.Parameters).ToList()
                : GetScriptBlockParameterNames(functionDefinitionAst.Body).ToList()
        ;
        
        _helpInfo = functionDefinitionAst.GetHelpContent();
    }
    
    private static IEnumerable<string> GetScriptBlockParameterNames(ScriptBlockAst ast)
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
    
    private static IEnumerable<string> GetParameterNames(IEnumerable<ParameterAst> parameterAstEnumerable)
    {
        return parameterAstEnumerable.Select(p => p.Name.ToString().TrimStart('$'));
    }

    public async Task<PowerShellRunner.Result> Run(string ipAddress, PowerShellRunner.InvokeParameter param)
    {
        var needCredential = IsCredentialRequired || IsSessionRequired;
        if (needCredential)
        {
            param.Parameters[Script.ReservedParameterName.Credential] = SessionManager.CreateCredential(param.Parameters);
        }
        
        if (IsSessionRequired)
        {
            var sessionResult = await SessionManager.CreateSession(ipAddress, param);
            var session = sessionResult.objs?.FirstOrDefault()?.BaseObject;
            if (session == null)
            {
                return sessionResult;
            }
                
            param.Parameters[Script.ReservedParameterName.Session] = session;
        }
        
        return await Run(param);
    }
    
    public async Task<PowerShellRunner.Result> Run(PowerShellRunner.InvokeParameter param)
    {
        var functionParameters = param.Parameters
            .Where(p => ParameterNames.Contains(p.Key, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(p => p.Key, p => p.Value);
        
        var functionInvokeParameter = new PowerShellRunner.InvokeParameter(
            parameters: functionParameters,
            cancellationToken: param.CancellationToken,
            runspacePool: param.RunspacePool,
            invocationStateChanged: param.InvocationStateChanged
        );
        
        return await PowerShellRunner.InvokeAsync(_scriptString, _commandString, functionInvokeParameter);
    }
}