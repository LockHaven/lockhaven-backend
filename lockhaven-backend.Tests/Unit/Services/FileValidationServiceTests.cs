using lockhaven_backend.Services;
using lockhaven_backend.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace lockhaven_backend.Tests.Unit.Services;

public class FileValidationServiceTests
{
    private static FileValidationService CreateSut(bool enableSignatureChecks = false) =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileValidation:EnableSignatureChecks"] = enableSignatureChecks ? "true" : "false"
            })
            .Build());

    [Fact]
    public void ValidateUpload_AcceptsTextFileWithPlainMime()
    {
        var sut = CreateSut();
        var file = TestFormFile.Create("notes.txt", "text/plain", "hello world");

        sut.ValidateUpload(file);
    }

    [Fact]
    public void ValidateUpload_ThrowsWhenExtensionNotAllowed()
    {
        var sut = CreateSut();
        var file = TestFormFile.Create("hack.exe", "application/octet-stream", [1, 2, 3]);

        Assert.Throws<BadHttpRequestException>(() => sut.ValidateUpload(file));
    }

    [Fact]
    public void ValidateUpload_ThrowsWhenMimeDoesNotMatchExtension()
    {
        var sut = CreateSut();
        var file = TestFormFile.Create("notes.txt", "image/png", "hello");

        Assert.Throws<BadHttpRequestException>(() => sut.ValidateUpload(file));
    }

    [Fact]
    public void ValidateUpload_ThrowsWhenFileEmpty()
    {
        var sut = CreateSut();
        var file = TestFormFile.Create("notes.txt", "text/plain", ReadOnlySpan<byte>.Empty);

        Assert.Throws<BadHttpRequestException>(() => sut.ValidateUpload(file));
    }
}
