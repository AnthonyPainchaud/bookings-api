using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bookings.IntegrationTests.TestSupport;

/// <summary>
/// JSON options mirroring the API's own serialization (camelCase, string
/// enums, case-insensitive). <see cref="HttpClient"/>'s JSON helpers default to
/// bare <see cref="JsonSerializerOptions.Default"/> (case-sensitive, numeric
/// enums) when no options are supplied, which would silently fail to bind the
/// API's actual camelCase/string-enum responses.
/// </summary>
public static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
