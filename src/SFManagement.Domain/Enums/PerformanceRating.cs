namespace SFManagement.Domain.Enums;

public enum PerformanceRating
{
    Poor,
    Average,
    Good,
    Excellent
}

public static class PerformanceRatingExtensions
{
    public static double ToBasePoints(this PerformanceRating rating) => rating switch
    {
        PerformanceRating.Poor => -0.5,
        PerformanceRating.Average => 0.0,
        PerformanceRating.Good => 0.2,
        PerformanceRating.Excellent => 0.5,
        _ => throw new ArgumentOutOfRangeException(nameof(rating), rating, null)
    };
}
