using System;

namespace Headquarters;

public abstract class Validator<T>(string invalidMessage)
{
    public abstract bool Validate(T value);
    public string InvalidMessage => invalidMessage;
}
