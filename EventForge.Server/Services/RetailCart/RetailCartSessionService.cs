using EventForge.DTOs.Promotions;
using EventForge.DTOs.RetailCart;
using EventForge.Server.Services.Promotions;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace EventForge.Server.Services.RetailCart
{
    /// <summary>
    /// In-memory implementation of retail cart session service.
    /// Provides tenant-aware, user-scoped cart storage with promotion integration.
    /// </summary>
    public class RetailCartSessionService : IRetailCartSessionService
    {
        private readonly ITenantContext _tenantContext;
        private readonly IPromotionService _promotionService;
        private readonly ILogger<RetailCartSessionService> _logger;
        private readonly IMemoryCache _cache;

        // In-memory storage - in production this would be replaced with persistent storage
        private static readonly ConcurrentDictionary<string, CartSession> _sessions = new();

        public RetailCartSessionService(
            ITenantContext tenantContext,
            IPromotionService promotionService,
            ILogger<RetailCartSessionService> logger,
            IMemoryCache cache)
        {
            _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
            _promotionService = promotionService ?? throw new ArgumentNullException(nameof(promotionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<CartSessionDto> CreateSessionAsync(CreateCartSessionDto createDto, CancellationToken cancellationToken = default)
        {
            var tenantId = GetCurrentTenantId();
            var sessionId = Guid.NewGuid();

            var session = new CartSession
            {
                Id = sessionId,
                TenantId = tenantId,
                CustomerId = createDto.CustomerId,
                SalesChannel = createDto.SalesChannel,
                Currency = createDto.Currency,
                Items = new List<CartSessionItem>(),
                CouponCodes = new List<string>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var sessionKey = GetSessionKey(tenantId, sessionId);
            _sessions[sessionKey] = session;

            _logger.LogInformation("Created cart session {SessionId} for tenant {TenantId}", sessionId, tenantId);

            return await MapToDto(session, cancellationToken);
        }

        public async Task<CartSessionDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            var session = GetSession(sessionId);
            if (session == null)
                return null;

            return await MapToDto(session, cancellationToken);
        }

        public async Task<CartSessionDto?> AddItemAsync(Guid sessionId, AddCartItemDto addItemDto, CancellationToken cancellationToken = default)
        {
            var session = GetSession(sessionId);
            if (session == null)
                return null;

            // Check if item already exists
            var existingItem = session.Items.FirstOrDefault(i => i.ProductId == addItemDto.ProductId);
            if (existingItem != null)
            {
                existingItem.Quantity += addItemDto.Quantity;
            }
            else
            {
                var newItem = new CartSessionItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = addItemDto.ProductId,
                    ProductCode = addItemDto.ProductCode,
                    ProductName = addItemDto.ProductName,
                    UnitPrice = addItemDto.UnitPrice,
                    Quantity = addItemDto.Quantity,
                    CategoryIds = addItemDto.CategoryIds
                };
                session.Items.Add(newItem);
            }

            session.UpdatedAt = DateTime.UtcNow;

            _logger.LogDebug("Added item {ProductId} (qty: {Quantity}) to cart session {SessionId}",
                addItemDto.ProductId, addItemDto.Quantity, sessionId);

            return await RecalculateAndMapToDto(session, cancellationToken);
        }

        public async Task<CartSessionDto?> RemoveItemAsync(Guid sessionId, Guid itemId, CancellationToken cancellationToken = default)
        {
            var session = GetSession(sessionId);
            if (session == null)
                return null;

            var item = session.Items.FirstOrDefault(i => i.Id == itemId);
            if (item != null)
            {
                _ = session.Items.Remove(item);
                session.UpdatedAt = DateTime.UtcNow;

                _logger.LogDebug("Removed item {ItemId} from cart session {SessionId}", itemId, sessionId);
            }

            return await RecalculateAndMapToDto(session, cancellationToken);
        }

        public async Task<CartSessionDto?> UpdateItemQuantityAsync(Guid sessionId, Guid itemId, UpdateCartItemDto updateDto, CancellationToken cancellationToken = default)
        {
            var session = GetSession(sessionId);
            if (session == null)
                return null;

            var item = session.Items.FirstOrDefault(i => i.Id == itemId);
            if (item != null)
            {
                if (updateDto.Quantity <= 0)
                {
                    _ = session.Items.Remove(item);
                    _logger.LogDebug("Removed item {ItemId} from cart session {SessionId} (quantity set to {Quantity})",
                        itemId, sessionId, updateDto.Quantity);
                }
                else
                {
                    item.Quantity = updateDto.Quantity;
                    _logger.LogDebug("Updated item {ItemId} quantity to {Quantity} in cart session {SessionId}",
                        itemId, updateDto.Quantity, sessionId);
                }

                session.UpdatedAt = DateTime.UtcNow;
            }

            return await RecalculateAndMapToDto(session, cancellationToken);
        }

        public async Task<CartSessionDto?> ApplyCouponsAsync(Guid sessionId, ApplyCouponsDto applyCouponsDto, CancellationToken cancellationToken = default)
        {
            var session = GetSession(sessionId);
            if (session == null)
                return null;

            session.CouponCodes = applyCouponsDto.CouponCodes?.Where(c => !string.IsNullOrWhiteSpace(c)).ToList() ?? new List<string>();
            session.UpdatedAt = DateTime.UtcNow;

            _logger.LogDebug("Applied {CouponCount} coupons to cart session {SessionId}",
                session.CouponCodes.Count, sessionId);

            return await RecalculateAndMapToDto(session, cancellationToken);
        }

        public async Task<CartSessionDto?> ClearAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            var session = GetSession(sessionId);
            if (session == null)
                return null;

            session.Items.Clear();
            session.CouponCodes.Clear();
            session.UpdatedAt = DateTime.UtcNow;

            _logger.LogDebug("Cleared cart session {SessionId}", sessionId);

            return await RecalculateAndMapToDto(session, cancellationToken);
        }

        public async Task<CartSessionDto?> GetTotalsAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            var session = GetSession(sessionId);
            if (session == null)
                return null;

            return await RecalculateAndMapToDto(session, cancellationToken);
        }

        #region Private Methods

        private Guid GetCurrentTenantId()
        {
            var tenantId = _tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required for cart session operations");
            return tenantId.Value;
        }

        private string GetSessionKey(Guid tenantId, Guid sessionId)
        {
            return $"{tenantId}:{sessionId}";
        }

        private CartSession? GetSession(Guid sessionId)
        {
            var tenantId = GetCurrentTenantId();
            var sessionKey = GetSessionKey(tenantId, sessionId);

            return _sessions.TryGetValue(sessionKey, out var session) ? session : null;
        }

        private async Task<CartSessionDto> RecalculateAndMapToDto(CartSession session, CancellationToken cancellationToken)
        {
            // Apply promotions if there are items
            if (session.Items.Any())
            {
                var applyDto = new ApplyPromotionRulesDto
                {
                    CartItems = session.Items.Select(item => new CartItemDto
                    {
                        ProductId = item.ProductId,
                        ProductCode = item.ProductCode,
                        ProductName = item.ProductName,
                        UnitPrice = item.UnitPrice,
                        Quantity = item.Quantity,
                        CategoryIds = item.CategoryIds,
                        ExistingLineDiscount = 0m
                    }).ToList(),
                    CustomerId = session.CustomerId,
                    SalesChannel = session.SalesChannel,
                    CouponCodes = session.CouponCodes,
                    OrderDateTime = DateTime.UtcNow,
                    Currency = session.Currency
                };

                try
                {
                    var promotionResult = await _promotionService.ApplyPromotionRulesAsync(applyDto, cancellationToken);
                    return MapSessionWithPromotions(session, promotionResult);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to apply promotions to cart session {SessionId}, returning without promotions", session.Id);
                    return MapSessionWithoutPromotions(session);
                }
            }

            return MapSessionWithoutPromotions(session);
        }

        private async Task<CartSessionDto> MapToDto(CartSession session, CancellationToken cancellationToken)
        {
            return await RecalculateAndMapToDto(session, cancellationToken);
        }

        private CartSessionDto MapSessionWithPromotions(CartSession session, PromotionApplicationResultDto promotionResult)
        {
            var dto = new CartSessionDto
            {
                Id = session.Id,
                CustomerId = session.CustomerId,
                SalesChannel = session.SalesChannel,
                Currency = session.Currency,
                CouponCodes = session.CouponCodes,
                OriginalTotal = promotionResult.OriginalTotal,
                FinalTotal = promotionResult.FinalTotal,
                TotalDiscountAmount = promotionResult.TotalDiscountAmount,
                AppliedPromotions = promotionResult.AppliedPromotions,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt
            };

            // Map items with promotion results
            dto.Items = session.Items.Select((item, index) =>
            {
                var promotionItem = index < promotionResult.CartItems.Count ? promotionResult.CartItems[index] : null;
                return new CartSessionItemDto
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductCode = item.ProductCode,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    CategoryIds = item.CategoryIds,
                    OriginalLineTotal = promotionItem?.OriginalLineTotal ?? (item.UnitPrice * item.Quantity),
                    FinalLineTotal = promotionItem?.FinalLineTotal ?? (item.UnitPrice * item.Quantity),
                    PromotionDiscount = promotionItem?.PromotionDiscount ?? 0m,
                    EffectiveDiscountPercentage = promotionItem?.EffectiveDiscountPercentage ?? 0m,
                    AppliedPromotions = promotionItem?.AppliedPromotions ?? new List<AppliedPromotionDto>()
                };
            }).ToList();

            return dto;
        }

        private CartSessionDto MapSessionWithoutPromotions(CartSession session)
        {
            var originalTotal = session.Items.Sum(i => i.UnitPrice * i.Quantity);

            return new CartSessionDto
            {
                Id = session.Id,
                CustomerId = session.CustomerId,
                SalesChannel = session.SalesChannel,
                Currency = session.Currency,
                CouponCodes = session.CouponCodes,
                OriginalTotal = originalTotal,
                FinalTotal = originalTotal,
                TotalDiscountAmount = 0m,
                AppliedPromotions = new List<AppliedPromotionDto>(),
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt,
                Items = session.Items.Select(item => new CartSessionItemDto
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductCode = item.ProductCode,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    CategoryIds = item.CategoryIds,
                    OriginalLineTotal = item.UnitPrice * item.Quantity,
                    FinalLineTotal = item.UnitPrice * item.Quantity,
                    PromotionDiscount = 0m,
                    EffectiveDiscountPercentage = 0m,
                    AppliedPromotions = new List<AppliedPromotionDto>()
                }).ToList()
            };
        }

        #endregion

        #region Internal Data Models

        private class CartSession
        {
            public Guid Id { get; set; }
            public Guid TenantId { get; set; }
            public Guid? CustomerId { get; set; }
            public string? SalesChannel { get; set; }
            public string Currency { get; set; } = "EUR";
            public List<CartSessionItem> Items { get; set; } = new();
            public List<string> CouponCodes { get; set; } = new();
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
        }

        private class CartSessionItem
        {
            public Guid Id { get; set; }
            public Guid ProductId { get; set; }
            public string? ProductCode { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public decimal UnitPrice { get; set; }
            public int Quantity { get; set; }
            public List<Guid>? CategoryIds { get; set; }
        }

        #endregion
    }
}