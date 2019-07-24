﻿using System;
using System.Threading.Tasks;
using Nm.Lib.Utils.Core.Result;
using Nm.Module.CodeGenerator.Application.EnumService.ViewModels;
using Nm.Module.CodeGenerator.Domain.Enum.Models;

namespace Nm.Module.CodeGenerator.Application.EnumService
{
    /// <summary>
    /// 枚举服务接口
    /// </summary>
    public interface IEnumService
    {
        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<IResultModel> Query(EnumQueryModel model);

        /// <summary>
        /// 创建
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<IResultModel> Add(EnumAddModel model);

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns></returns>
        Task<IResultModel> Delete(Guid id);

        /// <summary>
        /// 编辑
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<IResultModel> Edit(Guid id);

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<IResultModel> Update(EnumUpdateModel model);

        /// <summary>
        /// 下拉列表
        /// </summary>
        /// <returns></returns>
        Task<IResultModel> Select();
    }
}
