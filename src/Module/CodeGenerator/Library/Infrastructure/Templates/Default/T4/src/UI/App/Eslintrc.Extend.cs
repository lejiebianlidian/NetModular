﻿using System.IO;
using Nm.Module.CodeGenerator.Infrastructure.Templates.Models;

namespace Nm.Module.CodeGenerator.Infrastructure.Templates.Default.T4.src.UI.App
{
    public partial class Eslintrc : ITemplateHandler
    {
        private readonly TemplateBuildModel _model;

        public Eslintrc(TemplateBuildModel model)
        {
            _model = model;
        }

        public void Save()
        {
            var dir = Path.Combine(_model.RootPath, _model.Project.Code, $"src/UI/{_model.Project.WebUIDicName}");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var content = TransformText();
            var filePath = Path.Combine(dir, ".eslintrc.js");
            File.WriteAllText(filePath, content);
        }
    }
}
