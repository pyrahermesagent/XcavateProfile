namespace XcavateProfile.Client;

public class XcavateProfileClientOptions
{
    public required string ApiUrl { get; set; }
    public TimeSpan TimestampSkew { get; set; } = TimeSpan.FromMinutes(5);
}
