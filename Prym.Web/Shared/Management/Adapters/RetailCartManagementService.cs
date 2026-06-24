using Blazored.LocalStorage;
using Prym.DTOs.Common;
using Prym.DTOs.RetailCart;
using Prym.Web.Services;

namespace Prym.Web.Shared.Management.Adapters;

public class RetailCartManagementService(
    IRetailCartSessionService cartService,
    ILocalStorageService localStorage)
    : IEntityManagementService<CartSessionDto>
{
    private const string StorageKey = "retail_cart_tracked_sessions";
    private const int MaxTrackedSessions = 50;

    public async Task<PagedResult<CartSessionDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        Dictionary<string, object?>? filters = null,
        CancellationToken ct = default)
    {
        var trackedIds = await localStorage.GetItemAsync<List<Guid>>(StorageKey) ?? [];
        trackedIds = trackedIds.Distinct().Take(MaxTrackedSessions).ToList();

        var tasks = trackedIds.Select(id => cartService.GetSessionAsync(id, ct));
        var results = await Task.WhenAll(tasks);

        var all = results
            .Where(s => s is not null)
            .Cast<CartSessionDto>()
            .OrderByDescending(s => s.UpdatedAt)
            .ToList();

        // Persist cleaned list (remove IDs for deleted sessions)
        var validIds = all.Select(s => s.Id).ToList();
        if (validIds.Count != trackedIds.Count)
            await localStorage.SetItemAsync(StorageKey, validIds);

        // Date filters
        if (filters != null)
        {
            if (filters.TryGetValue("From", out var rawFrom) && rawFrom is DateTime from)
                all = all.Where(s => s.UpdatedAt.ToLocalTime().Date >= from.Date).ToList();
            if (filters.TryGetValue("To", out var rawTo) && rawTo is DateTime to)
                all = all.Where(s => s.UpdatedAt.ToLocalTime().Date <= to.Date).ToList();
            if (filters.TryGetValue("SalesChannel", out var rawChannel) && rawChannel is string channelFilter && !string.IsNullOrWhiteSpace(channelFilter))
                all = all.Where(s => (s.SalesChannel ?? string.Empty).Contains(channelFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Text search: sales channel or session ID prefix
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToUpperInvariant();
            all = all.Where(s =>
                (s.SalesChannel ?? string.Empty).ToUpperInvariant().Contains(term) ||
                s.Id.ToString().StartsWith(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var totalCount = all.Count;
        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<CartSessionDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>Removes the session from local tracking (does not delete server-side).</summary>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var trackedIds = await localStorage.GetItemAsync<List<Guid>>(StorageKey) ?? [];
        trackedIds.RemoveAll(x => x == id);
        await localStorage.SetItemAsync(StorageKey, trackedIds);
    }

    /// <summary>Creates a new session and adds it to local tracking.</summary>
    public async Task<CartSessionDto> CreateSessionAsync(string salesChannel, CancellationToken ct = default)
    {
        var session = await cartService.CreateSessionAsync(new CreateCartSessionDto
        {
            SalesChannel = string.IsNullOrWhiteSpace(salesChannel) ? "POS" : salesChannel.Trim(),
            Currency = "EUR"
        }, ct);

        await TrackSessionAsync(session.Id);
        return session;
    }

    /// <summary>Clears all items from a session.</summary>
    public Task<CartSessionDto?> ClearSessionAsync(Guid id, CancellationToken ct = default)
        => cartService.ClearSessionAsync(id, ct);

    /// <summary>Loads a session by ID and adds it to local tracking.</summary>
    public async Task<CartSessionDto?> LoadByIdAsync(Guid id, CancellationToken ct = default)
    {
        var session = await cartService.GetSessionAsync(id, ct);
        if (session is not null)
            await TrackSessionAsync(id);
        return session;
    }

    private async Task TrackSessionAsync(Guid sessionId)
    {
        var trackedIds = await localStorage.GetItemAsync<List<Guid>>(StorageKey) ?? [];
        trackedIds.RemoveAll(id => id == sessionId);
        trackedIds.Insert(0, sessionId);
        trackedIds = trackedIds.Distinct().Take(MaxTrackedSessions).ToList();
        await localStorage.SetItemAsync(StorageKey, trackedIds);
    }
}
