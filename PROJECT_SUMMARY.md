# XcavateProfile - Project Summary

## Overview

This document provides a comprehensive summary of the XcavateProfile project - a Substrate/Polkadot profile registration and management system.

## Project Structure

```
XcavateProfile/
├── src/
│   ├── XcavateProfileApi/          # ASP.NET Core Web API
│   │   ├── Controllers/
│   │   │   └── ProfilesController.cs
│   │   ├── Data/
│   │   │   ├── Profile.cs
│   │   │   ├── ProfileDbContext.cs
│   │   │   └── ModelBuilderExtensions.cs
│   │   ├── Middleware/
│   │   │   ├── SignatureValidator.cs
│   │   │   ├── ISignatureValidator.cs
│   │   │   └── SignatureValidationOptions.cs
│   │   ├── Services/
│   │   │   └── S3Service.cs
│   │   ├── Models/
│   │   │   └── profile.schema.json
│   │   ├── Program.cs
│   │   ├── CryptoExtensions.cs
│   │   └── XcavateProfileApi.csproj
│   │
│   └── XcavateProfileApiClient/    # C# SDK Client Library
│       ├── CryptoHelper.cs
│       ├── Profile.cs
│       ├── XcavateProfileApiClient.cs
│       ├── XcavateProfileApiClientOptions.cs
│       └── XcavateProfileApiClient.csproj
│
├── tests/
│   └── XcavateProfile.ApiTests/    # NUnit E2E Tests
│       ├── ProfileApiTests.cs
│       ├── Tests.cs
│       └── XcavateProfile.ApiTests.csproj
│
├── .github/workflows/
│   ├── deploy.yml    # GitHub Actions for Hetzner deployment
│   └── nuget.yml     # GitHub Actions for NuGet publishing
│
├── .env.example                      # Environment variables template
├── docker-compose.yml               # Docker Compose configuration
├── Dockerfile                       # Multi-stage Docker build
├── run_e2e_tests.sh                 # Test orchestration script
├── README.md                         # Main documentation
├── ADMIN_AUTH.md                     # Authentication documentation
├── XcavateProfile.sln               # Visual Studio solution file
└── PROJECT_SUMMARY.md               # This file
```

## Core Components

### 1. ASP.NET Core Web API (XcavateProfileApi)

**Framework**: .NET 8.0
**Database**: PostgreSQL with Entity Framework Core
**Auth**: Sr25519 signature verification (Substrate-compatible)
**Storage**: Hetzner Object Storage (S3-compatible)

**Endpoints**:
- `GET /api/profiles` - List all profiles
- `GET /api/profiles/{ss58address}` - Get profile by address
- `GET /api/profiles/nickname/{nickname}` - Get profile by nickname
- `POST /api/profiles` - Create profile (requires auth)
- `PUT /api/profiles/{ss58address}` - Update profile (requires auth)
- `DELETE /api/profiles/{ss58address}` - Delete profile (requires auth)
- `POST /api/profiles/{ss58address}/image` - Upload profile image (requires auth)

### 2. C# Client SDK (XcavateProfileApiClient)

**Features**:
- Automatic Sr25519 signature generation
- Blake2 body hash computation
- X-* HTTP header injection
- Async/await pattern throughout

**Usage**:
```csharp
var client = new XcavateProfileApiClient(new XcavateProfileApiClientOptions
{
    ApiUrl = "http://localhost:5000",
    SecretKey = secretKey,
    SS58Address = address
});

// Create, read, update, delete profiles with automatic authentication
var profile = await client.CreateProfileAsync(profileData, secretKey);
```

### 3. Authentication System

**Authentication Headers**:
- `X-SS58-Address`: Substrate SS58 address
- `X-Signature`: Hex-encoded Sr25519 signature
- `X-Timestamp`: ISO 8601 UTC timestamp

**Signed Payload**: `<method>:<path>:<body_hash>:<timestamp>`

**Security Features**:
- Replay attack prevention (5-minute timestamp window)
- Signature verification on all state-changing requests
- Profile ownership validation
- Admin override for privileged users

### 4. Database Schema

**Profile Entity**:
```csharp
public class Profile
{
    public string Ss58Address { get; set; } = string.Empty;  // PK, Required
    public string? Nickname { get; set; }                     // Unique, Optional
    public string? Bio { get; set; }                          // Optional
    public string? ProfilePicture { get; set; }              // Optional
    public string? X25519Key { get; set; }                   // Optional
}
```

## Technology Stack

| Component | Technology |
|-----------|------------|
| Backend Framework | ASP.NET Core 8.0 |
| ORM | Entity Framework Core 8.0 |
| Database | PostgreSQL 15 |
| Cryptography | Substrate.NET.API 7.1.0 |
| Object Storage | AWSSDK.S3 (Hetzner S3) |
| Testing | NUnit 4.0.1 |
| CI/CD | GitHub Actions |
| Containerization | Docker & Docker Compose |

## Authentication Flow

```
Client:
1. Generate keypair (ss58address, secretKey)
2. Construct request body
3. Compute Blake2 hash of body
4. Build payload: <method>:<path>:<hash>:<timestamp>
5. Sign payload with Sr25519
6. Send request with X-* headers

Server:
1. Receive request with headers
2. Parse X-SS58-Address, X-Signature, X-Timestamp
3. Compute Blake2 hash of body
4. Reconstruct payload string
5. Look up profile by SS58 address
6. Get public key from profile
7. Verify signature using public key
8. Check timestamp (within 5 min)
9. Check authorization (owner or admin)
10. Process request
```

