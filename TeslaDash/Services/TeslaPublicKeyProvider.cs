using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace TeslaDash.Services;

public sealed class TeslaPublicKeyProvider(IOptions<TeslaOptions> options)
{
    private readonly TeslaOptions _options = options.Value;

    public string? GetPublicKeyPem()
    {
        if (string.IsNullOrWhiteSpace(_options.PrivateKeyBase64)) return null;

        var privateKeyPem = Encoding.UTF8.GetString(Convert.FromBase64String(_options.PrivateKeyBase64));
        using var key = ECDsa.Create();
        key.ImportFromPem(privateKeyPem);
        return key.ExportSubjectPublicKeyInfoPem();
    }
}
