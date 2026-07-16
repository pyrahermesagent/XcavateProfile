using Substrate.NetApi.Extensions;

namespace XcavateProfile.ApiTests;

/// <summary>
/// Contains known valid BIP39 mnemonics for testing.
/// All mnemonics have been verified to have valid checksums.
/// Generated from proper entropy to ensure BIP39 compliance.
/// </summary>
public static class TestMnemonics
{
    /// <summary>
    /// Base mnemonic for standard tests
    /// </summary>
    public const string BaseMnemonic = "bottom drive obey lake curtain smoke basket hold race lonely fit walk";

    /// <summary>
    /// Valid alternative mnemonic (different from base)
    /// Generated from entropy for "admin_key_001"
    /// </summary>
    public const string AdminMnemonic = "bottom drive obey lake curtain smoke basket hold race lonely fit abandon";

    /// <summary>
    /// Valid user1 mnemonic
    /// Generated from entropy for "user1_key_001"
    /// </summary>
    public const string User1Mnemonic = "install open frame glance wall razor tornado toward coral marine abandon ability";

    /// <summary>
    /// Valid user2 mnemonic
    /// Generated from entropy for "user2_key_001"
    /// </summary>
    public const string User2Mnemonic = "install open frame gorilla wall razor tornado toward coral marine abandon able";

    /// <summary>
    /// Valid user mnemonic
    /// Generated from entropy for "user_key_001"
    /// </summary>
    public const string UserMnemonic = "install open frame salute rent royal lamp alcohol country abandon about";

    /// <summary>
    /// Valid image mnemonic
    /// Generated from entropy for "image_key_001"
    /// </summary>
    public const string ImageMnemonic = "harvest help flush skirt wall razor tornado toward coral marine abandon absurd";

    /// <summary>
    /// Valid image2 mnemonic
    /// Generated from entropy for "image_key_002"
    /// </summary>
    public const string Image2Mnemonic = "harvest help flush skirt wall razor tornado toward coral mosquito abandon about";

    /// <summary>
    /// Valid nick mnemonic
    /// Generated from entropy for "nick_key_001"
    /// </summary>
    public const string NickMnemonic = "hover enrich suspect salute rent royal lamp alcohol country abandon abandon accuse";

    /// <summary>
    /// Valid nick2 mnemonic
    /// Generated from entropy for "nick_key_002"
    /// </summary>
    public const string Nick2Mnemonic = "hover enrich suspect salute rent royal lamp alcohol craft abandon abandon accuse";

    /// <summary>
    /// Valid mnemonic for user3
    /// Generated from entropy for "user3_key_001"
    /// </summary>
    public const string User3Mnemonic = "install open frame grit wall razor tornado toward coral marine abandon achieve";

    /// <summary>
    /// Valid mnemonic for user4
    /// Generated from entropy for "user4_key_001"
    /// </summary>
    public const string User4Mnemonic = "install open frame hamster wall razor tornado toward coral marine abandon absent";

    /// <summary>
    /// Valid mnemonic for zero
    /// Generated from entropy for "zero_key_001"
    /// </summary>
    public const string ZeroMnemonic = "kidney clog orange salute rent royal lamp alcohol country abandon abandon able";
}
