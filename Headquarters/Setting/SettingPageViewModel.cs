using System.Collections.ObjectModel;

namespace Headquarters;

public class SettingPageViewModel : ViewModelBase
{
    public ObservableCollection<object> Fields { get; } = [];
    
    public void InitializeWithGlobalParameter()
    {
        Fields.Add(new HelpTextBlockViewModel(GlobalParameter.UserNameAndPasswordDescription));
        Fields.Add(GlobalParameter.CreateParameterInputFieldViewModel(GlobalParameter.UserNameParameterName));
        Fields.Add(GlobalParameter.CreateParameterInputFieldViewModel(GlobalParameter.UserPasswordParameterName));
        Fields.Add(Separator.Instance);
        Fields.Add(GlobalParameter.CreateParameterInputFieldViewModel(GlobalParameter.ShowConfirmationDialogOnExecuteParameterName));
    }
}