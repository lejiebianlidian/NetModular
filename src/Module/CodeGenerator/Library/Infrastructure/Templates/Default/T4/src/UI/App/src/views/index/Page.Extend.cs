﻿using System.IO;
using System.Linq;
using Nm.Module.CodeGenerator.Infrastructure.Templates.Models;

namespace Nm.Module.CodeGenerator.Infrastructure.Templates.Default.T4.src.UI.App.src.views.index
{
    public partial class Page : ITemplateHandler
    {
        private readonly TemplateBuildModel _model;
        private ClassBuildModel _class;

        public Page(TemplateBuildModel model)
        {
            _model = model;
        }

        public void Save()
        {
            if (_model.Project.ClassList != null && _model.Project.ClassList.Any())
            {
                foreach (var classModel in _model.Project.ClassList)
                {
                    _class = classModel;

                    var dir = Path.Combine(_model.RootPath, _model.Project.Code, $"src/UI/{_model.Project.WebUIDicName}/src/views", _class.Name, "index");
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    //清空
                    GenerationEnvironment.Clear();

                    var content = TransformText();

                    var filePath = Path.Combine(dir, $"page.js");
                    File.WriteAllText(filePath, content);
                }
            }
        }
    }
}
