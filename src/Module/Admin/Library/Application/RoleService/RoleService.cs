﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Nm.Lib.Data.Abstractions;
using Nm.Lib.Utils.Core.Extensions;
using Nm.Lib.Utils.Core.Result;
using Nm.Module.Admin.Application.AccountService;
using Nm.Module.Admin.Application.RoleService.ResultModels;
using Nm.Module.Admin.Application.RoleService.ViewModels;
using Nm.Module.Admin.Domain.AccountRole;
using Nm.Module.Admin.Domain.Button;
using Nm.Module.Admin.Domain.Role;
using Nm.Module.Admin.Domain.Role.Models;
using Nm.Module.Admin.Domain.RoleMenu;
using Nm.Module.Admin.Domain.RoleMenuButton;
using Nm.Module.Admin.Infrastructure.Repositories;

namespace Nm.Module.Admin.Application.RoleService
{
    public class RoleService : IRoleService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _uow;
        private readonly IRoleRepository _repository;
        private readonly IRoleMenuRepository _roleMenuRepository;
        private readonly IRoleMenuButtonRepository _roleMenuButtonRepository;
        private readonly IButtonRepository _buttonRepository;
        private readonly IAccountRoleRepository _accountRoleRepository;
        private readonly IAccountService _accountService;

        public RoleService(IMapper mapper, IUnitOfWork<AdminDbContext> uow, IRoleRepository repository, IRoleMenuRepository roleMenuRepository, IRoleMenuButtonRepository roleMenuButtonRepository, IButtonRepository buttonRepository, IAccountRoleRepository accountRoleRepository, IAccountService accountService)
        {
            _mapper = mapper;
            _uow = uow;
            _repository = repository;
            _roleMenuRepository = roleMenuRepository;
            _roleMenuButtonRepository = roleMenuButtonRepository;
            _buttonRepository = buttonRepository;
            _accountRoleRepository = accountRoleRepository;
            _accountService = accountService;
        }

        public async Task<IResultModel> Query(RoleQueryModel model)
        {
            var result = new QueryResultModel<RoleEntity>
            {
                Rows = await _repository.Query(model),
                Total = model.TotalCount
            };
            return ResultModel.Success(result);
        }

        public async Task<IResultModel> Add(RoleAddModel model)
        {
            if (await _repository.Exists(model.Name))
                return ResultModel.HasExists;

            var entity = _mapper.Map<RoleEntity>(model);

            var result = await _repository.AddAsync(entity);

            return ResultModel.Result(result);
        }

        public async Task<IResultModel> Delete(Guid id)
        {
            var exist = await _repository.ExistsAsync(id);
            if (!exist)
                return ResultModel.Failed("角色不存在");

            exist = await _accountRoleRepository.ExistsByRole(id);
            if (exist)
                return ResultModel.Failed("有账户绑定了该角色，请先删除对应绑定关系");

            _uow.BeginTransaction();
            var result = await _repository.SoftDeleteAsync(id);
            if (result)
            {
                result = await _roleMenuRepository.DeleteByRoleId(id);
                if (result)
                {
                    result = await _roleMenuButtonRepository.DeleteByRole(id);
                    if (result)
                    {
                        _uow.Commit();
                    }
                }
            }
            return ResultModel.Result(result);
        }

        public async Task<IResultModel> Edit(Guid id)
        {
            var entity = await _repository.GetAsync(id);
            if (entity == null)
                return ResultModel.NotExists;

            var model = _mapper.Map<RoleUpdateModel>(entity);
            return ResultModel.Success(model);
        }

        public async Task<IResultModel> Update(RoleUpdateModel model)
        {
            if (await _repository.Exists(model.Name, model.Id))
                return ResultModel.HasExists;

            var role = await _repository.GetAsync(model.Id);
            _mapper.Map(model, role);

            var result = await _repository.UpdateAsync(role);

            return ResultModel.Result(result);
        }

        public async Task<IResultModel> MenuList(Guid id)
        {
            var exists = await _repository.ExistsAsync(id);
            if (!exists)
                return ResultModel.NotExists;

            var list = await _roleMenuRepository.GetByRoleId(id);
            return ResultModel.Success(list);
        }

