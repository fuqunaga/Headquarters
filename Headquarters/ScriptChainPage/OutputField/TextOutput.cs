namespace Headquarters;

public readonly struct TextOutput(OutputIcon icon, string label, string text) : IOutputUnit
{
    public OutputIcon Icon { get; } = icon;
    public string Label { get; } = label;
    public string Text { get; } = text;
}