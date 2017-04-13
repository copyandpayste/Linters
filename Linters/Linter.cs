using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linters
{
    public abstract class Linter
    {
        public string Name { get; set; }

        public string Command { get; set; }

        protected virtual string ConfigFileName { get; set; }

        protected virtual bool IsEnabled { get; set; }

        protected abstract void ParseErrors(string output);

        public ErrorListProvider Provider { get; set; }

        public void Run(Project project) {

            var directory = Path.GetDirectoryName(project.FullName);

            //check if projeect has .ts files

            //check if project has tsconfig.json

            //run tslint
            var process = new System.Diagnostics.Process();
            
            process.EnableRaisingEvents = true;
            process.StartInfo = new ProcessStartInfo
            {
                WorkingDirectory = directory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal,
                FileName = "cmd.exe",
                Arguments = "/c " + Command
            };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (s, e) => outputBuilder.AppendLine(e.Data);
            process.ErrorDataReceived += (s, e) => errorBuilder.AppendLine(e.Data);

            process.Exited += (sender, args) =>
            {
                ParseErrors(outputBuilder.ToString());
                process.Dispose();
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
    }
}
