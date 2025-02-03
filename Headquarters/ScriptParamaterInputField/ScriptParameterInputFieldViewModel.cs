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
    /// スクリプトのパラメータを表すViewModel
    /// ParameterSetとIPListを参照し適切な値を取得・設定する
    /// </summary>
    public class ScriptParameterInputFieldViewModel : ViewModelBase
    {
        private static readonly IReadOnlyList<Type> SupportedExpectedTypes = new List<Type>
        {
            typeof(bool),
            typeof(SwitchParameter)
        };
        
        private readonly IpListViewModel _ipListViewModel;
        private readonly ParameterSet _scriptParameterSet;
        private readonly ScriptParameterInputFieldType _baseFieldType;
        
        public string Name { get; }
        public string Value
        {
            get => _scriptParameterSet.Get(Name);
            set
            {
                if (IsUseIpListParameter) return;
                if (_scriptParameterSet.Set(Name, value))
                {
                    OnPropertyChanged();
                }
            }
        }
        
        public string HelpFirstLine { get; }
        public string HelpDetail { get;  }
        public ScriptParameterInputFieldType FieldType => IsUseIpListParameter ? ScriptParameterInputFieldType.UseIpList : _baseFieldType; 
        
        public bool IsUseIpListParameter => _ipListViewModel.DataGridViewModel.Contains(Name);
        public bool ShowOpenFileButton { get; }

        public ICommand OpenFileCommand { get; } 
        
        private Type? ExpectedType { get; set; }
        public IReadOnlyList<string> ComboBoxItems { get; }

        public ScriptParameterInputFieldViewModel(ScriptParameter scriptParameter, string help, IpListViewModel ipListViewModel, ParameterSet scriptParameterSet)
        {
            using var reader = new StringReader(help);
            
            Name = scriptParameter.Name;
            HelpFirstLine = reader.ReadLine() ?? "";
            HelpDetail = reader.ReadToEnd() ?? "";

            ExpectedType = GetExpectedType(scriptParameter);
            ShowOpenFileButton = scriptParameter.AttributeNames.Contains(CustomAttributeName.WithNamespace(CustomAttributeName.Path));
            OpenFileCommand = new DelegateCommand(_ => OnOpenFile(), _ => ShowOpenFileButton);
            
            ComboBoxItems = scriptParameter.ValidateSetValues.ToList();
            _baseFieldType = ComboBoxItems.Any() switch
            {
                true => ScriptParameterInputFieldType.ComboBox,
                false when ExpectedType == typeof(bool) => ScriptParameterInputFieldType.ToggleButton,
                _ => ScriptParameterInputFieldType.TextBox
            };
           
            _ipListViewModel = ipListViewModel;
            _scriptParameterSet = scriptParameterSet;
            
            _ipListViewModel.DataGridViewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(IpListDataGridViewModel.Items))
                {
                    OnPropertyChanged(nameof(IsUseIpListParameter));
                    OnPropertyChanged(nameof(FieldType));
                }
            };
            
            
            // Valueが無かったらデフォルト値を入れる
            if (string.IsNullOrEmpty(Value))
            {
                Value = scriptParameter.DefaultValue?.ToString() ?? "";
            }
            
            // Valueが無くComboBoxなら値を入れる
            if (FieldType == ScriptParameterInputFieldType.ComboBox && string.IsNullOrEmpty(Value))
            {
                Value = ComboBoxItems.FirstOrDefault() ?? "";
            }
        }

        // スクリプト実行用のパラメータを取得する
        // bool値はstringのままだとエラーになるのでキャストする
        public object GetParameterForScript(string? ipListParameter = null)
        {
            var stringValue = ipListParameter ?? Value;
            
            if (ExpectedType == typeof(bool))
            {
                if ( bool.TryParse(stringValue, out var boolValue))
                {
                    return boolValue;
                }
            }

            return stringValue;
        }

        private static Type? GetExpectedType(ScriptParameter scriptParameter)
        {
            var type = scriptParameter.AttributeTypes.FirstOrDefault(attr => SupportedExpectedTypes.Contains(attr));
            if ( type == typeof(SwitchParameter))
            {
                type = typeof(bool);
            }

            return type;
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
