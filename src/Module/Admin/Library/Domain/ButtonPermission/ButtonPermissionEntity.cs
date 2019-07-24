﻿using System;
using Nm.Lib.Data.Abstractions.Attributes;
using Nm.Lib.Data.Core.Entities;

namespace Nm.Module.Admin.Domain.ButtonPermission
{
    /// <summary>
    /// 按钮权限
    /// </summary>
    [Table("Button_Permission")]
    public class ButtonPermissionEntity : Entity<int>
    {
        /// <summary>
        /// 按钮编号
        /// </summary>
        public Guid ButtonId { get; set; }

        /// <summary>
        /// 权限编号
        /// </summary>
        public Guid PermissionId { get; set; }
    }
}
