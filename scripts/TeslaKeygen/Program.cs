using System.Security.Cryptography;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: TeslaKeygen <private-key-output-path>");
    return 1;
}

var outputPath = Path.GetFullPath(args[0]);
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
await File.WriteAllTextAsync(outputPath, key.ExportECPrivateKeyPem());

// Import the saved value once so a corrupt or unsupported export fails immediately.
using var verificationKey = ECDsa.Create();
verificationKey.ImportFromPem(await File.ReadAllTextAsync(outputPath));

Console.WriteLine($"Created Tesla private key at {outputPath}");
Console.WriteLine("This file is ignored by Git. Back it up securely; replacing it requires registering the new public key with Tesla.");
return 0;
