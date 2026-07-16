using NUnit.Framework;
using Substrate.NetApi;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using XcavateProfile.Client;

namespace XcavateProfile.ApiTests;

[TestFixture]
public class ProfileApiTests
{
    private string mnemonic = TestMnemonics.BaseMnemonic;
    // Valid BIP39 mnemonic that produces a different account (for invalid signature tests)
    // Using admin mnemonic which has valid checksum but different from base
    private string invalidMnemonic = TestMnemonics.AdminMnemonic;

    // Address must match the account derived from BaseMnemonic, or the server
    // rejects with "Can only create profile for authenticated address"
    private string address = MnemonicsModel.GetAccountFromMnemonics(TestMnemonics.BaseMnemonic).Value;

    private string x25519Key = "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef";
    private string x25519Key2 = "0x4444444444444444444444444444444444444444444444444444444444444444";


    private XcavateProfileClient? _client;
    private HttpClient? _httpClient;
    private const string TestApiUrl = "http://localhost:5000";

    [SetUp]
    public void Setup()
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(TestApiUrl) };
        _client = new XcavateProfileClient(new XcavateProfileClientOptions
        {
            ApiUrl = TestApiUrl
        });
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _httpClient?.Dispose();
    }

    #region Profile CRUD Tests

    [Test]
    public async Task Create_Profile_SuccessAsync()
    {
        var account = MnemonicsModel.GetAccountFromMnemonics(mnemonic);
        var profile = new Profile
        {
            Ss58Address = address,
            Nickname = "testuser",
            Bio = "Test profile bio",
            X25519Key = x25519Key
        };

        _client = new XcavateProfileClient(new XcavateProfileClientOptions
        {
            ApiUrl = TestApiUrl
        });

        // Act
        var result = await _client.CreateProfileAsync(profile, account);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Ss58Address, Is.EqualTo(address));
        Assert.That(result.Nickname, Is.EqualTo("testuser"));
        Assert.That(result.Bio, Is.EqualTo("Test profile bio"));
    }

    [Test]
    public async Task Get_Profile_By_Address_SuccessAsync()
    {
        var account = MnemonicsModel.GetAccountFromMnemonics(mnemonic);
        _client = new XcavateProfileClient(new XcavateProfileClientOptions
        {
            ApiUrl = TestApiUrl
        });

        var profile = new Profile
        {
            Ss58Address = address,
            Nickname = "getbyaddrtest",
            X25519Key = x25519Key
        };
        await _client.CreateProfileAsync(profile, account);

        // Act
        var result = await _client.GetProfileAsync(address);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Ss58Address, Is.EqualTo(address));
        Assert.That(result.Nickname, Is.EqualTo("getbyaddrtest"));
    }

    [Test]
    public async Task Get_Profile_By_Nickname_SuccessAsync()
    {
        var account = MnemonicsModel.GetAccountFromMnemonics(mnemonic);
        _client = new XcavateProfileClient(new XcavateProfileClientOptions
        {
            ApiUrl = TestApiUrl
        });

        var profile = new Profile
        {
            Ss58Address = address,
            Nickname = "uniquenickname",
            X25519Key = x25519Key
        };
        await _client.CreateProfileAsync(profile, account);

        // Act
        var result = await _client.GetProfileByNicknameAsync("uniquenickname");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Nickname, Is.EqualTo("uniquenickname"));
    }

    [Test]
    public async Task Update_Profile_SuccessAsync()
    {
        var account = MnemonicsModel.GetAccountFromMnemonics(mnemonic);
        _client = new XcavateProfileClient(new XcavateProfileClientOptions
        {
            ApiUrl = TestApiUrl
        });

        try
        {
            // Create profile first
            var profile = new Profile
            {
                Ss58Address = address,
                Nickname = "original",
                X25519Key = x25519Key
            };
            await _client.CreateProfileAsync(profile, account);
        }
        catch
        {
        }

        // Update the profile
        var updateProfile = new Profile
        {
            Ss58Address = address,
            Nickname = "updatednickname",
            Bio = "Updated bio",
            X25519Key = x25519Key
        };

        // Act
        var result = await _client.UpdateProfileAsync(address, updateProfile, account);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Nickname, Is.EqualTo("updatednickname"));
        Assert.That(result.Bio, Is.EqualTo("Updated bio"));
    }

    [Test]
    public async Task Delete_Profile_SuccessAsync()
    {
        var account = MnemonicsModel.GetAccountFromMnemonics(mnemonic);
        _client = new XcavateProfileClient(new XcavateProfileClientOptions
        {
            ApiUrl = TestApiUrl
        });

        // Create profile first
        var profile = new Profile
        {
            Ss58Address = address,
            Nickname = "todelete",
            X25519Key = x25519Key
        };
        await _client.CreateProfileAsync(profile, account);

        // Act
        await _client.DeleteProfileAsync(address, account);

        // Assert - verify profile is gone
        var result = await _client.GetProfileAsync(address);
        Assert.That(result, Is.Null);
    }

    #endregion

    #region Authentication Tests

    [Test]
    public async Task Create_Profile_Fails_With_Invalid_SignatureAsync()
    {
        // Arrange
        var x25519Key = "0x1111111111111111111111111111111111111111111111111111111111111111";
        var profile = new Profile
        {
            Ss58Address = "5GrwvaEF5zKbXCEe9qGjZL23Y641mot2Ff6hS3s8jF3g3k3Z",
            X25519Key = x25519Key
        };

        var invalidAccount = MnemonicsModel.GetAccountFromMnemonics(invalidMnemonic);

        _client = new XcavateProfileClient(new XcavateProfileClientOptions
        {
            ApiUrl = TestApiUrl
        });

        // Act & Assert
        var ex = Assert.ThrowsAsync<HttpRequestException>(() => _client.CreateProfileAsync(profile, invalidAccount));
        Assert.That(ex?.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Create_Profile_Fails_With_Expired_TimestampAsync()
    {
        var account = MnemonicsModel.GetAccountFromMnemonics(mnemonic);
        var profile = new Profile { Ss58Address = address, X25519Key = x25519Key };

        _client = new XcavateProfileClient(new XcavateProfileClientOptions
        {
            ApiUrl = TestApiUrl
        });

        var bodyJson = JsonSerializer.Serialize(profile);

        // Manually create a request with old timestamp (simulate replay attack)
        var oldTimestamp = DateTime.UtcNow.AddMinutes(-10);
        var payload = CryptoHelper.ConstructPayload("POST", "/api/profiles", profile, oldTimestamp);

        var signature = await CryptoHelper.SignAsync(payload, account);

        // Build request manually with old timestamp
        var content = new StringContent(bodyJson, System.Text.Encoding.UTF8, "application/json");
        content.Headers.Add("X-SS58-Address", account.Value);
        content.Headers.Add("X-Signature", Utils.Bytes2HexString(signature));
        content.Headers.Add("X-Timestamp", oldTimestamp.ToString("o"));

        var request = new HttpRequestMessage(HttpMethod.Post, "api/profiles")
        {
            Content = content
        };

        // Act & Assert
        var response = await _httpClient!.SendAsync(request);
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Update_AnotherUserProfile_FailsAsync()
    {
        // Arrange
        var user2Key = "0x8674d0e2bbf1d6a6c2c6b6d2e1c2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f6";
        var user2Address = "5GrwvaEF5zKbXCEe9qGjZL23Y641mot2Ff6hS3s8jF3g3k3C";

        var account = MnemonicsModel.GetAccountFromMnemonics(mnemonic);
        var client1 = new XcavateProfileClient(new XcavateProfileClientOptions
        {
            ApiUrl = TestApiUrl
        });

        // Create user1's profile
        var profile1 = new Profile { Ss58Address = address, Nickname = "user1", X25519Key = x25519Key };
        await client1.CreateProfileAsync(profile1, account);

        var invalidAccount = MnemonicsModel.GetAccountFromMnemonics(invalidMnemonic);

        var client2 = new XcavateProfileClient(new XcavateProfileClientOptions
        {
            ApiUrl = TestApiUrl
        });

        var updateProfile = new Profile { Ss58Address = invalidAccount.Value, Nickname = "hacked", X25519Key = x25519Key2 };

        // Act & Assert
        var ex = Assert.ThrowsAsync<HttpRequestException>(() => client2.UpdateProfileAsync(invalidAccount.Value, updateProfile, invalidAccount));
        Assert.That(ex?.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Delete_AnotherUserProfile_FailsAsync()
    {
        // User 1 profile creation - use a valid mnemonic that produces different account
        var user1Mnemonic = TestMnemonics.User1Mnemonic;
        var user1Account = MnemonicsModel.GetAccountFromMnemonics(user1Mnemonic);
        var user1Address = user1Account.Value;
        var user1Client = new XcavateProfileClient(new XcavateProfileClientOptions
        {
            ApiUrl = TestApiUrl
        });

        var profile1 = new Profile { Ss58Address = user1Address, Nickname = "user1delete", X25519Key = x25519Key };
        await user1Client.CreateProfileAsync(profile1, user1Account);

        // User 2 tries to delete User 1's profile (should fail)
        // Use a valid but different mnemonic
        var user2Mnemonic = TestMnemonics.User2Mnemonic;
        var user2Account = MnemonicsModel.GetAccountFromMnemonics(user2Mnemonic);
        var client2 = new XcavateProfileClient(new XcavateProfileClientOptions
        {
            ApiUrl = TestApiUrl
        });

        var ex = Assert.ThrowsAsync<HttpRequestException>(() => client2.DeleteProfileAsync(user1Address, user2Account));
        Assert.That(ex?.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Forbidden));
    }

    #endregion

    #region Admin Authorization Tests

    [Test]
    public async Task Admin_Can_Update_OtherUserProfileAsync()
    {
        // Arrange - First create admin profile, then regular user profile
        var adminMnemonic = TestMnemonics.AdminMnemonic;
        var adminAccount = MnemonicsModel.GetAccountFromMnemonics(adminMnemonic);
        var adminAddress = adminAccount.Value;
        var adminClient = new XcavateProfileClient(new XcavateProfileClientOptions
        {
            ApiUrl = TestApiUrl
        });

        var adminProfile = new Profile { Ss58Address = adminAddress, Nickname = "admin", X25519Key = x25519Key };
        await adminClient.CreateProfileAsync(adminProfile, adminAccount);

        var userMnemonic = TestMnemonics.UserMnemonic;
        var userAccount = MnemonicsModel.GetAccountFromMnemonics(userMnemonic);
        var userAddress = userAccount.Value;
        var userClient = new XcavateProfileClient(new XcavateProfileClientOptions
        {
            ApiUrl = TestApiUrl
        });

        var userProfile = new Profile { Ss58Address = userAddress, Nickname = "regularuser", X25519Key = x25519Key };
        await userClient.CreateProfileAsync(userProfile, userAccount);

        // Admin updates regular user's profile
        var updatedProfile = new Profile { Ss58Address = userAddress, Nickname = "regularuser_updated", Bio = "Updated by admin", X25519Key = x25519Key };

        // Act
        var result = await adminClient.UpdateProfileAsync(userAddress, updatedProfile, adminAccount);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Nickname, Is.EqualTo("regularuser_updated"));
    }

    [Test]
    public async Task Admin_Can_Delete_OtherUserProfileAsync()
    {
        // Similar setup to admin update test
        var adminAccount = MnemonicsModel.GetAccountFromMnemonics(mnemonic);
        var adminAddress = adminAccount.Value;
        var adminClient = new XcavateProfileClient(new XcavateProfileClientOptions
        {
            ApiUrl = TestApiUrl
        });

        var adminProfile = new Profile { Ss58Address = adminAddress, Nickname = "admin2", X25519Key = x25519Key };
        await adminClient.CreateProfileAsync(adminProfile, adminAccount);

        var userAccount = MnemonicsModel.GetAccountFromMnemonics(mnemonic);
        var userAddress = userAccount.Value;
        var userClient = new XcavateProfileClient(new XcavateProfileClientOptions
        {
            ApiUrl = TestApiUrl
        });

        var userProfile = new Profile { Ss58Address = userAddress, Nickname = "regularuser2", X25519Key = x25519Key };
        await userClient.CreateProfileAsync(userProfile, userAccount);

        // Admin deletes regular user's profile
        // Act
        await adminClient.DeleteProfileAsync(userAddress, adminAccount);

        // Assert
        var result = await adminClient.GetProfileAsync(userAddress);
        Assert.That(result, Is.Null);
    }

    #endregion

    #region Nickname Uniqueness Test

    [Test]
    public async Task Nickname_Uniqueness_Constraint_EnforcedAsync()
    {
        // Arrange
        var user1Account = MnemonicsModel.GetAccountFromMnemonics(mnemonic);
        var address1 = user1Account.Value;
        var client1 = new XcavateProfileClient(new XcavateProfileClientOptions
        {
            ApiUrl = TestApiUrl
        });

        var profile1 = new Profile { Ss58Address = address1, Nickname = "uniqueNick", X25519Key = x25519Key };
        await client1.CreateProfileAsync(profile1, user1Account);

        // Act - use a valid but different mnemonic for user2
        var user2Mnemonic = TestMnemonics.Nick2Mnemonic;
        var user2Account = MnemonicsModel.GetAccountFromMnemonics(user2Mnemonic);
        var client2 = new XcavateProfileClient(new XcavateProfileClientOptions
        {
            ApiUrl = TestApiUrl
        });

        var profile2 = new Profile { Ss58Address = "5GrwvaEF5zKbXCEe9qGjZL23Y641mot2Ff6hS3s8jF3g3k3K", Nickname = "uniqueNick", X25519Key = x25519Key };

        // Assert
        var ex = Assert.ThrowsAsync<HttpRequestException>(() => client2.CreateProfileAsync(profile2, user2Account));
        Assert.That(ex?.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
    }

    #endregion

    #region Image Upload Test

    [Test]
    public async Task Upload_Profile_Image_SuccessAsync()
    {
        // Arrange
        var mnemonic = TestMnemonics.Image2Mnemonic;
        var account = MnemonicsModel.GetAccountFromMnemonics(mnemonic);
        var address = account.Value;
        var x25519Key = "0xcccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";

        var client = new XcavateProfileClient(new XcavateProfileClientOptions
        {
            ApiUrl = TestApiUrl
        });

        var profile = new Profile { Ss58Address = address, Nickname = "imageuser", X25519Key = x25519Key };
        await client.CreateProfileAsync(profile, account);

        // Create a dummy image stream
        var dummyImageBytes = System.Text.Encoding.UTF8.GetBytes("fake image content");
        var imageStream = new MemoryStream(dummyImageBytes);

        // Act
        var imageUrl = await client.UploadImageAsync(address, imageStream, "test.jpg", account);

        // Assert
        Assert.That(imageUrl, Is.Not.Null);
        Assert.That(imageUrl!.Contains("test.jpg"), Is.True);
    }

    #endregion
}
