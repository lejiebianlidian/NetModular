﻿using System;
using System.Threading.Tasks;
using Nm.Lib.Data.Abstractions;
using Nm.Lib.Data.Core;
using Nm.Module.CodeGenerator.Domain.ClassMethod;

namespace Nm.Module.CodeGenerator.Infrastructure.Repositories.SqlServer
{
    public class ClassMethodRepository : RepositoryAbstract<ClassMethodEntity>, IClassMethodRepository
    {
        public ClassMethodRepository(IDbContext context) : base(context)
        {

        }

        public Task<bool> DeleteByClass(Guid classId)
        {
            return Db.Find(m => m.ClassId == classId).DeleteAsync();
        }

        public Task<ClassMethodEntity> GetByClass(Guid classId)
        {
            return Db.Find(m => m.ClassId == classId).FirstAsync();
        }
    }
}
