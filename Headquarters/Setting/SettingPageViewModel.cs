using System.Collections.ObjectModel;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;

namespace Headquarters;

public class SettingPageViewModel : ViewModelBase
{
    public ObservableCollection<object> Fields { get; } = [];
    public ICommand OpenProfileWindowCommand { get; } = new DelegateCommand(_ =>
    {
        DialogService.CloseDialog();
        
        ProfileWindow.Instance.DataContext = new ProfileWindowViewModel();
        ProfileWindow.Instance.ShowDialog();
    });

    public void InitializeWithGlobalParameter()
    {
        Fields.Clear();
        Fields.Add(new HelpTextBlockViewModel(GlobalParameter.UserNameAndPasswordDescription));
        Fields.Add(GlobalParameter.CreateParameterInputFieldViewModel(GlobalParameter.UserNameParameterName));
        Fields.Add(GlobalParameter.CreateParameterInputFieldViewModel(GlobalParameter.UserPasswordParameterName));
        Fields.Add(Separator.Instance);
        Fields.Add(GlobalParameter.CreateParameterInputFieldViewModel(GlobalParameter.ShowConfirmationDialogOnExecuteParameterName));
    }
}