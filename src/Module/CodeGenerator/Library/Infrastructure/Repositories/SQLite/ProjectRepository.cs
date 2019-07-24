﻿using Nm.Lib.Data.Abstractions;

namespace Nm.Module.CodeGenerator.Infrastructure.Repositories.SQLite
{
    public class ProjectRepository : SqlServer.ProjectRepository
    {
        public ProjectRepository(IDbContext context) : base(context)
        {
        }
    }
}
