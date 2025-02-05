using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;

namespace Headquarters;

/// <summary>
/// Dialogを表示するサービス
/// </summary>
public static class DialogService
{
    private static readonly Dialog Dialog = new();

    public static async Task<(bool success, string)> ShowDialog(TextDialogViewModel viewModel)
    {
        Dialog.DataContext = viewModel;
        var result = await DialogHost.Show(Dialog, "RootDialog");

        return (
            result != null && (bool)result,
            viewModel.Text
        );
    }
}