## CI/CD Pipelines

### Deployment Pipeline (.github/workflows/deploy.yml)

**Triggers**: Push to main/master, Release published

**Steps**:
1. Checkout code
2. Set up Docker Buildx
3. Log in to GitHub Container Registry
4. Build and push Docker image
5. SSH to Hetzner server
6. Update repository
7. Generate .env from GitHub secrets
8. Run Docker Compose
9. Execute database migrations
10. Health check verification

**Required Secrets**:
- `HETZNER_HOST`, `HETZNER_USER`, `HETZNER_SSH_KEY`
- `HETZNER_DEPLOY_DIR`
- `HETZNER_POSTGRES_*` variables
- `HETZNER_S3_*` variables
- `HETZNER_ADMIN_ADDRESSES`

### NuGet Publishing Pipeline (.github/workflows/nuget.yml)

**Triggers**: Push to main/master, Release published

**Steps**:
1. Checkout code
2. Setup .NET
3. Get package version (git tag or build number)
4. Build package
5. Push to NuGet
6. Upload artifact

**Version Strategy**:
- Git releases: Use tag version
- Develop builds: Use `1.0.<build_number>`

**Required Secrets**:
- `NUGET_API_KEY`

## Test Coverage (E2E Tests)

**Location**: `tests/XcavateProfile.ApiTests/ProfileApiTests.cs`

**Test Categories**:
1. **Profile CRUD Tests**
   - Create profile
   - Get profile by address
   - Get profile by nickname
   - Update profile
   - Delete profile

2. **Authentication Tests**
   - Invalid signature rejection
   - Expired timestamp (replay attack) rejection
   - Unauthorized cross-profile update
   - Unauthorized cross-profile delete

3. **Admin Authorization Tests**
   - Admin can update other users' profiles
   - Admin can delete other users' profiles

4. **Nickname Uniqueness Test**
   - Constraint enforcement

5. **Image Upload Test**
   - Successful image upload
   - Profile picture URL update

## Running the Project

### Prerequisites
- .NET 10.0 SDK
- Docker & Docker Compose
- PostgreSQL (or use Docker)

### Local Development

```bash
# Clone repository
cd XcavateProfile

# Copy environment template
cp .env.example .env

# Edit .env with your settings

# Start with Docker Compose
docker-compose up -d

# Run migrations
dotnet ef database update --project src/XcavateProfileApi

# Run tests
./run_e2e_tests.sh
```

### Using the Client SDK

```csharp
using XcavateProfileApiClient;

var client = new XcavateProfileApiClient(new XcavateProfileApiClientOptions
{
    ApiUrl = "http://localhost:5000",
    SecretKey = "your-secret-key",
    SS58Address = "your-ss58-address"
});

// Create profile
var profile = new Profile {
    Ss58Address = "5GrwvaEF...",
    Nickname = "myprofile"
};
await client.CreateProfileAsync(profile, "your-secret-key");

// Get profile
var retrieved = await client.GetProfileAsync("5GrwvaEF...");

// Update profile
profile.Bio = "Updated bio";
await client.UpdateProfileAsync("5GrwvaEF...", profile, "your-secret-key");
```

## Key Design Decisions

1. **Signature-based Auth over JWT**: Aligns with Substrate ecosystem, no token refresh needed

2. **Blake2 Hashing**: Native Substrate hash function ensures cryptographic compatibility

3. **Environment-based Admin List**: Simple admin management without database migrations

4. **Async First**: All API operations use async/await for scalability

5. **Hetzner S3**: Cost-effective object storage that's S3-compatible

6. **Docker Compose**: Simple local development and deployment setup

## Security Considerations

- All state-changing requests require Sr25519 signature verification
- Timestamp validation prevents replay attacks (5-minute window)
- Admin addresses stored in environment variables, not database
- Hetzner credentials stored as environment variables or GitHub secrets
- Profile ownership enforced for regular users
- Admin users can bypass ownership checks

## Future Enhancements

- Rate limiting middleware
- Redis caching for profile lookups
- Image resizing/optimization pipeline
- Profile verification badges
- NFT-based profile ownership verification
- Cross-chain address support (multiple ss58 formats)

## Troubleshooting

### Common Issues

1. **Database connection errors**
   - Check PostgreSQL container is running
   - Verify connection string in .env
   - Ensure network between api and postgres containers

2. **Signature verification failures**
   - Verify timestamp is current
   - Check exact same body hash computation
   - Ensure SS58 address matches keypair

3. **S3 upload errors**
   - Verify S3_ENDPOINT, S3_ACCESS_KEY, S3_SECRET_KEY
   - Ensure bucket exists
   - Check firewall allows outbound connections

## Support

For issues and questions:
- Check README.md for setup instructions
- Check ADMIN_AUTH.md for authentication details
- Review E2E tests for usage examples
- Check GitHub Issues for known problems

## License

MIT License - See LICENSE file for details

## Acknowledgments

- Built with [Substrate.NET.API](https://github.com/paritytech/parity-substrate-dotnet)
- Uses Entity Framework Core for data persistence
- Deployed on Hetzner Cloud infrastructure
