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
            typeof(FileInfo),
            typeof(DirectoryInfo)
        };
        
        private readonly IpListViewModel _ipListViewModel;
        private readonly ParameterSet _scriptParameterSet;

        private ScriptParameterInputFieldType _baseFieldType;
        
        public string Name { get; }
        public string Value
        {
            get => _scriptParameterSet.Get(Name);
            set
            {
                if (IsUseIpListParameter) return;
                if ( _scriptParameterSet.Set(Name, value) )
                {
                    OnPropertyChanged();
                }
            }
        }
        
        public string HelpFirstLine { get; }
        public string HelpDetail { get;  }
        public ScriptParameterInputFieldType FieldType => IsUseIpListParameter ? ScriptParameterInputFieldType.UseIpList : _baseFieldType; 
        
        public bool IsUseIpListParameter => _ipListViewModel.DataGridViewModel.Contains(Name);
        public bool IsExpectFileSystemInfo { get; }

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
            IsExpectFileSystemInfo = ExpectedType?.IsSubclassOf(typeof(FileSystemInfo)) ?? false;
            
            OpenFileCommand = new DelegateCommand(_ => OnOpenFile(), _ => IsExpectFileSystemInfo);
           
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
            
            ComboBoxItems = scriptParameter.ValidateSetValues.ToList();
            _baseFieldType = ComboBoxItems.Any() 
                ? ScriptParameterInputFieldType.ComboBox
                : ScriptParameterInputFieldType.TextBox;
        }

        private static Type? GetExpectedType(ScriptParameter scriptParameter)
        {
            return scriptParameter.AttributeTypes.FirstOrDefault(attr => SupportedExpectedTypes.Contains(attr));
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
