using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Windows.Input;

namespace Headquarters
{
    /// <summary>
    /// パラメータを表すViewModel
    /// </summary>
    public class ParameterInputFieldViewModel : ViewModelBase, IHelpTextBlockViewModel
    {
        protected readonly IParameterDefinition parameterDefinition;
        private readonly ParameterSet _scriptParameterSet;
        
        public string Name => parameterDefinition.Name;
        public virtual string Value
        {
            get => _scriptParameterSet.Get(Name);
            set
            {
                if (_scriptParameterSet.Set(Name, value))
                {
                    OnPropertyChanged();
                }
            }
        }
        
        public bool HasHelp { get; }
        public string HelpFirstLine { get; }
        public string HelpDetail { get;  }
        public virtual ParameterInputFieldType FieldType { get; }

        public bool ShowOpenFileButton => parameterDefinition.IsPath;

        public ICommand OpenFileCommand { get; } 
        
        public IReadOnlyList<string> ComboBoxItems { get; }

        public ParameterInputFieldViewModel(IParameterDefinition parameterDefinition, string help, ParameterSet scriptParameterSet)
        {
            this.parameterDefinition = parameterDefinition;
            _scriptParameterSet = scriptParameterSet;
            
            using var reader = new StringReader(help);
            HelpFirstLine = reader.ReadLine() ?? Name;
            HelpDetail = reader.ReadToEnd() ?? "";
            HasHelp = !string.IsNullOrEmpty(help);
            
            ComboBoxItems = parameterDefinition.ValidateSetValues.ToList();
            FieldType = GetFieldType();
            
            OpenFileCommand = new DelegateCommand(_ => OnOpenFile(), _ => ShowOpenFileButton);
            
            return;

            
            ParameterInputFieldType GetFieldType()
            {
                if (ComboBoxItems.Any())
                {
                    return ParameterInputFieldType.ComboBox;
                }

                if (parameterDefinition.IsBool())
                {
                    return ParameterInputFieldType.ToggleButton;
                }

                return parameterDefinition.ConstraintType switch
                {
                    var t when t == typeof(int) => ParameterInputFieldType.IntBox,
                    var t when t == typeof(uint) => ParameterInputFieldType.UintBox,
                    var t when t == typeof(float) => ParameterInputFieldType.FloatBox,
                    var t when t == typeof(double) => ParameterInputFieldType.DoubleBox,
                    _ => ParameterInputFieldType.TextBox
                };
            }
        }
        
        private void OnOpenFile()
        {
            var dialog = new OpenFileOrFolderDialog();
       
            if (dialog.ShowDialog())
            {
                Value = dialog.FileOrFolderName;
            }
        }
    }
}
