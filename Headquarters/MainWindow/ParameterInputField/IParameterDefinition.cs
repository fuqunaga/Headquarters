using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Headquarters;

public interface IParameterDefinition
{
    public string Name { get; }
    public Type? ConstraintType { get; }
    public object? DefaultValue { get; }
    public IEnumerable<string> ValidateSetValues { get; }
    public bool IsPath { get; }
}

public static class ParameterDefinitionExtensions
{
    public static bool IsBool(this IParameterDefinition parameterDefinition)
    {
        return parameterDefinition.ConstraintType == typeof(bool) 
               || parameterDefinition.ConstraintType == typeof(SwitchParameter);
    }
}