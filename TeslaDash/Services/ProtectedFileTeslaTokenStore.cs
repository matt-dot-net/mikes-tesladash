using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace TeslaDash.Services;

public interface ITeslaTokenStore
{
    Task<TeslaToken?> GetAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(TeslaToken token, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}

public sealed class ProtectedFileTeslaTokenStore : ITeslaTokenStore
{
    private readonly IDataProtector _protector;
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public ProtectedFileTeslaTokenStore(IDataProtectionProvider provider, IOptions<TeslaOptions> options, IWebHostEnvironment environment)
    {
        _protector = provider.CreateProtector("TeslaDash.Tokens.v1");
        _path = Path.IsPathRooted(options.Value.TokenPath)
            ? options.Value.TokenPath
            : Path.Combine(environment.ContentRootPath, options.Value.TokenPath);
    }

    public async Task<TeslaToken?> GetAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_path)) return null;
            var protectedValue = await File.ReadAllTextAsync(_path, cancellationToken);
            return JsonSerializer.Deserialize<TeslaToken>(_protector.Unprotect(protectedValue));
        }
        finally { _gate.Release(); }
    }

    public async Task SaveAsync(TeslaToken token, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var protectedValue = _protector.Protect(JsonSerializer.Serialize(token));
            await File.WriteAllTextAsync(_path, protectedValue, cancellationToken);
        }
        finally { _gate.Release(); }
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(_path)) File.Delete(_path);
        return Task.CompletedTask;
    }
}
