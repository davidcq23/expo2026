using System.Security.Cryptography;
using ContactLandingApi.Data;
using Microsoft.EntityFrameworkCore;

namespace ContactLandingApi.Services;

public class ContactCodeService(AppDbContext dbContext)
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = GenerateCode(5);
            var exists = await dbContext.Contacts.AnyAsync(x => x.Codigo == code, cancellationToken);
            if (!exists)
            {
                return code;
            }
        }

        throw new InvalidOperationException("No fue posible generar un código único.");
    }

    private static string GenerateCode(int length)
    {
        var chars = new char[length];
        var bytes = RandomNumberGenerator.GetBytes(length);

        for (var i = 0; i < length; i++)
        {
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];
        }

        return new string(chars);
    }
}
