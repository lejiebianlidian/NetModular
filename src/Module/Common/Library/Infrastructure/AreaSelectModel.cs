﻿using System.Collections.Generic;

namespace Nm.Module.Common.Infrastructure
{
    /// <summary>
    /// 区域选择视图模型
    /// </summary>
    public class AreaSelectModel : List<AreaSelectOptionModel>
    {
        /// <summary>
        /// 省
        /// </summary>
        public AreaSelectOptionModel Province => Count > 0 ? this[0] : null;

        /// <summary>
        /// 市
        /// </summary>
        public AreaSelectOptionModel City => Count > 1 ? this[1] : null;

        /// <summary>
        /// 区县
        /// </summary>
        public AreaSelectOptionModel Area => Count > 2 ? this[2] : null;

        /// <summary>
        /// 街道
        /// </summary>
        public AreaSelectOptionModel Town => Count > 3 ? this[3] : null;
    }

    /// <summary>
    /// 区域项
    /// </summary>
    public class AreaSelectOptionModel
    {
        /// <summary>
        /// ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 值
        /// </summary>
        public string Value { get; set; }
    }
}