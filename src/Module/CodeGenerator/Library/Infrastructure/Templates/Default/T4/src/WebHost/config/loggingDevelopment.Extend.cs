﻿using System.IO;
using Nm.Module.CodeGenerator.Infrastructure.Templates.Models;

namespace Nm.Module.CodeGenerator.Infrastructure.Templates.Default.T4.src.WebHost.config
{
    public partial class loggingDevelopment : ITemplateHandler
    {
        private readonly TemplateBuildModel _model;

        public loggingDevelopment(TemplateBuildModel model)
        {
            _model = model;
        }

        public void Save()
        {
            var dir = Path.Combine(_model.RootPath, _model.Project.Code, "src/WebHost/config");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var content = TransformText();
            var filePath = Path.Combine(dir, "logging.Development.json");
            File.WriteAllText(filePath, content);
        }
    }
}
