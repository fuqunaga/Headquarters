namespace Headquarters;

public static class CustomAttributeName
{
    public const string NamespaceName = "Headquarters";
    public const string Path = "Path";
    
    public static string WithNamespace(string name) => $"{NamespaceName}.{name}";
}