        public async Task<IResultModel> BindMenu(RoleMenuBindModel model)
        {
            var exists = await _repository.ExistsAsync(model.Id);
            if (!exists)
                return ResultModel.NotExists;

            List<RoleMenuEntity> entityList = null;
            if (model.Menus != null && model.Menus.Any())
            {
                entityList = model.Menus.Select(m => new RoleMenuEntity { RoleId = model.Id, MenuId = m }).ToList();
            }

            /*
             * 操作逻辑
             * 1、清除已有的绑定数据
             * 2、添加新的绑定数据
             */
            _uow.BeginTransaction();

            var clear = await _roleMenuRepository.DeleteByRoleId(model.Id);
            if (clear)
            {
                if (entityList == null || !entityList.Any() || await _roleMenuRepository.AddAsync(entityList))
                {
                    _uow.Commit();

                    await ClearAccountPermissionCache(model.Id);

                    return ResultModel.Success();
                }
            }

            _uow.Rollback();
            return ResultModel.Failed();
        }

        public async Task<IResultModel> MenuButtonList(Guid id, Guid menuId)
        {
            var exists = await _repository.ExistsAsync(id);
            if (!exists)
                return ResultModel.NotExists;

            var list = new List<RoleMenuButtonModel>();
            var data = await _roleMenuButtonRepository.Query(id, menuId);
            if (data.Any())
            {
                foreach (var button in data)
                {
                    list.Add(new RoleMenuButtonModel
                    {
                        Id = button.Id,
                        Name = button.Name,
                        Checked = button.RoleId != Guid.Empty
                    });
                }
            }

            return ResultModel.Success(list);
        }

        public async Task<IResultModel> BindMenuButton(RoleMenuButtonBindModel model)
        {
            var exists = await _repository.ExistsAsync(model.RoleId);
            if (!exists)
                return ResultModel.NotExists;

            bool result;
            if (model.ButtonId.NotEmpty())
            {
                #region ==单个按钮==

                var entity = _mapper.Map<RoleMenuButtonEntity>(model);
                //如果已存在
                if (await _roleMenuButtonRepository.Exists(entity))
                {
                    if (model.Checked)
                    {
                        return ResultModel.Success();
                    }

                    result = await _roleMenuButtonRepository.Delete(entity);

                    await ClearAccountPermissionCache(model.RoleId);

                    return ResultModel.Result(result);
                }

                if (!model.Checked)
                    return ResultModel.Success();

                result = await _roleMenuButtonRepository.AddAsync(entity);

                await ClearAccountPermissionCache(model.RoleId);

                return ResultModel.Result(result);

                #endregion
            }


            #region ==批量添加指定菜单的所有按钮==

            _uow.BeginTransaction();
            result = await _roleMenuButtonRepository.Delete(model.RoleId, model.MenuId);
            if (result)
            {
                if (model.Checked)
                {
                    var buttons = await _buttonRepository.QueryByMenu(model.MenuId);
                    var entities = buttons.Select(m => new RoleMenuButtonEntity
                    {
                        RoleId = model.RoleId,
                        MenuId = model.MenuId,
                        ButtonId = m.Id
                    }).ToList();

                    if (await _roleMenuButtonRepository.AddAsync(entities))
                    {
                        _uow.Commit();
                        await ClearAccountPermissionCache(model.RoleId);

                        return ResultModel.Success();
                    }
                }
                else
                {
                    _uow.Commit();
                    await ClearAccountPermissionCache(model.RoleId);

                    return ResultModel.Success();
                }
            }
            _uow.Rollback();
            return ResultModel.Failed();

            #endregion
        }

        public async Task<IResultModel> Select()
        {
            var all = await _repository.GetAllAsync();
            var list = all.Select(m => new OptionResultModel
            {
                Label = m.Name,
                Value = m.Id
            }).ToList();

            return ResultModel.Success(list);
        }

        /// <summary>
        /// 清除角色关联账户的权限缓存
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        private async Task ClearAccountPermissionCache(Guid roleId)
        {
            var relationList = await _accountRoleRepository.QueryByRole(roleId);
            if (relationList.Any())
            {
                foreach (var relation in relationList)
                {
                    _accountService.ClearPermissionListCache(relation.AccountId);
                }
            }
        }
    }
}
