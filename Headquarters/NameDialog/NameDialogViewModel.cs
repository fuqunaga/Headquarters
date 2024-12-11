using System.Collections.Generic;
using System.Linq;

namespace Headquarters
{
    public class NameDialogViewModel
    {
        public string? Title { get; set; }
        
        public string CancelButtonContent { get; set; } = "Cancel";
        public string OkButtonContent { get; set; } = "Ok";
        
        public string? Name { get; set; }
        public bool HasSuggestions => Suggestions != null && Suggestions.Any();
        public IEnumerable<string>? Suggestions { get; set; }
        public string? Suffix { get; set; }
        
        public bool IsEnabled { get; set; } = true;
    }
}
