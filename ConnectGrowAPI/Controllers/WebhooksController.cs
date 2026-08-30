
using ConnectGrowAPI.Interfaces;
using ConnectGrowAPI.Services;
using ConnectGrowAPI.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectGrowAPI.Controllers;

//WebHooks gets real-time data notificiations from the Payfast server after payment occurs (ITN)
//https://dj-payfast.readthedocs.io/en/stable/webhooks.html 
//https://www.c-sharpcorner.com/article/webhooks-in-net/

[Route("api/webhooks")]
[AllowAnonymous]
[ApiController]
public class WebhooksController : ControllerBase
{
    private readonly IPayFastItnValidator _validator;
    private readonly IBookingService _bookings;
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IPayFastItnValidator validator,
        IBookingService bookings,
        IBookingRepository bookingRepository,
        ILogger<WebhooksController> logger)
    {
        _validator = validator;
        _bookings = bookings;
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    //Payfast ITN 
    //After user completes payment, Payfast sends post request. 

    [HttpPost("payfast")]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PayFastItn(CancellationToken ct)
    {
        if (!Request.HasFormContentType)
        {
            _logger.LogWarning("ITN received with unexpected content type {Type}.",
                Request.ContentType);
            return BadRequest();
        }

        var form = await Request.ReadFormAsync(ct);

        var fields = form
            .Select(f => new KeyValuePair<string, string?>(f.Key, f.Value.ToString()))
            .ToList();

        var reference = Value(fields, "m_payment_id") ?? Value(fields, "custom_str1");
        var gatewayReference = Value(fields, "pf_payment_id");
        var paymentStatus = Value(fields, "payment_status");

        _logger.LogInformation(
            "ITN received for {Reference} with status {Status}.",
            reference ?? "(no reference)", paymentStatus ?? "(none)");

        if (string.IsNullOrWhiteSpace(reference))
        {
            _logger.LogWarning("ITN carried no booking reference. Discarded.");
            return BadRequest();
        }

        //Updates booking first
        var booking = await _bookingRepository.GetByReferenceAsync(reference, ct);

        if (booking is null)
        {
            _logger.LogWarning("ITN referenced unknown booking {Reference}.", reference);

            return Ok();
        }

        var validation = await _validator.ValidateAsync(
            fields,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            booking.Amount,
            ct);

        if (!validation.IsValid)
        {
            _logger.LogError(
                "ITN validation failed for {Reference}: {Reason}",
                reference, validation.FailureReason);

            // No booking state changes on a failed validation.
            return BadRequest();
        }

        var rawPayload = System.Text.Json.JsonSerializer.Serialize(
            fields.ToDictionary(f => f.Key, f => f.Value));

        if (string.Equals(paymentStatus, "COMPLETE", StringComparison.OrdinalIgnoreCase))
        {
            var result = await _bookings.ConfirmPaymentAsync(
                bookingReference: reference,
                gatewayReference: gatewayReference ?? $"PF-{reference}",
                amountPaid: booking.Amount,
                paymentMethod: "PayFast",
                rawPayload: rawPayload,
                ct: ct);

            if (result.IsFailure)
            {
                _logger.LogError(
                    "Validated ITN for {Reference} could not be applied: {Error}",
                    reference, result.Error);

        
                return BadRequest();
            }

            _logger.LogInformation("Booking {Reference} confirmed via ITN.", reference);
            return Ok();
        }

        if (string.Equals(paymentStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(paymentStatus, "FAILED", StringComparison.OrdinalIgnoreCase))
        {
            await _bookings.RecordFailedPaymentAsync(
                bookingReference: reference,
                gatewayReference: gatewayReference ?? string.Empty,
                errorMessage: $"Payfast reported status {paymentStatus}.",
                rawPayload: rawPayload,
                ct: ct);

            return Ok();
        }

        // Intermediate states are recorded and otherwise ignored, like pending 
        _logger.LogInformation(
            "ITN for {Reference} carried status {Status}. No action taken.",
            reference, paymentStatus);

        return Ok();
    }

    private static string? Value(List<KeyValuePair<string, string?>> fields, string key) =>
        fields.FirstOrDefault(f =>
            string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase)).Value;
}