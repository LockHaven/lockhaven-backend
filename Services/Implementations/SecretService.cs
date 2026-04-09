using System.Data;
using System.Security.Cryptography;
using System.Text;
using lockhaven_backend.Constants;
using lockhaven_backend.Data;
using lockhaven_backend.Exceptions;
using lockhaven_backend.Models;
using lockhaven_backend.Models.Requests;
using lockhaven_backend.Models.Responses;
using lockhaven_backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace lockhaven_backend.Services;

public class SecretService : ISecretService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IKeyEncryptionService _keyEncryptionService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SecretService(
        ApplicationDbContext dbContext,
        IKeyEncryptionService keyEncryptionService,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _keyEncryptionService = keyEncryptionService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<UpsertSecretResponse> UpsertSecretAsync(
        Guid projectId,
        Guid environmentId,
        string key,
        UpsertSecretRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedKey = NormalizeSecretKey(key);
        if (string.IsNullOrEmpty(normalizedKey))
        {
            throw new BadHttpRequestException("Secret key is required");
        }

        var (_, environment, role) = await GetAuthorizedEnvironmentAsync(projectId, environmentId, userId, cancellationToken);
        EnsureCanWriteSecrets(role);

        var newPlaintext = request.Value ?? string.Empty;
        var newPayloadHash = ComputePayloadHash(newPlaintext);

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var secret = await _dbContext.Secrets
            .FirstOrDefaultAsync(
                s => s.EnvironmentId == environment.Id && s.Key == normalizedKey && !s.IsDeleted,
                cancellationToken);

        var now = DateTime.UtcNow;

        if (secret == null)
        {
            var (encPayload, encDek, encIv) = await EncryptSecretValueAsync(newPlaintext, cancellationToken);

            secret = new Secret
            {
                EnvironmentId = environment.Id,
                Key = normalizedKey,
                CurrentVersion = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                LastRotatedAtUtc = now,
                IsDeleted = false
            };

            _dbContext.Secrets.Add(secret);

            var version = new SecretVersion
            {
                SecretId = secret.Id,
                Version = 1,
                EncryptedPayload = encPayload,
                EncryptedDek = encDek,
                Iv = encIv,
                PayloadHash = newPayloadHash,
                CreatedByUserId = userId,
                CreatedAtUtc = now
            };
            _dbContext.SecretVersions.Add(version);

            AppendAuditEvent(projectId, environment.Id, secret.Id, userId, SecretAuditActions.Write, success: true);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new UpsertSecretResponse { CurrentVersion = 1, CreatedNewVersion = true };
        }

        var latestVersion = await _dbContext.SecretVersions
            .Where(v => v.SecretId == secret.Id)
            .OrderByDescending(v => v.Version)
            .FirstAsync(cancellationToken);

        if (await PlaintextMatchesLatestAsync(latestVersion, newPlaintext, newPayloadHash, cancellationToken))
        {
            AppendAuditEvent(projectId, environment.Id, secret.Id, userId, SecretAuditActions.Write, success: true);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new UpsertSecretResponse
            {
                CurrentVersion = secret.CurrentVersion,
                CreatedNewVersion = false
            };
        }

        var nextVersion = secret.CurrentVersion + 1;
        var encrypted = await EncryptSecretValueAsync(newPlaintext, cancellationToken);

        var newRow = new SecretVersion
        {
            SecretId = secret.Id,
            Version = nextVersion,
            EncryptedPayload = encrypted.EncryptedPayloadB64,
            EncryptedDek = encrypted.EncryptedDek,
            Iv = encrypted.EncryptedIv,
            PayloadHash = newPayloadHash,
            CreatedByUserId = userId,
            CreatedAtUtc = now
        };
        _dbContext.SecretVersions.Add(newRow);

        secret.CurrentVersion = nextVersion;
        secret.UpdatedAtUtc = now;
        secret.LastRotatedAtUtc = now;

        AppendAuditEvent(projectId, environment.Id, secret.Id, userId, SecretAuditActions.Write, success: true);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new UpsertSecretResponse { CurrentVersion = nextVersion, CreatedNewVersion = true };
    }

    public async Task<GetSecretResponse> GetSecretAsync(
        Guid projectId,
        Guid environmentId,
        string key,
        Guid userId,
        bool includeMetadata,
        int? version,
        CancellationToken cancellationToken = default)
    {
        var normalizedKey = NormalizeSecretKey(key);
        if (string.IsNullOrEmpty(normalizedKey))
        {
            throw new BadHttpRequestException("Secret key is required");
        }

        var (_, environment, _) = await GetAuthorizedEnvironmentAsync(projectId, environmentId, userId, cancellationToken);

        var secret = await _dbContext.Secrets
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.EnvironmentId == environment.Id && s.Key == normalizedKey && !s.IsDeleted,
                cancellationToken);

        if (secret == null)
        {
            AppendAuditEvent(projectId, environment.Id, null, userId, SecretAuditActions.ReadValue, success: false);
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new FileNotFoundException($"Secret '{normalizedKey}' was not found in this environment");
        }

        var versionQuery = _dbContext.SecretVersions.AsNoTracking().Where(v => v.SecretId == secret.Id);
        SecretVersion row;
        if (version.HasValue)
        {
            row = await versionQuery.FirstOrDefaultAsync(v => v.Version == version.Value, cancellationToken)
                ?? throw new FileNotFoundException($"Version {version.Value} does not exist for this secret");
        }
        else
        {
            row = await versionQuery.OrderByDescending(v => v.Version).FirstAsync(cancellationToken);
        }

        var plaintext = await DecryptSecretValueAsync(row, cancellationToken);

        AppendAuditEvent(projectId, environment.Id, secret.Id, userId, SecretAuditActions.ReadValue, success: true);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new GetSecretResponse { Value = plaintext };

        if (includeMetadata)
        {
            response.Key = secret.Key;
            response.CurrentVersion = secret.CurrentVersion;
            response.VersionReturned = row.Version;
            response.CreatedAtUtc = secret.CreatedAtUtc;
            response.UpdatedAtUtc = secret.UpdatedAtUtc;
        }

        return response;
    }

    public async Task DeleteSecretAsync(
        Guid projectId,
        Guid environmentId,
        string key,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var normalizedKey = NormalizeSecretKey(key);
        if (string.IsNullOrEmpty(normalizedKey))
        {
            throw new BadHttpRequestException("Secret key is required");
        }

        var (_, environment, role) = await GetAuthorizedEnvironmentAsync(projectId, environmentId, userId, cancellationToken);
        EnsureCanWriteSecrets(role);

        var secret = await _dbContext.Secrets
            .FirstOrDefaultAsync(
                s => s.EnvironmentId == environment.Id && s.Key == normalizedKey && !s.IsDeleted,
                cancellationToken);

        if (secret == null)
        {
            AppendAuditEvent(projectId, environment.Id, null, userId, SecretAuditActions.Delete, success: false);
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new FileNotFoundException($"Secret '{normalizedKey}' was not found in this environment");
        }

        var now = DateTime.UtcNow;
        secret.IsDeleted = true;
        secret.DeletedAtUtc = now;
        secret.UpdatedAtUtc = now;

        AppendAuditEvent(projectId, environment.Id, secret.Id, userId, SecretAuditActions.Delete, success: true);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> PlaintextMatchesLatestAsync(
        SecretVersion latestVersion,
        string newPlaintext,
        string newPayloadHash,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(latestVersion.PayloadHash) &&
            string.Equals(latestVersion.PayloadHash, newPayloadHash, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var existing = await DecryptSecretValueAsync(latestVersion, cancellationToken);
        return string.Equals(existing, newPlaintext, StringComparison.Ordinal);
    }

    private async Task<(Project project, ProjectEnvironment environment, ProjectMemberRole role)>
        GetAuthorizedEnvironmentAsync(Guid projectId, Guid environmentId, Guid userId, CancellationToken cancellationToken)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project ID is required", nameof(projectId));
        }

        if (environmentId == Guid.Empty)
        {
            throw new ArgumentException("Environment ID is required", nameof(environmentId));
        }

        if (userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted, cancellationToken)
            ?? throw new FileNotFoundException($"Project with id {projectId} not found");

        ProjectMemberRole role;
        if (project.OwnerUserId == userId)
        {
            role = ProjectMemberRole.Owner;
        }
        else
        {
            var membership = await _dbContext.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId, cancellationToken)
                ?? throw new UnauthorizedAccessException("You do not have access to this project");

            role = membership.Role;
        }

        var environment = await _dbContext.Environments
            .FirstOrDefaultAsync(e => e.Id == environmentId && e.ProjectId == projectId && !e.IsDeleted, cancellationToken)
            ?? throw new FileNotFoundException($"Environment with id {environmentId} not found");

        return (project, environment, role);
    }

    private static void EnsureCanWriteSecrets(ProjectMemberRole role)
    {
        if (role == ProjectMemberRole.ReadOnly)
        {
            throw new ForbiddenException("Read-only members cannot create, update, or delete secrets");
        }
    }

    private void AppendAuditEvent(
        Guid projectId,
        Guid environmentId,
        Guid? secretId,
        Guid userId,
        string action,
        bool success)
    {
        var http = _httpContextAccessor.HttpContext;
        var ip = http?.Connection.RemoteIpAddress?.ToString();
        var ua = http?.Request.Headers.UserAgent.ToString();
        if (ua?.Length > 256)
        {
            ua = ua[..256];
        }

        _dbContext.SecretAuditEvents.Add(new SecretAuditEvent
        {
            ProjectId = projectId,
            EnvironmentId = environmentId,
            SecretId = secretId,
            UserId = userId,
            Action = action,
            Success = success,
            Ip = ip,
            UserAgent = ua,
            OccurredAtUtc = DateTime.UtcNow
        });
    }

    private async Task<(string EncryptedPayloadB64, string EncryptedDek, string EncryptedIv)> EncryptSecretValueAsync(
        string plaintext,
        CancellationToken cancellationToken)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var key = RandomNumberGenerator.GetBytes(EncryptionConstants.EncryptionKeySize);
        var nonce = RandomNumberGenerator.GetBytes(EncryptionConstants.NonceSize);

        var ciphertext = new byte[plainBytes.Length];
        var tag = new byte[EncryptionConstants.TagSize];
        using (var aes = new AesGcm(key, EncryptionConstants.TagSize))
        {
            aes.Encrypt(nonce, plainBytes, ciphertext, tag);
        }

        var combined = new byte[ciphertext.Length + tag.Length];
        Buffer.BlockCopy(ciphertext, 0, combined, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, combined, ciphertext.Length, tag.Length);

        var encPayload = Convert.ToBase64String(combined);
        var encDek = await _keyEncryptionService.EncryptKeyAsync(key, cancellationToken);
        var encIv = await _keyEncryptionService.EncryptIvAsync(nonce, cancellationToken);

        return (encPayload, encDek, encIv);
    }

    private async Task<string> DecryptSecretValueAsync(SecretVersion version, CancellationToken cancellationToken)
    {
        var key = await _keyEncryptionService.DecryptKeyAsync(version.EncryptedDek, cancellationToken);
        var nonce = await _keyEncryptionService.DecryptIvAsync(version.Iv, cancellationToken);

        var combined = Convert.FromBase64String(version.EncryptedPayload);
        if (combined.Length < EncryptionConstants.TagSize)
        {
            throw new CryptographicException("Invalid encrypted secret payload");
        }

        var ciphertextLength = combined.Length - EncryptionConstants.TagSize;
        var ciphertext = combined.AsSpan(0, ciphertextLength);
        var tag = combined.AsSpan(ciphertextLength);
        var plainBytes = new byte[ciphertextLength];

        using var aes = new AesGcm(key, EncryptionConstants.TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    private static string ComputePayloadHash(string plaintext)
    {
        var bytes = Encoding.UTF8.GetBytes(plaintext);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static string NormalizeSecretKey(string key)
    {
        return key.Trim().ToUpperInvariant();
    }
}
