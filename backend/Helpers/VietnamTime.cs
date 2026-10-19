namespace GiaPhuWaterDelivery.Api.Helpers;

public static class VietnamTime
{
    public static TimeZoneInfo Zone { get; } = FindZone();

    public static DateTime Now =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

    public static DateTime ToUtc(DateTime localUnspecified) =>
        TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(localUnspecified, DateTimeKind.Unspecified), Zone);

    public static DateTime FromUtc(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    private static TimeZoneInfo FindZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
    }
}
