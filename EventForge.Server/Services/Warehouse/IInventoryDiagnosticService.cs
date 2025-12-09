using EventForge.DTOs.Warehouse;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EventForge.Server.Services.Warehouse;

public interface IInventoryDiagnosticService
{
    Task<InventoryDiagnosticReportDto> DiagnoseDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<InventoryRepairResultDto> AutoRepairDocumentAsync(Guid documentId, InventoryAutoRepairOptionsDto options, string currentUser, CancellationToken cancellationToken = default);
    Task<bool> RepairRowAsync(Guid documentId, Guid rowId, InventoryRowRepairDto repairData, string currentUser, CancellationToken cancellationToken = default);
    Task<int> RemoveProblematicRowsAsync(Guid documentId, List<Guid> rowIds, string currentUser, CancellationToken cancellationToken = default);
}
