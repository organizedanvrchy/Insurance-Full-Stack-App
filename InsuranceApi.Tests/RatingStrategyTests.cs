using System.Text.Json;
using InsuranceApi.Exceptions;
using InsuranceApi.Rating;
using Xunit;

namespace InsuranceApi.Tests;

public class RatingStrategyTests
{
    // A fixed "today", so these results never 
    // change as the calendar moves on.
    private static readonly DateOnly Today = new(2026, 10, 22);

    private static string Json(object answers) => JsonSerializer.Serialize(answers);

    private static RatingResult Rate(IRatingStrategy strategy, decimal baseRate, string answersJson)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(answersJson)!;
        var answers = AnswerSet.From(strategy.Questions, raw);     // validates the answers first
        return strategy.Rate(baseRate, answers, Today);
    }

    // Prices: premium = base x every factor

    [Theory]
    [InlineData(120.0, """{"dateOfBirth":"2004-05-10","vehicleYear":2020,"atFaultAccidents":1,"annualMileage":"7,500 to 15,000"}""", 249.60)]  // age 22 x1.60, 1 accident x1.30
    [InlineData(140.0, """{"dateOfBirth":"2004-05-10","vehicleYear":2020,"atFaultAccidents":0,"annualMileage":"7,500 to 15,000"}""", 224.00)]  // age 22 x1.60 only
    public void Auto_prices_follow_the_rating_rules(double baseRate, string answers, double expected)
    {
        var result = Rate(new AutoRatingStrategy(), (decimal)baseRate, answers);

        Assert.Equal((decimal)expected, result.MonthlyPremium);
    }

    [Theory]
    [InlineData(450.0, """{"dateOfBirth":"1998-03-01","tobaccoUse":false,"conditions":"None"}""", 382.50)]   // age 28 x0.85
    [InlineData(450.0, """{"dateOfBirth":"1974-01-15","tobaccoUse":false,"conditions":"None"}""", 607.50)]   // age 52 x1.35
    [InlineData(450.0, """{"dateOfBirth":"1986-04-04","tobaccoUse":true,"conditions":"Major"}""", 913.50)]   // x1.00 x1.40 x1.45
    public void Health_prices_follow_the_rating_rules(double baseRate, string answers, double expected)
    {
        var result = Rate(new HealthRatingStrategy(), (decimal)baseRate, answers);

        Assert.Equal((decimal)expected, result.MonthlyPremium);
    }

    [Theory]
    [InlineData(29.0, """{"dateOfBirth":"1998-03-01","tobaccoUse":false,"conditions":"None"}""", 20.30)]    // age 28 x0.70
    [InlineData(29.0, """{"dateOfBirth":"1991-02-10","tobaccoUse":true,"conditions":"None"}""", 55.10)]     // age 35 x1.00, tobacco x1.90
    public void Life_prices_follow_the_rating_rules(double baseRate, string answers, double expected)
    {
        var result = Rate(new LifeRatingStrategy(), (decimal)baseRate, answers);

        Assert.Equal((decimal)expected, result.MonthlyPremium);
    }

    [Theory]
    [InlineData(210.0, """{"yearBuilt":2015,"securitySystem":true,"priorClaims":0}""", 189.00)]    // x1.00 x0.90 x1.00
    [InlineData(210.0, """{"yearBuilt":1985,"securitySystem":false,"priorClaims":1}""", 289.80)]   // x1.15 x1.00 x1.20
    public void Home_prices_follow_the_rating_rules(double baseRate, string answers, double expected)
    {
        var result = Rate(new HomeRatingStrategy(), (decimal)baseRate, answers);

        Assert.Equal((decimal)expected, result.MonthlyPremium);
    }

    [Fact]
    public void Travel_prices_follow_the_rating_rules()
    {
        var answers = Json(new { dateOfBirth = "1961-03-03", tripLengthDays = 21, destination = "International" });

        var result = Rate(new TravelRatingStrategy(), 120m, answers);

        Assert.Equal(304.50m, result.MonthlyPremium);   // age 65 x1.40, 21 days x1.25, international x1.45
    }

    [Fact]
    public void The_result_explains_how_the_price_was_built()
    {
        var answers = Json(new { dateOfBirth = "2004-05-10", vehicleYear = 2020, atFaultAccidents = 1, annualMileage = "7,500 to 15,000" });

        var result = Rate(new AutoRatingStrategy(), 120m, answers);

        Assert.Equal(120m, result.BasePremium);
        Assert.Equal(4, result.Factors.Count);
        Assert.Contains(result.Factors, f => f.Label == "Driver age 22" && f.Multiplier == 1.60m);
    }

    // Underwriting declines

    [Fact]
    public void Auto_declines_four_or_more_at_fault_accidents()
    {
        var answers = Json(new { dateOfBirth = "1990-01-01", vehicleYear = 2020, atFaultAccidents = 4, annualMileage = "Under 7,500" });

        var ex = Assert.Throws<BusinessRuleException>(() => Rate(new AutoRatingStrategy(), 120m, answers));

        Assert.Contains("unable to offer", ex.Message);
    }

    [Fact]
    public void Auto_declines_drivers_under_18()
    {
        var answers = Json(new { dateOfBirth = "2010-06-01", vehicleYear = 2020, atFaultAccidents = 0, annualMileage = "Under 7,500" });

        Assert.Throws<BusinessRuleException>(() => Rate(new AutoRatingStrategy(), 120m, answers));
    }

    [Fact]
    public void Health_declines_applicants_under_18()
    {
        var answers = Json(new { dateOfBirth = "2010-06-01", tobaccoUse = false, conditions = "None" });

        Assert.Throws<BusinessRuleException>(() => Rate(new HealthRatingStrategy(), 450m, answers));
    }

    [Fact]
    public void Life_declines_applicants_aged_70_or_over()
    {
        var answers = Json(new { dateOfBirth = "1950-01-01", tobaccoUse = false, conditions = "None" });

        Assert.Throws<BusinessRuleException>(() => Rate(new LifeRatingStrategy(), 29m, answers));
    }

    [Fact]
    public void Home_declines_three_or_more_prior_claims()
    {
        var answers = Json(new { yearBuilt = 2000, securitySystem = true, priorClaims = 3 });

        Assert.Throws<BusinessRuleException>(() => Rate(new HomeRatingStrategy(), 210m, answers));
    }

    // Answer validation (runs before any pricing)

    [Fact]
    public void A_missing_answer_is_rejected()
    {
        var answers = Json(new { dateOfBirth = "2004-05-10" });    // the other three questions are missing

        var ex = Assert.Throws<BusinessRuleException>(() => Rate(new AutoRatingStrategy(), 120m, answers));

        Assert.Contains("Please answer", ex.Message);
    }

    [Fact]
    public void A_number_question_rejects_text()
    {
        var answers = Json(new { dateOfBirth = "2004-05-10", vehicleYear = "abc", atFaultAccidents = 0, annualMileage = "Under 7,500" });

        var ex = Assert.Throws<BusinessRuleException>(() => Rate(new AutoRatingStrategy(), 120m, answers));

        Assert.Contains("whole number", ex.Message);
    }

    [Fact]
    public void A_number_outside_the_allowed_range_is_rejected()
    {
        var answers = Json(new { dateOfBirth = "2004-05-10", vehicleYear = 1900, atFaultAccidents = 0, annualMileage = "Under 7,500" });

        var ex = Assert.Throws<BusinessRuleException>(() => Rate(new AutoRatingStrategy(), 120m, answers));

        Assert.Contains("between", ex.Message);
    }

    [Fact]
    public void A_choice_must_be_one_of_the_listed_options()
    {
        var answers = Json(new { dateOfBirth = "2004-05-10", vehicleYear = 2020, atFaultAccidents = 0, annualMileage = "Lots" });

        var ex = Assert.Throws<BusinessRuleException>(() => Rate(new AutoRatingStrategy(), 120m, answers));

        Assert.Contains("listed options", ex.Message);
    }

    [Fact]
    public void A_yes_no_question_rejects_text()
    {
        var answers = Json(new { dateOfBirth = "1990-01-01", tobaccoUse = "yes", conditions = "None" });

        var ex = Assert.Throws<BusinessRuleException>(() => Rate(new HealthRatingStrategy(), 450m, answers));

        Assert.Contains("yes or no", ex.Message);
    }

    [Fact]
    public void A_date_question_rejects_the_wrong_format()
    {
        var answers = Json(new { dateOfBirth = "05/10/2004", tobaccoUse = false, conditions = "None" });

        var ex = Assert.Throws<BusinessRuleException>(() => Rate(new HealthRatingStrategy(), 450m, answers));

        Assert.Contains("valid date", ex.Message);
    }
}