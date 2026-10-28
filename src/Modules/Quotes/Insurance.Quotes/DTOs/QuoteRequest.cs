using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Insurance.Quotes.DTOs;

public class QuoteRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "PolicyId is required.")]
    public int PolicyId { get; set; }

    // Questions differ per policy type, Answers arrive as key/value pairs.
    // AnswerSet validates Answers against that policy type's question list.
    [Required]
    public Dictionary<string, JsonElement>? Answers { get; set; }
}