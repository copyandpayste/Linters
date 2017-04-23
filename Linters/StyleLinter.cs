﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Linters
{
    class StyleLinter : Linter
    {
        public StyleLinter(LintErrorProvider provider)
        {
            Provider = provider;
            Name = "StyleLint";
            Command = "stylelint **.scss --formatter json";
            ConfigFileName = ".stylelintrc";
            IsEnabled = true;
        }

        protected override void ParseErrors(string output)
        {
            JArray array = (JArray)JsonConvert.DeserializeObject(output);

            foreach (JObject obj in array)
            {
                string fileName = obj["source"]?.Value<string>().Replace("/", "\\");

                if (string.IsNullOrEmpty(fileName))
                    continue;

                JArray warnings = obj["warnings"]?.Value<JArray>();
                foreach (JObject warning in warnings)
                {
                    var error = new ErrorTask()
                    {
                        Text = warning["text"]?.Value<string>(),
                        Line = warning["line"]?.Value<int>() ?? 0,
                        Column = warning["column"]?.Value<int>() ?? 0,
                        Document = fileName,
                        ErrorCategory = TaskErrorCategory.Warning,
                        HelpKeyword = warning["rule"]?.Value<string>()
                    };
                    error.Navigate += Provider.OnTaskNavigate;
                    //error.HelpLink = $"https://github.com/palantir/tslint?rule={le.ErrorCode}#supported-rules";
                    Provider.Tasks.Add(error);
                }
            }
        }
    }
}
