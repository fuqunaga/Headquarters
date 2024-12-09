using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        
        public string Name { get; }
        public string Value
        {
            get => _scriptParameterSet.Get(Name);
            set
            {
                if (IsDependIp) return;
                if ( _scriptParameterSet.Set(Name, value) )
                {
                    OnPropertyChanged();
                }
            }
        }
        
        public string HelpFirstLine { get; private set; }
        public string HelpDetail { get; private set; }
        public bool IsDependIp => _ipListViewModel.DataGridViewModel.Contains(Name);
        public bool IsExpectFileSystemInfo { get; }

        public ICommand OpenFileCommand { get; } 
        
        private Type? ExpectedType { get; set; }
        
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
                    OnPropertyChanged(nameof(IsDependIp));
                }
            };
        }

        private static Type? GetExpectedType(ScriptParameter scriptParameter)
        {
            return scriptParameter.Attributes.FirstOrDefault(attr => SupportedExpectedTypes.Contains(attr));
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
