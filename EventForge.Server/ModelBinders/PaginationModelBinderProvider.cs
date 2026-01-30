using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace EventForge.Server.ModelBinders;

public class PaginationModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.Metadata.ModelType == typeof(PaginationParameters))
        {
            return new BinderTypeModelBinder(typeof(PaginationModelBinder));
        }

        return null;
    }
}
