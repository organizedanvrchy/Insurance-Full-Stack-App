using InsuranceApi.Enums;

namespace InsuranceApi.Rating;

public class QuestionDefinition
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public QuestionType Kind { get; init; }
    public string? HelpText { get; init; }
    public int? Min { get; init; }          // Number questions
    public int? Max { get; init; }
    public List<string> Options { get; init; } = new();   // Choice questions
}