namespace Headquarters;

public class NotEmptyValidator(string invalidMessage) : Validator<string>(invalidMessage)
{
    public override bool Validate(string value) => !string.IsNullOrEmpty(value);
}
