﻿using NetModular.Lib.Data.Abstractions;

namespace NetModular.Module.Admin.Infrastructure.Repositories.MySql
{
    public class RolePlatformPermissionRepository : SqlServer.RolePlatformPermissionRepository
    {
        public RolePlatformPermissionRepository(IDbContext context) : base(context)
        {
        }
    }
}
