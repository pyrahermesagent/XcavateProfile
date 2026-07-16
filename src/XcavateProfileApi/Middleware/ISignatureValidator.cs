using XcavateProfileApiClient;

namespace XcavateProfileApi.Middleware;

public interface ISignatureValidator
{
    Task<SignatureValidationResult> ValidateAsync(
        string address,
        string signatureHex,
        string timestamp,
        string method,
        string path,
        IPayloadBody payloadBody);

    bool IsAdmin(string address);
}
