using System.Collections.Generic;

namespace Headquarters;

public class ListDialogViewModel : DialogViewModelBase
{
    public string Message { get; set; } = "";
    public IEnumerable<string> Items { get; set; } = [];
}