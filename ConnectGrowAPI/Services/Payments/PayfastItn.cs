using System.Globalization;
using System.Net;
using ConnectGrowAPI.Services.Payments;
using Microsoft.Extensions.Options;

namespace ConnectGrowAPI.Services.Payments;

//https://support.payfast.help/portal/en/kb/articles/how-to-set-up-and-test-your-payfast-gateway-integration 
public record ItnValidationResult(bool IsValid, string? FailureReason)
{
    public static ItnValidationResult Valid() => new(true, null);
    public static ItnValidationResult Invalid(string reason) => new(false, reason);
}

public interface IPayFastItnValidator
{ 
    Task<ItnValidationResult> ValidateAsync(
        IReadOnlyList<KeyValuePair<string, string?>> form,//fields in the correct order for payfast
        string? sourceIp,//payfast source
        decimal expectedAmount,//expected amount
        CancellationToken ct = default);
}

/*Different checks to avoid leaving the endpoint exploitable and confirm validity of payment process*/
public class PayFastItnValidator : IPayFastItnValidator
{
    private readonly PayFastOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PayFastItnValidator> _logger;

    public const string HttpClientName = "payfast";


    public PayFastItnValidator(
        IOptions<PayFastOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<PayFastItnValidator> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ItnValidationResult> ValidateAsync(
        IReadOnlyList<KeyValuePair<string, string?>> form,
        string? sourceIp,
        decimal expectedAmount,
        CancellationToken ct = default)
    {
        // signature checks if the payload has passphrase
        var received = form.FirstOrDefault(f => f.Key == "signature").Value;

        // Everything except the signature itself, in the order it arrived.
        var signable = form.Where(f => f.Key != "signature").ToList();

        var expected = PayFastSignatureHelper.GenerateSignature(
    signable, _options.Passphrase, skipEmptyValues: false);
        if (!PayFastSignatureHelper.SignaturesMatch(expected, received))
        {
            _logger.LogWarning(
                "ITN signature mismatch. Expected {Expected}, received {Received}.",
                expected, received ?? "(none)");

            return ItnValidationResult.Invalid("Signature mismatch.");
        }

        // checks if payment came from payfast itself
        if (_options.ValidateSourceIp)
        {
            var ipValid = await IsKnownPayFastAddressAsync(sourceIp, ct);
            if (!ipValid)
            {
                _logger.LogWarning("ITN rejected from unrecognised address {Ip}.", sourceIp);
                return ItnValidationResult.Invalid("Unrecognised source address.");
            }
        }
        else
        {
            _logger.LogWarning(
                "Payfast source-IP validation is disabled. This weakens ITN verification.");
        }

        // checks if ampunts match from Payfast
        var grossRaw = form.FirstOrDefault(f => f.Key == "amount_gross").Value;

        if (!decimal.TryParse(grossRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var gross))
        {
            _logger.LogWarning("ITN carried an unparseable amount_gross: {Value}.", grossRaw);
            return ItnValidationResult.Invalid("Amount could not be read.");
        }

        // uses absolute rounding for accuracy
        if (Math.Abs(gross - expectedAmount) > 0.01m)
        {
            _logger.LogError(
                "ITN amount mismatch: booking expects {Expected}, notification carried {Received}.",
                expectedAmount, gross);

            return ItnValidationResult.Invalid("Amount does not match the booking.");
        }

        var confirmed = await ConfirmWithPayFastAsync(signable, received, ct);
        if (!confirmed)
        {
            _logger.LogWarning("Payfast did not confirm the notification as genuine.");
            return ItnValidationResult.Invalid("Payfast did not validate the notification.");
        }

        return ItnValidationResult.Valid();
    }

    //resolves the configured hostnames and compares
    private async Task<bool> IsKnownPayFastAddressAsync(string? sourceIp, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sourceIp)) return false;
        if (!IPAddress.TryParse(sourceIp, out var caller)) return false;

        foreach (var host in _options.ValidHosts)
        {
            try
            {
                var addresses = await Dns.GetHostAddressesAsync(host, ct);
                if (addresses.Any(a => a.Equals(caller))) return true;
            }
            catch (Exception ex)
            {
                // A DNS failure must not be treated as a pass.
                _logger.LogWarning(ex, "Could not resolve Payfast host {Host}.", host);
            }
        }

        return false;
    }

    //Sends the payload back to Payfast, which replies VALID or INVALID. 
    private async Task<bool> ConfirmWithPayFastAsync(
        IReadOnlyList<KeyValuePair<string, string?>> signable,
        string? signature,
        CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);

            var fields = signable
                .Where(f => f.Value is not null)
                .Select(f => new KeyValuePair<string, string>(f.Key, f.Value!))
                .ToList();

            if (!string.IsNullOrWhiteSpace(signature))
                fields.Add(new KeyValuePair<string, string>("signature", signature));

            using var content = new FormUrlEncodedContent(fields);
            using var response = await client.PostAsync(_options.ValidateUrl, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Payfast validation endpoint returned {Status}.", response.StatusCode);
                return false;
            }

            var body = (await response.Content.ReadAsStringAsync(ct)).Trim();

            return body.StartsWith("VALID", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            // Fail closed. An unconfirmed notification is not a paid booking
            _logger.LogError(ex, "Payfast server-to-server validation call failed.");
            return false;
        }
    }
}