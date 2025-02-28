using System;
using System.Collections.Generic;

namespace Headquarters;

public class ParameterDefinition(string name) : IParameterDefinition
{
    public string Name => name;
    public Type? ConstraintType { get; set;}
    public object? DefaultValue { get; set;}
    public IEnumerable<string> ValidateSetValues { get; set;} = [];
    public bool IsPath { get; set;}
}