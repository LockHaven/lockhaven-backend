using Microsoft.AspNetCore.Http;

namespace lockhaven_backend.Services.Interfaces;

public interface IFileValidationService
{
    void ValidateUpload(IFormFile file);
}
