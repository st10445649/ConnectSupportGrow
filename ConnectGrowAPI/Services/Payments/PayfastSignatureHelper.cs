using System.Security.Cryptography;
using System.Text;

namespace ConnectGrowAPI.Services.Payments;


//request has to geenrate an MD5 siganture for valdiity. 
//https://developers.payfast.co.za/api#authentication


//has to be url encoded which is converted to here and hashed. 
public static class PayFastSignatureHelper
{

    public static string UrlEncode(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        return Uri.EscapeDataString(value)
                  .Replace("%20", "+")
                  .Replace("~", "%7E");
    }

    public static string BuildParameterString(
        IEnumerable<KeyValuePair<string, string?>> parameters,
        string? passphrase = null)
    {
        var builder = new StringBuilder();

        foreach (var (key, value) in parameters)
        {
            if (string.IsNullOrEmpty(value)) continue;

            if (builder.Length > 0) builder.Append('&');

            builder.Append(key).Append('=').Append(UrlEncode(value.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(passphrase))
        {
            builder.Append("&passphrase=").Append(UrlEncode(passphrase.Trim()));
        }

        return builder.ToString();
    }

    public static string GenerateSignature(
        IEnumerable<KeyValuePair<string, string?>> parameters,
        string? passphrase = null)
    {
        var parameterString = BuildParameterString(parameters, passphrase);
        return Md5Hex(parameterString);
    }

    public static string Md5Hex(string input)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static bool SignaturesMatch(string expected, string? received)
    {
        if (string.IsNullOrWhiteSpace(received)) return false;
        if (expected.Length != received.Length) return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(received.ToLowerInvariant()));
    }
}