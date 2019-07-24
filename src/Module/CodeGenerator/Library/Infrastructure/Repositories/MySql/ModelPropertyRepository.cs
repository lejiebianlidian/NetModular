﻿using Nm.Lib.Data.Abstractions;

namespace Nm.Module.CodeGenerator.Infrastructure.Repositories.MySql
{
    public class ModelPropertyRepository : SqlServer.ModelPropertyRepository
    {
        public ModelPropertyRepository(IDbContext context) : base(context)
        {
        }
    }
}
