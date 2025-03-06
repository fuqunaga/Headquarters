using System;
using System.Collections;
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
    public Type? ConstraintType => Attributes.OfType<TypeConstraintAst>().Select(ast => ast.TypeName.GetReflectionType()).FirstOrDefault();
    public object? DefaultValue { get; }
    public IEnumerable<string> ValidateSetValues => Attributes.OfType<AttributeAst>()
        .Where(ast => ast.TypeName.GetReflectionAttributeType() == typeof(ValidateSetAttribute))
        .SelectMany(ast => ast.PositionalArguments)
        .Select(ast => ast.SafeGetValue().ToString());
    
    public bool IsPath => AttributeNames.Contains(CustomAttributeName.WithNamespace(CustomAttributeName.Path));
    
    #endregion


    private IReadOnlyList<AttributeBaseAst> Attributes => _parameterAst.Attributes;
    private IEnumerable<string> AttributeNames => Attributes.Select(ast => ast.TypeName.Name);
    
    
    public ScriptParameterDefinition(ParameterAst parameterAst)
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
    
    
    #region Equality
    
    public class NameEqualityComparer : IEqualityComparer<ScriptParameterDefinition>
    {
        public static NameEqualityComparer Default { get; } = new();
    
        public bool Equals(ScriptParameterDefinition x, ScriptParameterDefinition y) => x.Name == y.Name;
        public int GetHashCode(ScriptParameterDefinition obj) => obj.Name.GetHashCode();
    }
    
    #endregion
}
