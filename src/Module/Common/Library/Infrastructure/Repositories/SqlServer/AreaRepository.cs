using System.Collections.Generic;
using System.Threading.Tasks;
using Nm.Lib.Data.Abstractions;
using Nm.Lib.Data.Core;
using Nm.Lib.Data.Query;
using Nm.Lib.Utils.Core.Extensions;
using Nm.Module.Common.Domain.Area;
using Nm.Module.Common.Domain.Area.Models;

namespace Nm.Module.Common.Infrastructure.Repositories.SqlServer
{
    public class AreaRepository : RepositoryAbstract<AreaEntity>, IAreaRepository
    {
        public AreaRepository(IDbContext context) : base(context)
        {
        }

        public async Task<IList<AreaEntity>> Query(AreaQueryModel model)
        {
            var paging = model.Paging();

            var query = Db.Find(m => m.ParentId == model.ParentId);
            query.WhereIf(model.Name.NotNull(), m => m.Name.Contains(model.Name));

            var result = await query.PaginationAsync(paging);

            model.TotalCount = paging.TotalCount;

            return result;
        }

        public Task<IList<AreaEntity>> QueryChildren(int parentId)
        {
            return Db.Find(m => m.ParentId == parentId).ToListAsync();
        }

        public Task<bool> Exists(AreaEntity entity)
        {
            var query = Db.Find(m => m.ParentId == entity.ParentId );
            query.Where(m => m.Name == entity.Name || m.Code == entity.Code);
            query.WhereIf(entity.Id > 0, m => m.Id != entity.Id);

            return query.ExistsAsync();
        }
    }
}
