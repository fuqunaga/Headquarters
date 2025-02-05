namespace Headquarters;

public class DialogViewModelBase : ViewModelBase
{
    public string? Title { get; set; }
        
    public string CancelButtonContent { get; set; } = "Cancel";
    public string OkButtonContent { get; set; } = "Ok";
        
    public bool IsEditable { get; set; } = true;
}