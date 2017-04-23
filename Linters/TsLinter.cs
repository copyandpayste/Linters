using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linters
{
    class TsLinter : Linter
    {
        public TsLinter(LintErrorProvider provider)
        {
            Provider = provider;
            Name = "TSLint";
            Command = "tslint --help";
            ConfigFileName = "tslint.json";
            IsEnabled = true;
        }

        protected override void ParseErrors(string output)
        {
            JArray array = (JArray)JsonConvert.DeserializeObject(output);

            foreach (JObject obj in array)
            {
                string fileName = obj["name"]?.Value<string>().Replace("/", "\\");

                if (string.IsNullOrEmpty(fileName))
                    continue;

                var error = new ErrorTask();
                error.Text = obj["failure"]?.Value<string>();
                error.Line = obj["startPosition"]?["line"]?.Value<int>() ?? 0;
                error.Column = obj["startPosition"]?["character"]?.Value<int>() ?? 0;
                error.Document = fileName;
                error.ErrorCategory = TaskErrorCategory.Warning;
                error.HelpKeyword = obj["ruleName"]?.Value<string>();
                //error.HelpLink = $"https://github.com/palantir/tslint?rule={le.ErrorCode}#supported-rules";
                Provider.Tasks.Add(error);
                Provider.Show();
            }
        }
    }
}
