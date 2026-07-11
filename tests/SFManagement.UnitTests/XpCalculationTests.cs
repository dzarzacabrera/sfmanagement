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
    [InlineData(-0.5, Criticality.High, -0.75)]
    [InlineData(0.2, Criticality.Critical, 0.4)]
    [InlineData(0.5, Criticality.Low, 0.25)]
    public void Impact_BasePointsTimesMultiplier_IsCorrect(double basePoints, Criticality criticality, double expectedImpact)
    {
        var multiplier = SkillVector.CalculateCriticalityMultiplier(criticality);
        var impact = basePoints * multiplier;
        impact.Should().Be(expectedImpact);
    }
}
