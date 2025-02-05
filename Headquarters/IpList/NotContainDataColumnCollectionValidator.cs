using System.Data;

namespace Headquarters;

public class NotContainDataColumnCollectionValidator(DataColumnCollection dataColumnCollection, string invalidMessage)
    : Validator<string>(invalidMessage)
{
    public override bool Validate(string value) => !dataColumnCollection.Contains(value);
}
