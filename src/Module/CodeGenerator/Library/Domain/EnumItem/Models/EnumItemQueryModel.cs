﻿using System;
using System.ComponentModel.DataAnnotations;
using Nm.Lib.Data.Query;

namespace Nm.Module.CodeGenerator.Domain.EnumItem.Models
{
    public class EnumItemQueryModel : QueryModel
    {
        /// <summary>
        /// 枚举编号
        /// </summary>
        [Required(ErrorMessage = "请选择枚举")]
        public Guid EnumId { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
    }
}
