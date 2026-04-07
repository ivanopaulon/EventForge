using EventForge.Client.Shared.Components;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace EventForge.Client.Shared.Management;

public class EntityManagementConfig<TEntity> where TEntity : class
{
    /// <summary>
    /// Optional breadcrumb items passed to ManagementPageHeader.
    /// When null, no breadcrumbs are shown.
    /// </summary>
    public List<BreadcrumbItem>? BreadcrumbItems { get; set; }
    public required string ComponentKey { get; set; }
    public required string PageTitleKey { get; set; }
    public required string PageTitleDefault { get; set; }
    public required string EntityIcon { get; set; }
    public required string PageRootCssClass { get; set; }
    public required string BaseRoute { get; set; }
    public required List<EFTableColumnConfiguration> Columns { get; set; }
    public string SearchPlaceholderKey { get; set; } = "common.search";
    public string SearchPlaceholderDefault { get; set; } = "Cerca...";
    public string ExcelFileName { get; set; } = "Export";
    public bool ShowExport { get; set; } = true;
    public int DefaultPageSize { get; set; } = 20;

    /// <summary>
    /// When true, EntityManagementPage passes searchTerm and filters to GetPagedAsync
    /// and delegates paging to the server. Default is false (client-side filtering).
    /// </summary>
    public bool UseServerSidePaging { get; set; } = false;

    /// <summary>
    /// Optional list of progress messages to show in the loading overlay progress log.
    /// When null or empty, the overlay shows only the message without a log panel.
    /// </summary>
    public IReadOnlyList<string>? LoadingProgressMessages { get; set; }

    /// <summary>
    /// When true, the loading overlay shows a progress log panel with cycling messages.
    /// Requires LoadingProgressMessages to be populated.
    /// </summary>
    public bool ShowLoadingProgressLog { get; set; } = false;

    /// <summary>
    /// When true, the loading overlay shows an elapsed time indicator.
    /// </summary>
    public bool ShowLoadingElapsedTime { get; set; } = false;

    public List<QuickFilter<TEntity>> QuickFilters { get; set; } = new();
    public bool ShowEdit { get; set; } = true;
    public bool ShowDelete { get; set; } = true;
    public bool ShowAuditLog { get; set; } = true;
    public bool ShowToggleStatus { get; set; } = false;
    public bool ShowView { get; set; } = false;
    public Func<TEntity, Color>? GetStatusColor { get; set; }
    public Func<TEntity, string>? GetStatusIcon { get; set; }
    public Func<TEntity, string>? GetStatusText { get; set; }
    public Func<TEntity, bool>? GetIsActive { get; set; }
    public required Func<TEntity, string> GetDisplayName { get; set; }
    public required Func<TEntity, Guid> GetId { get; set; }

    /// <summary>
    /// Optional predicate to determine if a specific entity can be deleted.
    /// When null, all entities are considered deletable.
    /// When provided and returning false, delete is blocked with a warning message.
    /// </summary>
    public Func<TEntity, bool>? CanDelete { get; set; }

    /// <summary>
    /// Optional predicate to determine if a specific entity is editable.
    /// When null, all entities are considered editable (if ShowEdit = true).
    /// When provided and returning false, the Edit button is disabled for that row.
    /// </summary>
    public Func<TEntity, bool>? CanEdit { get; set; }

    /// <summary>
    /// Translation key for the Edit button tooltip. Default: "common.edit".
    /// </summary>
    public string EditTooltipKey { get; set; } = "common.edit";

    /// <summary>
    /// Translation key for the Delete button tooltip. Default: "common.delete".
    /// </summary>
    public string DeleteTooltipKey { get; set; } = "common.delete";

    /// <summary>
    /// Translation key for the View button tooltip. Default: "common.view".
    /// </summary>
    public string ViewTooltipKey { get; set; } = "common.view";

    /// <summary>
    /// Translation key for the AuditLog button tooltip. Default: "common.auditLog".
    /// </summary>
    public string AuditLogTooltipKey { get; set; } = "common.auditLog";

    /// <summary>
    /// Override of the loading content. When null, the standard PageLoadingOverlay is used.
    /// </summary>
    public RenderFragment? CustomLoadingContent { get; set; }

    /// <summary>
    /// Translation key for the warning message shown when CanDelete returns false.
    /// </summary>
    public string CannotDeleteMessageKey { get; set; } = "common.cannotDelete";

    public string CreateTooltip { get; set; } = "common.createNew";
    public string DeleteConfirmMessageKey { get; set; } = "common.confirmDelete";
    public string DeleteSuccessMessageKey { get; set; } = "common.deleteSuccess";
    public string DeleteErrorMessageKey { get; set; } = "common.deleteError";
    public string BulkDeleteConfirmMessageKey { get; set; } = "common.confirmBulkDelete";
    public string BulkDeleteSuccessMessageKey { get; set; } = "common.bulkDeleteSuccess";
    public string LoadErrorMessageKey { get; set; } = "common.loadError";
    public required string EntityTypeName { get; set; }
}
