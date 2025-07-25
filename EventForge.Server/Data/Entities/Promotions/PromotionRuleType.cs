namespace EventForge.Server.Data.Entities.Promotions;


/// <summary>
/// Types of promotion rules.
/// </summary>
public enum PromotionRuleType
{
    Discount,           // Percentage or fixed discount
    BuyXGetY,           // Buy X get Y free or discounted
    FixedPrice,         // Fixed price for a set of products
    Bundle,             // Bundle of products at special price
    CartAmountDiscount, // Discount if cart total exceeds a threshold
    CategoryDiscount,   // Discount on a product category
    CustomerSpecific,   // Discount for specific customers/groups
    Coupon,             // Requires coupon code
    TimeLimited,        // Valid only in certain time slots
    Exclusive           // Not combinable with other promotions
}