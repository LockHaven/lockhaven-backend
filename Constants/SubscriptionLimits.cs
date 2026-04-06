using lockhaven_backend.Models;

namespace lockhaven_backend.Constants;

public static class SubscriptionLimits
{
    public const long FreeMaxFileSizeBytes = 25L * 1024 * 1024; // 25 MB
    public const long FreeMaxTotalStorageBytes = 250L * 1024 * 1024; // 250 MB
    public const int FreeMaxUploadsPerDay = 100;

    public const long PaidMaxFileSizeBytes = 250L * 1024 * 1024; // 250 MB
    public const long PaidMaxTotalStorageBytes = 10L * 1024 * 1024 * 1024; // 10 GB

    public static Limits ForTier(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Paid => new Limits(
            PaidMaxFileSizeBytes,
            PaidMaxTotalStorageBytes,
            MaxUploadsPerDay: null),
        _ => new Limits(
            FreeMaxFileSizeBytes,
            FreeMaxTotalStorageBytes,
            FreeMaxUploadsPerDay)
    };

    public sealed record Limits(long MaxFileSizeBytes, long MaxTotalStorageBytes, int? MaxUploadsPerDay);
}
