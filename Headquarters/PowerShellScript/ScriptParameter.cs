using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace Headquarters;

public class ScriptParameter
{
    private readonly ParameterAst _parameterAst;
    
    public string Name { get; }
    public object? DefaultValue { get; }
    
    public IReadOnlyList<AttributeBaseAst> Attributes => _parameterAst.Attributes;
    public IEnumerable<Type> AttributeTypes => Attributes.Select(ast => ast.TypeName.GetReflectionType()).Where(type => type != null);
    public IEnumerable<string> AttributeNames => Attributes.Select(ast => ast.TypeName.Name);
    
    public IEnumerable<string> ValidateSetValues => Attributes.OfType<AttributeAst>()
        .Where(ast => ast.TypeName.GetReflectionAttributeType() == typeof(ValidateSetAttribute))
        .SelectMany(ast => ast.PositionalArguments)
        .Select(ast => ast.SafeGetValue().ToString());

    public ScriptParameter(ParameterAst parameterAst)
    {
        _parameterAst = parameterAst;
        Name =  _parameterAst.Name.ToString().TrimStart('$');
        
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