using GiaPhuWaterDelivery.Api.Helpers;

namespace GiaPhuWaterDelivery.Api.Tests.Helpers;

public sealed class VietnamTimeTests
{
    [Fact]
    public void Vietnam_zone_has_seven_hour_utc_offset()
    {
        var localDate = new DateTime(2026, 7, 23, 12, 0, 0, DateTimeKind.Unspecified);

        Assert.Equal(TimeSpan.FromHours(7), VietnamTime.Zone.GetUtcOffset(localDate));
    }

    [Fact]
    public void ToUtc_interprets_input_as_vietnam_local_time()
    {
        var localDate = new DateTime(2026, 7, 23, 12, 30, 0);

        var utc = VietnamTime.ToUtc(localDate);

        Assert.Equal(new DateTime(2026, 7, 23, 5, 30, 0, DateTimeKind.Utc), utc);
        Assert.Equal(DateTimeKind.Utc, utc.Kind);
    }

    [Fact]
    public void Now_matches_conversion_from_current_utc_time()
    {
        var expected = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTime.Zone);

        Assert.InRange(
            VietnamTime.Now,
            expected.AddSeconds(-1),
            expected.AddSeconds(1));
    }
}
