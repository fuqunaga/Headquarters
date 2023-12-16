using System.Data;
using System.Globalization;
using System.Windows.Controls;

namespace Headquarters;

public class NotContainDataColumnCollectionValidationRule(DataColumnCollection collection, string invalidMessage):  ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        return (collection != null && value != null && collection.Contains((string)value))
            ? new ValidationResult(false, invalidMessage)
            : ValidationResult.ValidResult;
    }
}
