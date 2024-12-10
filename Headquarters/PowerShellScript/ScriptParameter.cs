using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace Headquarters;

public class ScriptParameter(ParameterAst parameterAst)
{
    public string Name { get; } = parameterAst.Name.ToString().TrimStart('$');
    public Type StaticType { get; } = parameterAst.StaticType;
    public bool HasDefaultValue { get; } = parameterAst.DefaultValue != null;
    public object? DefaultValue { get; } = parameterAst.DefaultValue?.SafeGetValue();
    
    public IReadOnlyList<AttributeBaseAst> Attributes => parameterAst.Attributes;
    public IEnumerable<Type> AttributeTypes => Attributes.Select(ast => ast.TypeName.GetReflectionType());
    public IEnumerable<string> ValidateSetValues => Attributes.OfType<AttributeAst>()
        .Where(ast => ast.TypeName.GetReflectionType() == typeof(ValidateSetAttribute))
        .SelectMany(ast => ast.PositionalArguments)
        .Select(ast => ast.SafeGetValue().ToString());
}