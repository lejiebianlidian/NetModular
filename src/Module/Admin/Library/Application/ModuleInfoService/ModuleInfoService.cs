﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Nm.Lib.Data.Abstractions;
using Nm.Lib.Module.Abstractions;
using Nm.Lib.Utils.Core.Result;
using Nm.Module.Admin.Domain.Menu;
using Nm.Module.Admin.Domain.ModuleInfo;
using Nm.Module.Admin.Domain.ModuleInfo.Models;
using Nm.Module.Admin.Domain.Permission;
using Nm.Module.Admin.Infrastructure.Repositories;
using ModuleInfoEntity = Nm.Module.Admin.Domain.ModuleInfo.ModuleInfoEntity;

namespace Nm.Module.Admin.Application.ModuleInfoService
{
    public class ModuleInfoService : IModuleInfoService
    {
        private readonly IModuleInfoRepository _repository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IMenuRepository _menuRepository;
        private readonly IUnitOfWork _uow;
        private readonly IModuleCollection _moduleCollection;

        public ModuleInfoService(IModuleInfoRepository repository, IPermissionRepository permissionRepository, IMenuRepository menuRepository, IUnitOfWork<AdminDbContext> uow, IModuleCollection moduleCollection)
        {
            _repository = repository;
            _permissionRepository = permissionRepository;
            _menuRepository = menuRepository;
            _uow = uow;
            _moduleCollection = moduleCollection;
        }

        public async Task<IResultModel> Query(ModuleInfoQueryModel model)
        {
            var result = new QueryResultModel<ModuleInfoEntity>
            {
                Rows = await _repository.Query(model),
                Total = model.TotalCount
            };
            return ResultModel.Success(result);
        }

        public async Task<IResultModel> Sync()
        {
            var modules = _moduleCollection.Select(m => new ModuleInfoEntity
            {
                Name = m.Name,
                Code = m.Id,
                Version = m.Version
            });

            _uow.BeginTransaction();

            foreach (var moduleInfo in modules)
            {
                if (!await _repository.Exists(moduleInfo.Code))
                {
                    if (!await _repository.AddAsync(moduleInfo))
                    {
                        _uow.Rollback();
                        return ResultModel.Failed();
                    }
                }
                else
                {
                    if (!await _repository.UpdateByCode(moduleInfo))
                    {
                        _uow.Rollback();
                        return ResultModel.Failed();
                    }
                }
            }

            _uow.Commit();

            return ResultModel.Success();
        }

        public async Task<IResultModel> Delete(Guid id)
        {
            var entity = await _repository.GetAsync(id);
            if (entity == null)
                return ResultModel.NotExists;

            var exists = await _permissionRepository.ExistsWidthModule(entity.Code);
            if (exists)
                return ResultModel.Failed("有权限绑定了该模块，请先删除绑定关系");

            exists = await _menuRepository.ExistsWidthModule(entity.Code);
            if (exists)
                return ResultModel.Failed("有菜单绑定了该模块，请先删除绑定关系");

            var result = await _repository.DeleteAsync(id);
            return ResultModel.Result(result);
        }

        public async Task<IResultModel> Select()
        {
            var all = await _repository.GetAllAsync();
            var list = all.Select(m => new OptionResultModel
            {
                Label = m.Name,
                Value = m.Code
            }).ToList();
            return ResultModel.Success(list);
        }

    }
}
