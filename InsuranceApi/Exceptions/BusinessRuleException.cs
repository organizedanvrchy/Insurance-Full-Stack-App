namespace InsuranceApi.Exceptions;

/// <summary>
/// A rule our code enforces (e.g. date ranges). 
/// Controllers turn it into a 400.
/// </summary>
public class BusinessRuleException: Exception
{
    public BusinessRuleException(string message) : base(message) {}
}