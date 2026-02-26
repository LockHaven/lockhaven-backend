# 🔒 LockHaven - Secure File Storage Backend API

A secure ASP.NET Core Web API for authentication, encrypted file storage, and protected file access using JWT, ~~Azure Blob Storage, Azure SQL, and Azure Key Vault~~.

## 🚀 Features

- **Secure Authentication**: JWT-based auth with register, login, and protected profile endpoints
- **Encrypted File Storage**: Server-side AES-256-GCM chunked encryption for uploaded files
- **Envelope Encryption**: ~~Per-file keys and IVs encrypted with Azure Key Vault (KEK)~~
- **Cloud Storage Ready**: ~~Azure Blob Storage integration for file persistence~~
- **Health & Reliability**: Liveness/readiness endpoints and global exception handling middleware
- **API Docs**: Swagger/OpenAPI support with JWT bearer auth (feature-flagged)

## 🛠️ Tech Stack

- **Backend Framework**: ASP.NET Core Web API (.NET 9) (TODO: Upgrarde to .NET 10)
- **Language**: C#
- **Authentication**: JWT Bearer + BCrypt password hashing
- **Database**: ~~Azure SQL via Entity Framework Core 9~~ (NOTE: Moving to DigitalOcean)
- **File Storage**: ~~Azure Blob Storage~~
- **Key Management**: ~~Azure Key Vault (envelope encryption)~~
- **API Documentation**: Swagger / Swashbuckle

## 📦 Getting Started

### Prerequisites

- .NET 10 SDK
- ~~Azure SQL connection string~~
- ~~Azure Blob Storage connection string (or Azurite for local development)~~
- ~~Azure Key Vault access~~
- ~~Azure CLI login (for `DefaultAzureCredential` in local dev)~~

### Installation

1. Clone the repository:
```bash
git clone <your-repo-url>
cd lockhaven-backend
```

2. Restore dependencies:
```bash
dotnet restore
```

3. Configure development settings:

- Update `appsettings.Development.json` (or environment variables) for:
  - `ConnectionStrings:Postgres`
  - `BlobStorage:ConnectionString`
  - `BlobStorage:ContainerName`
  - `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`
  - `KeyEncryption:Provider`
  - `Vault:Address`, `Vault:TransitKeyName`, `Vault:Token`
  - `Features:EnableSwagger` (true/false)

4. Apply database migrations:
```bash
dotnet ef database update
```

5. Run the API:
```bash
dotnet run
```

6. Open the API/Swagger URL shown in terminal output (Swagger appears at root when enabled).

## 🎨 Project Structure

```text
lockhaven-backend/
├── Controllers/
│   ├── AuthController.cs
│   ├── FileController.cs
│   └── HealthController.cs
├── Services/
│   ├── Implementations/
│   │   ├── AuthService.cs
│   │   ├── FileService.cs
│   │   ├── BlobStorageService.cs
│   │   ├── VaultTransitKeyEncryptionService.cs
│   │   └── JwtService.cs
│   └── Interfaces/
├── Data/
│   ├── ApplicationDbContext.cs
│   └── Migrations/
├── Infrastructure/
│   ├── Extensions/
│   ├── Configuration/
│   └── Health/
├── Middleware/
├── Models/
├── Program.cs
└── lockhaven-backend.csproj
```

## 🔧 Available Commands

- `dotnet restore` - Restore NuGet packages
- `dotnet build` - Build the project
- `dotnet run` - Start the API
- `dotnet ef database update` - Apply migrations to database

## 📡 API Endpoints (Current)

- `POST /api/auth/register` - Register a new user
- `POST /api/auth/login` - Login and receive JWT
- `GET /api/auth/profile` - Get authenticated user profile
- `POST /api/file/upload` - Upload and encrypt file
- `GET /api/file/download/{fileId}` - Download and decrypt file
- `GET /api/file/list` - List authenticated user's files
- `DELETE /api/file/{fileId}` - Delete a file
- `GET /api/file/storage` - Get user's storage usage
- `GET /api/health` - API diagnostic health check
- `GET /health/live` - Liveness probe
- `GET /health/ready` - Readiness probe
- `GET /healthz` - Detailed health alias

## 🎯 Current Status

- ✅ JWT authentication flow (register/login/profile)
- ✅ User and file domain models with EF Core migration
- ✅ File upload/download/list/delete/storage APIs
- ✅ Server-side AES-256-GCM chunked encryption
- ✅ Envelope encryption with HashiCorp Vault Transit for DEK/IV protection
- ✅ ~~Azure Blob + Azure SQL persistence pipeline~~ (NOTE: To be removed and migrated to DigitalOcean)
- ✅ Global exception middleware with structured problem responses
- ✅ Health endpoints for diagnostics/readiness/liveness
- ✅ Swagger integration with JWT support (feature toggle)
- 🔄 Client-side encryption path (`IsClientEncrypted`) not yet active
- 🔄 Refresh token endpoint/rotation flow not yet implemented
- 🔄 File sharing/group-based access not yet implemented
- 🔄 Automated tests and CI pipeline setup in progress

## 🔐 Security Features

- **AES-256-GCM Encryption**: Files encrypted before storage using per-file data keys
- **Envelope Encryption**: Data keys and IVs encrypted with a Vault Transit KEK
- **JWT Access Control**: Protected routes enforce authenticated user access
- **Ownership Enforcement**: File queries and operations scoped to token user ID
- **Password Security**: BCrypt hashing for stored user credentials
- **Secure Error Handling**: Centralized exception middleware with consistent API errors

## 🚀 Deployment

This backend is designed to run on:

- Azure App Service / Azure Container Apps
- Docker-compatible hosts
- Any platform supporting .NET 9

Make sure production environment variables are configured for SQL, Blob Storage, JWT, and Vault Transit.

## 📝 License

This project is licensed under the MIT License - see the `LICENSE` file for details.

## 🤝 Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

Built with ❤️ using ASP.NET Core, Azure, and C#