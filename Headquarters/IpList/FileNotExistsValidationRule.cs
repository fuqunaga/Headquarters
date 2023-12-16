using System.Globalization;
using System.IO;
using System.Windows.Controls;

namespace Headquarters;

public class FileNotExistsValidationRule(string folderPath) : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        var fileName = $"{value}.csv";

        return File.Exists(Path.Combine(folderPath, fileName))
            ? new ValidationResult(false, "File already exists")
            : ValidationResult.ValidResult;
    }
}