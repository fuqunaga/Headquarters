namespace Headquarters;

public interface IOutputUnit
{
    OutputIcon Icon { get; }
    string Label { get; }
    string Text { get; }
}