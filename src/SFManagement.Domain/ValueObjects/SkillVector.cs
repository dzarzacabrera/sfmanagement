using SFManagement.Domain.Enums;

namespace SFManagement.Domain.ValueObjects;

public class SkillVector
{
    private readonly float[] _values;

    public int Dimensions => _values.Length;

    public SkillVector(float[] values)
    {
        if (values is null || values.Length == 0)
            throw new ArgumentException("Vector must have at least one dimension.", nameof(values));

        _values = values.Select(v => Math.Clamp(v, 0.0f, 10.0f)).ToArray();
    }

    public float this[int index] => _values[index];

    public float[] ToArray() => _values.ToArray();

    public SkillVector ApplyImpact(int skillPosition, double basePoints, double criticalityMultiplier)
    {
        if (skillPosition < 0 || skillPosition >= _values.Length)
            throw new ArgumentOutOfRangeException(nameof(skillPosition));

        var impact = (float)(basePoints * criticalityMultiplier);
        var updated = _values.ToArray();
        updated[skillPosition] = Math.Clamp(updated[skillPosition] + impact, 0.0f, 10.0f);
        return new SkillVector(updated);
    }

    public static double CalculateCriticalityMultiplier(Criticality criticality) => criticality switch
    {
        Enums.Criticality.Low      => 0.5,
        Enums.Criticality.Medium   => 1.0,
        Enums.Criticality.High     => 1.5,
        Enums.Criticality.Critical => 2.0,
        _ => throw new ArgumentOutOfRangeException(nameof(criticality), criticality, null)
    };

    public override bool Equals(object? obj) =>
        obj is SkillVector other && _values.SequenceEqual(other._values);

    public override int GetHashCode() =>
        _values.Aggregate(17, (hash, val) => hash * 31 + val.GetHashCode());
}
