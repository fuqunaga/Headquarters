namespace Headquarters;

public class DialogViewModelBase : ViewModelBase
{
    public string? Title { get; set; }
    public string CancelButtonContent { get; set; } = "Cancel";
    public string OkButtonContent { get; set; } = "Ok";
    public virtual bool IsOkButtonEnabled { get; set; } = true;
}