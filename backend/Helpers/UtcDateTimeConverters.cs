using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GiaPhuWaterDelivery.Api.Helpers;

public sealed class UtcDateTimeConverter()
    : ValueConverter<DateTime, DateTime>(
        value => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(),
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

public sealed class NullableUtcDateTimeConverter()
    : ValueConverter<DateTime?, DateTime?>(
        value => value.HasValue
            ? (value.Value.Kind == DateTimeKind.Utc
                ? value.Value
                : value.Value.ToUniversalTime())
            : value,
        value => value.HasValue
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : value);
