namespace Headquarters
{
    public class NameDialogViewModel
    {
        public string? Title { get; set; }
        
        public string CancelButtonContent { get; set; } = "Cancel";
        public string OkButtonContent { get; set; } = "Ok";
        
        public string? Suffix { get; set; }
        public string? Name { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}
