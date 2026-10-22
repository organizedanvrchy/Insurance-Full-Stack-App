using InsuranceApi.Enums;

namespace InsuranceApi.Rating;

// Questions several policy types share.
public static class CommonQuestions
{
    public static QuestionDefinition DateOfBirth() => new()
    {
        Key = "dateOfBirth",
        Label = "Date of birth",
        Kind = QuestionType.Date
    };

    public static QuestionDefinition Tobacco() => new()
    {
        Key = "tobaccoUse",
        Label = "Do you use tobacco or nicotine products?",
        Kind = QuestionType.Boolean
    };

    public static QuestionDefinition Conditions() => new()
    {
        Key = "conditions",
        Label = "Any pre-existing medical conditions?",
        Kind = QuestionType.Choice,
        Options = new() { "None", "Minor", "Major" },
        HelpText = "Minor: well managed (for example mild asthma). Major: serious or ongoing."
    };
}