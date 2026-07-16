using Substrate.NetApi;
using Substrate.NetApi.Model.Types;
using System.Text;
using System.Text.Json;
using XcavateProfileApiClient;

namespace XcavateProfile.Client;

public class XcavateProfileClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly XcavateProfileClientOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public XcavateProfileClient(XcavateProfileClientOptions options)
    {
        _options = options;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(_options.ApiUrl);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Get all profiles
    /// </summary>
    public async Task<List<Profile>> GetProfilesAsync()
    {
        var response = await _httpClient.GetAsync("api/profiles");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Profile>>(content, _jsonOptions) ?? new List<Profile>();
    }

    /// <summary>
    /// Get a profile by SS58 address
    /// </summary>
    public async Task<Profile?> GetProfileAsync(string ss58address)
    {
        var response = await _httpClient.GetAsync($"api/profiles/{ss58address}");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Profile>(content, _jsonOptions);
    }

    /// <summary>
    /// Get a profile by nickname
    /// </summary>
    public async Task<Profile?> GetProfileByNicknameAsync(string nickname)
    {
        var response = await _httpClient.GetAsync($"api/profiles/nickname/{nickname}");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Profile>(content, _jsonOptions);
    }

    /// <summary>
    /// Create a new profile with Sr25519 authentication
    /// </summary>
    public async Task<Profile> CreateProfileAsync(Profile profile, Account account)
    {
        if (account == null)
            throw new InvalidOperationException("Account is required for profile creation");

        // Serialize the profile to get the body JSON
        var bodyJson = JsonSerializer.Serialize(profile, _jsonOptions);

        // Construct the payload
        var timestamp = DateTime.UtcNow;
        var payload = CryptoHelper.ConstructPayload("POST", "/api/profiles", profile, timestamp);

        // Sign the payload using Account
        var signature = await CryptoHelper.SignAsync(payload, account);

        // Add authentication headers
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-SS58-Address", account.Value);
        _httpClient.DefaultRequestHeaders.Add("X-Signature", Utils.Bytes2HexString(signature));
        _httpClient.DefaultRequestHeaders.Add("X-Timestamp", timestamp.ToUniversalTime().ToString("o"));

        // Create the request content
        var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("api/profiles", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Profile>(responseContent, _jsonOptions) ?? throw new InvalidOperationException("Failed to create profile");
    }

    /// <summary>
    /// Update an existing profile with Sr25519 authentication
    /// </summary>
    public async Task<Profile> UpdateProfileAsync(string ss58address, Profile profile, Account? account = null)
    {
        if (account == null)
            throw new InvalidOperationException("Account is required for profile update");

        // Serialize the profile to get the body JSON
        var bodyJson = JsonSerializer.Serialize(profile, _jsonOptions);

        // Construct the payload
        var timestamp = DateTime.UtcNow;
        var payload = CryptoHelper.ConstructPayload("PUT", $"/api/profiles/{ss58address}", profile, timestamp);

        // Sign the payload using Account
        var signature = await CryptoHelper.SignAsync(payload, account);

        // Add authentication headers
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-SS58-Address", account.Value);
        _httpClient.DefaultRequestHeaders.Add("X-Signature", Utils.Bytes2HexString(signature));
        _httpClient.DefaultRequestHeaders.Add("X-Timestamp", timestamp.ToUniversalTime().ToString("o"));

        // Create the request content
        var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync($"api/profiles/{ss58address}", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Profile>(responseContent, _jsonOptions) ?? throw new InvalidOperationException("Failed to update profile");
    }

    /// <summary>
    /// Delete a profile with Sr25519 authentication
    /// </summary>
    public async Task DeleteProfileAsync(string ss58address, Account? account = null)
    {
        if (account == null)
            throw new InvalidOperationException("Account is required for profile deletion");

        // Construct the payload
        var timestamp = DateTime.UtcNow;
        var payload = CryptoHelper.ConstructPayload("DELETE", $"/api/profiles/{ss58address}", new EmptyPayloadBody(), timestamp);

        // Sign the payload using Account
        var signature = await CryptoHelper.SignAsync(payload, account);

        // Add authentication headers
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-SS58-Address", account.Value);
        _httpClient.DefaultRequestHeaders.Add("X-Signature", Utils.Bytes2HexString(signature));
        _httpClient.DefaultRequestHeaders.Add("X-Timestamp", timestamp.ToUniversalTime().ToString("o"));

        var response = await _httpClient.DeleteAsync($"api/profiles/{ss58address}");
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Upload a profile image with Sr25519 authentication
    /// </summary>
    public async Task<string> UploadImageAsync(string ss58address, Stream imageStream, string filename, Account? account = null)
    {
        if (account == null)
            throw new InvalidOperationException("Account is required for image upload");

        // Construct the payload
        var timestamp = DateTime.UtcNow;
        var payload = CryptoHelper.ConstructPayload("POST", $"/api/profiles/{ss58address}/image", new EmptyPayloadBody(), timestamp);

        // Sign the payload using Account
        var signature = await CryptoHelper.SignAsync(payload, account);

        // Create the request content
        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(imageStream), "image", filename);

        // Add authentication headers properly
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-SS58-Address", account.Value);
        _httpClient.DefaultRequestHeaders.Add("X-Signature", Utils.Bytes2HexString(signature));
        _httpClient.DefaultRequestHeaders.Add("X-Timestamp", timestamp.ToUniversalTime().ToString("o"));

        var uri = new Uri($"api/profiles/{ss58address}/image", UriKind.Relative);

        var response = await _httpClient.PostAsync(uri, content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();

        // ASP.NET Core serves bare strings as text/plain; only parse JSON when the
        // server actually sent JSON
        if (response.Content.Headers.ContentType?.MediaType == "application/json")
        {
            return JsonSerializer.Deserialize<string>(responseContent, _jsonOptions) ?? "";
        }

        return responseContent;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
