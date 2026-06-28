using FluentAssertions;
using SFManagement.Domain.Enums;
using SFManagement.Domain.ValueObjects;

namespace SFManagement.UnitTests;

public class XpCalculationTests
{
    [Theory]
    [InlineData(Criticality.Low, 0.5)]
    [InlineData(Criticality.Medium, 1.0)]
    [InlineData(Criticality.High, 1.5)]
    [InlineData(Criticality.Critical, 2.0)]
    public void CriticalityMultiplier_ReturnsCorrectValue(Criticality criticality, double expected)
    {
        var result = SkillVector.CalculateCriticalityMultiplier(criticality);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(PerformanceRating.Poor, -0.5)]
    [InlineData(PerformanceRating.Average, 0.0)]
    [InlineData(PerformanceRating.Good, 0.2)]
    [InlineData(PerformanceRating.Excellent, 0.5)]
    public void PerformanceRating_ToBasePoints_ReturnsCorrectValue(PerformanceRating rating, double expected)
    {
        var result = rating.ToBasePoints();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(PerformanceRating.Poor, Criticality.High, -0.75)]
    [InlineData(PerformanceRating.Good, Criticality.Critical, 0.4)]
    [InlineData(PerformanceRating.Excellent, Criticality.Low, 0.25)]
    public void Impact_BasePointsTimesMultiplier_IsCorrect(PerformanceRating rating, Criticality criticality, double expectedImpact)
    {
        var basePoints = rating.ToBasePoints();
        var multiplier = SkillVector.CalculateCriticalityMultiplier(criticality);
        var impact = basePoints * multiplier;
        impact.Should().Be(expectedImpact);
    }
}
