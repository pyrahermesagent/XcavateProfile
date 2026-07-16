namespace XcavateProfileApi.Middleware;

public class SignatureValidationOptions
{
    public TimeSpan TimestampSkew { get; set; } = TimeSpan.FromMinutes(5);
}

public class SignatureValidationResult
{
    public bool IsValid { get; set; }
    public string? Error { get; set; }
    public string? Ss58Address { get; set; }
}
