namespace Prym.DTOs.PaymentTerminal;

public class PaymentResultDto
{
    public bool Success { get; set; }
    public bool Approved { get; set; }
    public string? ResponseCode { get; set; }
    public string? AuthorizationCode { get; set; }
    public decimal Amount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime TransactionAt { get; set; } = DateTime.UtcNow;
}
