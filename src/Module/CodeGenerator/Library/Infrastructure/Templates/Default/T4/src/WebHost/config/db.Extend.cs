﻿using System.IO;
using Nm.Module.CodeGenerator.Infrastructure.Templates.Models;

namespace Nm.Module.CodeGenerator.Infrastructure.Templates.Default.T4.src.WebHost.config
{
    public partial class db : ITemplateHandler
    {
        private readonly TemplateBuildModel _model;
        private readonly string _prefix;

        public db(TemplateBuildModel model)
        {
            _model = model;
            _prefix = model.Project.Prefix;
        }

        public void Save()
        {
            var dir = Path.Combine(_model.RootPath, _model.Project.Code, "src/WebHost/config");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var content = TransformText();
            var filePath = Path.Combine(dir, "db.json");
            File.WriteAllText(filePath, content);
        }
    }
}
