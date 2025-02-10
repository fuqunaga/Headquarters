using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;

namespace Headquarters;

/// <summary>
/// Dialogを表示するサービス
/// </summary>
public static class DialogService
{
    private static readonly Dialog Dialog = new();

    public static async Task<bool> ShowDialog(DialogViewModelBase viewModel, string dialogIdentifier = "RootDialog")
    {
        Dialog.DataContext = viewModel;
        var result = await DialogHost.Show(Dialog, dialogIdentifier);

        return result != null && (bool)result;
    }
    
    public static void CloseDialog(string dialogIdentifier = "RootDialog")
    {
        DialogHost.Close(dialogIdentifier);
    }
}