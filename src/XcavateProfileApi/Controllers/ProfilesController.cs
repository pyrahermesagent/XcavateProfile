using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Substrate.NetApi;
using XcavateProfile.Client;
using XcavateProfileApi.Data;
using XcavateProfileApi.Middleware;
using XcavateProfileApi.Services;
using XcavateProfileApi.Swagger;
using XcavateProfileApiClient;

namespace XcavateProfileApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfilesController : ControllerBase
{
    private readonly ProfileDbContext _context;
    private readonly ISignatureValidator _signatureValidator;
    private readonly IS3Service _s3Service;

    public ProfilesController(
        ProfileDbContext context,
        ISignatureValidator signatureValidator,
        IS3Service s3Service)
    {
        _context = context;
        _signatureValidator = signatureValidator;
        _s3Service = s3Service;
    }

    // GET: api/profiles
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Profile>>> GetProfilesAsync()
    {
        var profiles = await _context.Profiles.ToListAsync();
        return Ok(profiles);
    }

    // GET: api/profiles/5GrwvaEF5zKbXCEe9qGjZL23Y641mot2Ff6hS3s8jF3g3k3W
    [HttpGet("{ss58address}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Profile>> GetProfileAsync(string ss58address)
    {
        var profile = await _context.Profiles.FindAsync(ss58address);
        if (profile == null)
        {
            return NotFound();
        }
        return Ok(profile);
    }

    // GET: api/profiles/nickname/xena
    [HttpGet("nickname/{nickname}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Profile>> GetProfileByNicknameAsync(string nickname)
    {
        var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.Nickname == nickname);
        if (profile == null)
        {
            return NotFound();
        }
        return Ok(profile);
    }

    // POST: api/profiles
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Profile>> CreateProfileAsync([FromBody] Profile profile)
    {
        // Verify authentication headers from request
        var address = Request.Headers["X-SS58-Address"].FirstOrDefault();
        var signature = Request.Headers["X-Signature"].FirstOrDefault();
        var timestamp = Request.Headers["X-Timestamp"].FirstOrDefault();

        if (string.IsNullOrEmpty(address) || string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(timestamp))
        {
            return Unauthorized("Missing authentication headers");
        }

        // Validate signature
        var result = await _signatureValidator.ValidateAsync(
            address,
            signature,
            timestamp,
            "POST",
            "/api/profiles",
            profile);

        if (!result.IsValid)
        {
            return Unauthorized(result.Error);
        }

        // Check if the user is trying to create a profile for someone else
        if (!string.IsNullOrEmpty(address) && address != profile.Ss58Address)
        {
            return Unauthorized("Can only create profile for authenticated address");
        }

        // Check if profile already exists
        if (await _context.Profiles.FindAsync(profile.Ss58Address) != null)
        {
            return BadRequest("Profile already exists");
        }

        // Check nickname uniqueness
        if (!string.IsNullOrEmpty(profile.Nickname) &&
            await _context.Profiles.AnyAsync(p => p.Nickname == profile.Nickname))
        {
            return BadRequest("Nickname already exists");
        }

        _context.Profiles.Add(profile);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProfileAsync), new { ss58address = profile.Ss58Address }, profile);
    }

    // PUT: api/profiles/5GrwvaEF5zKbXCEe9qGjZL23Y641mot2Ff6hS3s8jF3g3k3W
    [HttpPut("{ss58address}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Profile>> UpdateProfileAsync(string ss58address, [FromBody] Profile profile)
    {
        // Verify authentication headers
        var address = Request.Headers["X-SS58-Address"].FirstOrDefault();
        var signature = Request.Headers["X-Signature"].FirstOrDefault();
        var timestamp = Request.Headers["X-Timestamp"].FirstOrDefault();

        if (string.IsNullOrEmpty(address) || string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(timestamp))
        {
            return Unauthorized("Missing authentication headers");
        }

        // Validate signature
        var result = await _signatureValidator.ValidateAsync(
            address,
            signature,
            timestamp,
            "PUT",
            $"/api/profiles/{ss58address}",
            profile);

        if (!result.IsValid)
        {
            return Unauthorized(result.Error);
        }

        // Check authorization: can only update own profile or is admin
        if (address != ss58address && !_signatureValidator.IsAdmin(address))
        {
            return Forbid("You can only update your own profile");
        }

        // Check if profile exists
        var existingProfile = await _context.Profiles.FindAsync(ss58address);
        if (existingProfile == null)
        {
            return NotFound();
        }

        // Check nickname uniqueness if nickname is being changed
        if (!string.IsNullOrEmpty(profile.Nickname) && profile.Nickname != existingProfile.Nickname)
        {
            if (await _context.Profiles.AnyAsync(p => p.Nickname == profile.Nickname && p.Ss58Address != ss58address, CancellationToken.None))
            {
                return BadRequest("Nickname already exists");
            }
        }

        // Update profile properties
        existingProfile.Nickname = profile.Nickname;
        existingProfile.Bio = profile.Bio;
        existingProfile.ProfilePicture = profile.ProfilePicture;
        existingProfile.X25519Key = profile.X25519Key;

        await _context.SaveChangesAsync();
        return Ok(existingProfile);
    }

    // DELETE: api/profiles/5GrwvaEF5zKbXCEe9qGjZL23Y641mot2Ff6hS3s8jF3g3k3W
    [HttpDelete("{ss58address}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteProfileAsync(string ss58address)
    {
        // Verify authentication headers
        var address = Request.Headers["X-SS58-Address"].FirstOrDefault();
        var signature = Request.Headers["X-Signature"].FirstOrDefault();
        var timestamp = Request.Headers["X-Timestamp"].FirstOrDefault();

        if (string.IsNullOrEmpty(address) || string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(timestamp))
        {
            return Unauthorized("Missing authentication headers");
        }

        // Validate signature
        var result = await _signatureValidator.ValidateAsync(
            address,
            signature,
            timestamp,
            "DELETE",
            $"/api/profiles/{ss58address}",
            new EmptyPayloadBody());

        if (!result.IsValid)
        {
            return Unauthorized(result.Error);
        }

        // Check authorization: only admin or profile owner can delete
        if (address != ss58address && !_signatureValidator.IsAdmin(address))
        {
            return Forbid("You can only delete your own profile");
        }

        var profile = await _context.Profiles.FindAsync(ss58address);
        if (profile == null)
        {
            return NotFound();
        }

        _context.Profiles.Remove(profile);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // POST: api/profiles/5GrwvaEF5zKbXCEe9qGjZL23Y641mot2Ff6hS3s8jF3g3k3W/image
    // NOTE: This endpoint is excluded from Swagger due to issues with IFormFile model binding
    [SwaggerExclude]
    [HttpPost("{ss58address}/image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> UploadImageAsync(string ss58address, [FromForm] IFormFile image)
    {
        // Verify authentication headers
        var address = Request.Headers["X-SS58-Address"].FirstOrDefault();
        var signature = Request.Headers["X-Signature"].FirstOrDefault();
        var timestamp = Request.Headers["X-Timestamp"].FirstOrDefault();

        if (string.IsNullOrEmpty(address) || string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(timestamp))
        {
            return Unauthorized("Missing authentication headers");
        }

        // Compute body hash using Blake2 (for multipart/form-data, same as client - empty body)
        // The client computes hash from empty string for multipart uploads
        var bodyJson = string.Empty;
        var bodyHashHex = ComputeBodyHash(bodyJson);

        // Validate signature
        var result = await _signatureValidator.ValidateAsync(
            address,
            signature,
            timestamp,
            "POST",
            $"/api/profiles/{ss58address}/image",
            new EmptyPayloadBody());

        if (!result.IsValid)
        {
            return Unauthorized(result.Error);
        }

        // Check authorization: can only upload image for own profile or is admin
        if (address != ss58address && !_signatureValidator.IsAdmin(address))
        {
            return Forbid("You can only upload image for your own profile");
        }

        // Check if profile exists
        var profile = await _context.Profiles.FindAsync(ss58address);
        if (profile == null)
        {
            return NotFound();
        }

        // Upload to S3
        if (image.Length > 0)
        {
            using (var stream = image.OpenReadStream())
            {
                var key = $"profiles/{ss58address}/{Guid.NewGuid()}_{image.FileName}";
                var url = await _s3Service.UploadImageAsync("xcavate-profiles", key, stream, image.ContentType);

                // Update profile picture URL
                profile.ProfilePicture = url;
                await _context.SaveChangesAsync();

                return Ok(url);
            }
        }

        return BadRequest("No image file provided");
    }

    private static string ComputeBodyHash(string body)
    {
        var hash = CryptoHelper.Hash(body);
        return Utils.Bytes2HexString(hash);
    }
}
