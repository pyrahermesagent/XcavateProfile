namespace XcavateProfileApiClient;

public class XcavateProfileApiClientOptions
{
    public required string ApiUrl { get; set; }
    public TimeSpan TimestampSkew { get; set; } = TimeSpan.FromMinutes(5);
}
