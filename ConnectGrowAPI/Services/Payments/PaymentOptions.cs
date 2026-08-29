namespace ConnectGrowAPI.Services.Payments;
public class PayFastOptions
{
    public const string SectionName = "PayFast";
 
    public string MerchantId { get; set; } = string.Empty;
    public string MerchantKey { get; set; } = string.Empty;
 
    public string? Passphrase { get; set; }
 
    public bool UseSandbox { get; set; } = true;
 
    //where the buyer is sent  after a successful payment
    public string ReturnUrl { get; set; } = string.Empty;
 
    public string CancelUrl { get; set; } = string.Empty;
    public string NotifyUrl { get; set; } = string.Empty;
 
    //Hostnames Payfast sends notifications from. Resolved to IP addresses at
    // validation time rather than hard-coded as Payfast changes the
    // underlying addresses periodically.
    public string[] ValidHosts { get; set; } =
    {
        "www.payfast.co.za",
        "sandbox.payfast.co.za",
        "w1w.payfast.co.za",
        "w2w.payfast.co.za"
    };
    public bool ValidateSourceIp { get; set; } = true;
 
    //payfast rejects transactions below a minimum amount
    public decimal MinimumAmount { get; set; } = 5.00m;
 

 //https://developers.payfast.co.za/docs#sandbox
 //https://www.youtube.com/watch?v=aEb_v5OjQX4&t=595s 
    public string ProcessUrl => UseSandbox
        ? "https://sandbox.payfast.co.za/eng/process"
        : "https://www.payfast.co.za/eng/process";
 
    public string ValidateUrl => UseSandbox
        ? "https://sandbox.payfast.co.za/eng/query/validate"
        : "https://www.payfast.co.za/eng/query/validate";
}