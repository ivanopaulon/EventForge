namespace Prym.DTOs.Business.Fidelity;

public class FidelityPointsCampaignDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Multiplier { get; set; }
    public FidelityPointsRoundingMode RoundingMode { get; set; }
    public bool IsActive { get; set; }
    public string? ProductIdsJSON { get; set; }
    public string? CategoryIdsJSON { get; set; }
    public DateTime CreatedAt { get; set; }
}
