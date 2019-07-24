﻿using Nm.Module.CodeGenerator.Infrastructure.Templates.Models;

namespace Nm.Module.CodeGenerator.Infrastructure.Templates.Default
{
    public class DefaultTemplateBuilder : TemplateBuilderAbstract
    {
        public DefaultTemplateBuilder() : base("Default")
        {

        }

        public override void Build(TemplateBuildModel model)
        {
            base.Build(model);
        }
    }
}
