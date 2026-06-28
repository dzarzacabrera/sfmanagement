using FluentAssertions;
using SFManagement.Domain.Enums;
using SFManagement.Domain.ValueObjects;

namespace SFManagement.UnitTests;

public class SkillVectorTests
{
    private static readonly float[] DefaultValues = [6.0f, 4.0f, 8.0f, 2.0f, 0.0f, 10.0f];

    [Fact]
    public void Constructor_ClampsValuesAboveTen()
    {
        var vector = new SkillVector([12.0f, 15.0f, 9.0f]);

        vector[0].Should().Be(10.0f);
        vector[1].Should().Be(10.0f);
        vector[2].Should().Be(9.0f);
    }

    [Fact]
    public void Constructor_ClampsValuesBelowZero()
    {
        var vector = new SkillVector([-1.0f, -5.0f, 3.0f]);

        vector[0].Should().Be(0.0f);
        vector[1].Should().Be(0.0f);
        vector[2].Should().Be(3.0f);
    }

    [Fact]
    public void Constructor_ThrowsOnEmptyArray()
    {
        Action act = () => new SkillVector([]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ApplyImpact_PoorHigh_DecreasesSkill()
    {
        var vector = new SkillVector([6.0f]);
        var result = vector.ApplyImpact(0, PerformanceRating.Poor.ToBasePoints(),
            SkillVector.CalculateCriticalityMultiplier(Criticality.High));

        result[0].Should().Be(5.25f);
    }

    [Fact]
    public void ApplyImpact_ExcellentCritical_ClampsToTen()
    {
        var vector = new SkillVector([9.5f]);
        var result = vector.ApplyImpact(0, PerformanceRating.Excellent.ToBasePoints(),
            SkillVector.CalculateCriticalityMultiplier(Criticality.Critical));

        result[0].Should().Be(10.0f);
    }

    [Fact]
    public void ApplyImpact_PoorCritical_ClampsToZero()
    {
        var vector = new SkillVector([0.5f]);
        var result = vector.ApplyImpact(0, PerformanceRating.Poor.ToBasePoints(),
            SkillVector.CalculateCriticalityMultiplier(Criticality.Critical));

        result[0].Should().Be(0.0f);
    }

    [Fact]
    public void ApplyImpact_Average_DoesNotChange()
    {
        var vector = new SkillVector([6.0f]);
        var result = vector.ApplyImpact(0, PerformanceRating.Average.ToBasePoints(),
            SkillVector.CalculateCriticalityMultiplier(Criticality.Medium));

        result[0].Should().Be(6.0f);
    }

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var v1 = new SkillVector([1.0f, 2.0f, 3.0f]);
        var v2 = new SkillVector([1.0f, 2.0f, 3.0f]);

        v1.Equals(v2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        var v1 = new SkillVector([1.0f, 2.0f]);
        var v2 = new SkillVector([1.0f, 3.0f]);

        v1.Equals(v2).Should().BeFalse();
    }
}
