using Substrate.NetApi;
using System.Text.Json;
using System.Text.Json.Serialization;
using XcavateProfileApiClient;

namespace XcavateProfile.Client;

/// <summary>
/// Represents a user profile with properties for address, nickname, bio, profile picture, and encryption key
/// </summary>
public class Profile : IPayloadBody
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [JsonPropertyName("ss58address")]
    public required string Ss58Address { get; set; }

    [JsonPropertyName("nickname")]
    public string? Nickname { get; set; }

    [JsonPropertyName("bio")]
    public string? Bio { get; set; }

    [JsonPropertyName("profilePicture")]
    public string? ProfilePicture { get; set; }

    [JsonPropertyName("x25519Key")]
    public required string X25519Key { get; set; }

    public string Hash()
    {
        var json = JsonSerializer.Serialize(this, JsonOptions);
        var hash = CryptoHelper.Hash(json);
        return Utils.Bytes2HexString(hash);
    }
}
