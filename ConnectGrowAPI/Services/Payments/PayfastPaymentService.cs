using System.Globalization;
using System.Text;
using ConnectGrowAPI.Interfaces;
using ConnectGrowAPI.Models;
using ConnectGrowAPI.Services;
using ConnectGrowAPI.Services.Payments;
using Microsoft.Extensions.Options;

namespace ConnectGrowAPI.Services.Payments;


//https://developers.payfast.co.za/api#introduction 
//helps to build a signed payfast checkout url
//redirects to payfast and returns back to app after.

//the app does not know what happens after redirect.
//just receives ITN callback 


//implements payment service. any other payment option can use this service too 
public class PayFastPaymentService : IPaymentService
{
    private readonly PayFastOptions _options;
    private readonly ILogger<PayFastPaymentService> _logger;

    public PayFastPaymentService(
        IOptions<PayFastOptions> options,
        ILogger<PayFastPaymentService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.MerchantId) ||
            string.IsNullOrWhiteSpace(_options.MerchantKey))
        {
            throw new InvalidOperationException(
                "PayFast:MerchantId and PayFast:MerchantKey must be configured.");
        }
    }

    public Task<Result<string>> CreateCheckoutAsync(Booking booking, CancellationToken ct = default)
    {
        if (booking.Webinar is null || booking.User is null)
        {
            
            _logger.LogError(
                "Booking {Reference} reached checkout without Webinar or User loaded.",
                booking.BookingReference);

            return Task.FromResult(Result<string>.Failure(
                ErrorType.Unexpected, "Could not start checkout. Please try again."));
        }

        if (booking.Amount < _options.MinimumAmount)
        {
            return Task.FromResult(Result<string>.Invalid(
                $"Payfast cannot process amounts below R{_options.MinimumAmount:F2}."));
        }

        // from payfast documentation
        var parameters = new List<KeyValuePair<string, string?>>
        {
            new("merchant_id",   _options.MerchantId),
            new("merchant_key",  _options.MerchantKey),
             new("return_url",    AppendQuery(_options.ReturnUrl, "id", booking.Id.ToString())),
            new("cancel_url",    AppendQuery(_options.CancelUrl, "webinarId", booking.WebinarId.ToString())),
 
            new("notify_url",    _options.NotifyUrl),
            new("name_first",    Truncate(booking.User.FirstName, 100)),
            new("name_last",     Truncate(booking.User.LastName, 100)),
            new("email_address", booking.User.Email),

            //csg reference sent to the ITN
            new("m_payment_id",  booking.BookingReference),

            new("amount",        booking.Amount.ToString("F2", CultureInfo.InvariantCulture)),
            new("item_name",     Truncate(booking.Webinar.Title, 100)),
            new("item_description", Truncate(
                $"Webinar on {booking.Webinar.StartDateTime:d MMMM yyyy}", 255)),

            new("custom_str1",   booking.BookingReference)
        };

        var signature = PayFastSignatureHelper.GenerateSignature(parameters, _options.Passphrase);

        var url = BuildRedirectUrl(parameters, signature);

        _logger.LogInformation(
            "Payfast checkout created for booking {Reference} ({Amount}, sandbox: {Sandbox}).",
            booking.BookingReference, booking.Amount, _options.UseSandbox);

        return Task.FromResult(Result<string>.Success(url));
    }

    // Builds a GET redirect. Encoding here uses the same helper as the
    // signature, so the values Payfast receives are byte-identical to the ones
    // that were hashed
    private string BuildRedirectUrl(
        List<KeyValuePair<string, string?>> parameters, string signature)
    {
        var query = new StringBuilder();

        foreach (var (key, value) in parameters)
        {
            if (string.IsNullOrEmpty(value)) continue;

            if (query.Length > 0) query.Append('&');

            query.Append(key)
                 .Append('=')
                 .Append(PayFastSignatureHelper.UrlEncode(value.Trim()));
        }

        query.Append("&signature=").Append(signature);

        return $"{_options.ProcessUrl}?{query}";
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string AppendQuery(string url, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;
 
        var separator = url.Contains('?') ? '&' : '?';
        return $"{url}{separator}{key}={value}";
    }
}