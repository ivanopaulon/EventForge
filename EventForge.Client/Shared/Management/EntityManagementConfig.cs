using MudBlazor;
using EventForge.Client.Shared.Components;

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
