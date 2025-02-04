using System;
using System.Collections.Generic;

namespace Headquarters;

public interface IParameterDefinition
{
    public string Name { get; }
    public bool IsBool { get; }
    public object? DefaultValue { get; }
    public IEnumerable<string> ValidateSetValues { get; }
    public bool IsPath { get; }
}