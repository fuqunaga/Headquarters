using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace Headquarters;

public class ScriptParameterDefinition : IParameterDefinition
{
    private readonly ParameterAst _parameterAst;
    
    
    #region IParameterDefinition
    
    public string Name { get; }
    public bool IsBool { get; }
    public object? DefaultValue { get; }
    public IEnumerable<string> ValidateSetValues => Attributes.OfType<AttributeAst>()
        .Where(ast => ast.TypeName.GetReflectionAttributeType() == typeof(ValidateSetAttribute))
        .SelectMany(ast => ast.PositionalArguments)
        .Select(ast => ast.SafeGetValue().ToString());
    
    public bool IsPath => AttributeNames.Contains(CustomAttributeName.WithNamespace(CustomAttributeName.Path));
    
    #endregion


    private IReadOnlyList<AttributeBaseAst> Attributes => _parameterAst.Attributes;
    private IEnumerable<Type> AttributeTypes => Attributes.Select(ast => ast.TypeName.GetReflectionType()).Where(type => type != null);
    private IEnumerable<string> AttributeNames => Attributes.Select(ast => ast.TypeName.Name);
    
    
    public ScriptParameterDefinition(ParameterAst parameterAst)
    {
        _parameterAst = parameterAst;
        
        Name =  _parameterAst.Name.ToString().TrimStart('$');
        IsBool = AttributeTypes.Any(attr => attr == typeof(bool) || attr == typeof(SwitchParameter));
        
        try
        {
            DefaultValue = _parameterAst.DefaultValue?.SafeGetValue();
        }
        catch (Exception)
        {
            // ignored
        }
    }
    
}