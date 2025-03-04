using System.Linq;

namespace Headquarters
{
    /// <summary>
    /// スクリプトのパラメータを表すViewModel
    /// ParameterSetとIPListを参照し適切な値を取得・設定する
    /// </summary>
    public class ScriptParameterInputFieldViewModel : ParameterInputFieldViewModel
    {
        private readonly IpListViewModel _ipListViewModel;

        public override string Value
        {
            get => base.Value;
            set
            {
                if (IsUseIpListParameter) return;
                base.Value = value;
            }
        }
        
        public override ParameterInputFieldType FieldType => IsUseIpListParameter 
            ? ParameterInputFieldType.UseIpList 
            : base.FieldType;

        private bool IsUseIpListParameter => _ipListViewModel.DataGridViewModel.Contains(Name);

        public ScriptParameterInputFieldViewModel(ScriptParameterDefinition parameterDefinition, string help, IpListViewModel ipListViewModel, ParameterSet scriptParameterSet)
            : base(parameterDefinition, help, scriptParameterSet)
        {
            _ipListViewModel = ipListViewModel;
            _ipListViewModel.DataGridViewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(IpListDataGridViewModel.Items))
                {
                    OnPropertyChanged(nameof(FieldType));
                }
            };
        }
        
        public ScriptParameterInputFieldViewModel InitializeValueIfEmpty()
        {
            if (string.IsNullOrEmpty(Value))
            {
                Value = parameterDefinition.DefaultValue?.ToString() 
                        ?? (ComboBoxItems.FirstOrDefault()
                            ?? ""
                        );
            }

            return this;
        }

        // スクリプト実行用のパラメータを取得する
        // bool値はstringのままだとエラーになるのでキャストする
        public object GetParameterForScript(string? ipListParameter = null)
        {
            var stringValue = ipListParameter ?? Value;
            
            if (parameterDefinition.IsBool())
            {
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return false;
                }
                
                if ( bool.TryParse(stringValue, out var boolValue))
                {
                    return boolValue;
                }
            }

            return stringValue;
        }
    }
}
