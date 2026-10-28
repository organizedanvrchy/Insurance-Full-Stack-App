using System.Globalization;
using System.Text.Json;
using Insurance.BuildingBlocks.Exceptions;
using Insurance.Quotes.Enums;

namespace Insurance.Quotes.Rating;

/// <summary>Customer answers that have been checked against the question definitions.</summary>
public class AnswerSet
{
    private readonly Dictionary<string, object> _values;
    private AnswerSet(Dictionary<string, object> values) => _values = values;

    public int Int(string key) => (int)_values[key];
    public bool Bool(string key) => (bool)_values[key];
    public DateOnly Date(string key) => (DateOnly)_values[key];
    public string Choice(string key) => (string)_values[key];

    /// <summary>The validated answers, stored with the quote as an audit trail.</summary>
    public IReadOnlyDictionary<string, object> Values => _values;

    public static AnswerSet From(IReadOnlyList<QuestionDefinition> questions, IReadOnlyDictionary<string, JsonElement>? raw)
    {
        var values = new Dictionary<string, object>();

        foreach (var q in questions)
        {
            JsonElement el = default;
            var present = raw is not null
                          && raw.TryGetValue(q.Key, out el)
                          && el.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined);

            if (!present)
                throw new BusinessRuleException($"Please answer: {q.Label}");

            values[q.Key] = q.Kind switch
            {
                QuestionType.Number => ParseNumber(q, el),
                QuestionType.Boolean => ParseBool(q, el),
                QuestionType.Date => ParseDate(q, el),
                QuestionType.Choice => ParseChoice(q, el),
                _ => throw new InvalidOperationException($"Unknown question kind {q.Kind}")
            };
        }

        return new AnswerSet(values);
    }

    private static object ParseNumber(QuestionDefinition q, JsonElement el)
    {
        if (el.ValueKind != JsonValueKind.Number || !el.TryGetInt32(out var n))
            throw new BusinessRuleException($"{q.Label}: please enter a whole number.");

        if (n < (q.Min ?? int.MinValue) || n > (q.Max ?? int.MaxValue))
            throw new BusinessRuleException($"{q.Label}: enter a value between {q.Min} and {q.Max}.");

        return n;
    }

    private static object ParseBool(QuestionDefinition q, JsonElement el) =>
        el.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new BusinessRuleException($"{q.Label}: please answer yes or no.")
        };

    private static object ParseDate(QuestionDefinition q, JsonElement el)
    {
        if (el.ValueKind != JsonValueKind.String ||
            !DateOnly.TryParseExact(el.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            throw new BusinessRuleException($"{q.Label}: please enter a valid date.");

        return d;
    }

    private static object ParseChoice(QuestionDefinition q, JsonElement el)
    {
        var s = el.ValueKind == JsonValueKind.String ? el.GetString() : null;
        if (s is null || !q.Options.Contains(s))
            throw new BusinessRuleException($"{q.Label}: please choose one of the listed options.");

        return s;
    }
}