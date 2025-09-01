using System;

namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// DTO for stock movement summary data.
    /// </summary>
    public class MovementSummaryDto
    {
        public Guid? ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }

        public Guid? LocationId { get; set; }
        public string? LocationCode { get; set; }
        public string? WarehouseName { get; set; }

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public decimal TotalInbound { get; set; }
        public decimal TotalOutbound { get; set; }
        public decimal NetMovement => TotalInbound - TotalOutbound;

        public int InboundTransactionCount { get; set; }
        public int OutboundTransactionCount { get; set; }
        public int TotalTransactionCount => InboundTransactionCount + OutboundTransactionCount;

        public decimal? TotalInboundValue { get; set; }
        public decimal? TotalOutboundValue { get; set; }
        public decimal? NetValue
        {
            get
            {
                var inbound = TotalInboundValue ?? 0;
                var outbound = TotalOutboundValue ?? 0;
                return inbound - outbound;
            }
        }

        public decimal? AverageInboundCost
        {
            get
            {
                if (InboundTransactionCount > 0 && TotalInboundValue.HasValue)
                    return TotalInboundValue.Value / InboundTransactionCount;
                return null;
            }
        }

        public decimal? AverageOutboundCost
        {
            get
            {
                if (OutboundTransactionCount > 0 && TotalOutboundValue.HasValue)
                    return TotalOutboundValue.Value / OutboundTransactionCount;
                return null;
            }
        }
    }
}