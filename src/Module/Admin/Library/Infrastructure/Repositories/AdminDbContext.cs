﻿using Nm.Lib.Data.Abstractions;
using Nm.Lib.Data.Core;

namespace Nm.Module.Admin.Infrastructure.Repositories
{
    public class AdminDbContext : DbContext
    {
        public AdminDbContext(IDbContextOptions options) : base(options)
        {
        }
    }
}
