using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Infrastructure;
namespace Application.Features;
public class ModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        if(context is WmsDbContext wmsDbContext)
            return (wmsDbContext.GetType(), wmsDbContext._schemaName, designTime);

        return (context.GetType(), designTime);
    }
}