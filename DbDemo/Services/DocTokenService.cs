using System.Text.Json;

namespace DbDemo.Services;

public class DocTokenService(CryptoService crypto)
{
    private readonly CryptoService crypto = crypto;

    public DocToken? Decrypt(string token)
    {
        try
        {
            var result = crypto.Decrypt(token);
            return JsonSerializer.Deserialize<DocToken>(result);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public string? GenerateDocToken(DocToken docToken)
    {
        try
        {
            if (docToken == null)
                return null;

            var json = JsonSerializer.Serialize(docToken);
            return crypto.Encrypt(json);
        }
        catch (Exception)
        {
            return null;
        }
    }


}
