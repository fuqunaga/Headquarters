using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Headquarters;

public class ComboBoxDialogViewModel : TextBoxDialogViewModel
{
    public IEnumerable<string> Suggestions { get; set; } = [];
}