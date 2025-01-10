using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using System.Text;

namespace DbDemo.Services;

public class CryptoService
{
    private const string keyStr = CRYPTO.KEY;
    private const string ivStr = CRYPTO.IV;
    private readonly byte[] key = Encoding.UTF8.GetBytes(keyStr);
    private readonly byte[] iv = Encoding.UTF8.GetBytes(ivStr);

    public string? Encrypt(string plaintext)
    {
        try
        {
            var input = Encoding.UTF8.GetBytes(plaintext);
            var engine = new AesEngine();
            var blockCipher = new CbcBlockCipher(engine);
            var cipher = new PaddedBufferedBlockCipher(blockCipher); // PKCS5/7 padding
            var keyParam = new KeyParameter(key);
            var keyParamWithIV = new ParametersWithIV(keyParam, iv);

            cipher.Init(true, keyParamWithIV);
            var output = new byte[cipher.GetOutputSize(input.Length)];
            var length = cipher.ProcessBytes(input, output, 0);
            cipher.DoFinal(output, length);

            return Convert.ToBase64String(output);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public string? Decrypt(string ciphertext)
    {
        try
        {
            var input = Convert.FromBase64String(ciphertext);
            var engine = new AesEngine();
            var blockCipher = new CbcBlockCipher(engine);
            var cipher = new PaddedBufferedBlockCipher(blockCipher); // PKCS5/7 padding
            var keyParam = new KeyParameter(key);
            var keyParamWithIV = new ParametersWithIV(keyParam, iv);

            cipher.Init(false, keyParamWithIV);
            var output = new byte[cipher.GetOutputSize(input.Length)];
            var length = cipher.ProcessBytes(input, output, 0);
            cipher.DoFinal(output, length);

            return Encoding.UTF8.GetString(output).TrimEnd('\0');
        }
        catch (Exception)
        {
            return null;
        }
    }
}
