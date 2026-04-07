using Microsoft.AspNetCore.Http;

namespace lockhaven_backend.Tests.TestInfrastructure;

public static class TestFormFile
{
    public static IFormFile Create(string fileName, string contentType, ReadOnlySpan<byte> content)
    {
        var stream = new MemoryStream();
        stream.Write(content);
        stream.Position = 0;
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    public static IFormFile Create(string fileName, string contentType, string utf8Text) =>
        Create(fileName, contentType, System.Text.Encoding.UTF8.GetBytes(utf8Text));
}
