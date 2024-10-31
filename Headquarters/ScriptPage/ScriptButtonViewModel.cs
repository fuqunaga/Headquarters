﻿using System;
using System.Linq;
using System.Windows.Input;

namespace Headquarters;

public class ScriptButtonViewModel : ViewModelBase
{
    public Script Script { get; }
    
    public string Name  => Script.Name;
    public string Synopsis => Script.Synopsis;
    public bool HasError => Script.HasError;
    
    public ICommand SelectCommand { get; }


    public ScriptButtonViewModel(Script script, Action<Script>? onSelected)
    {
        Script = script;
        SelectCommand = new DelegateCommand(_ => onSelected?.Invoke(Script));
    }
}