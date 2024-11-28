using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;

namespace Headquarters;

public class ScriptParameter(ParameterAst parameterAst)
{
    public string Name { get; } = parameterAst.Name.ToString().TrimStart('$');
    public Type StaticType { get; } = parameterAst.StaticType;
    public bool HasDefaultValue { get; } = parameterAst.DefaultValue != null;
    public object? DefaultValue { get; } = parameterAst.DefaultValue?.SafeGetValue();
    
    public IEnumerable<Type> Attributes => parameterAst.Attributes.Select(ast => ast.TypeName.GetReflectionType());
}