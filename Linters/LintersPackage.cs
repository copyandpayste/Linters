//------------------------------------------------------------------------------
// <copyright file="LintersPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Composition;
using EnvDTE;
using System.IO;
using Microsoft.VisualStudio.Threading;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Linters
{

    
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [Guid(LintersPackage.PackageGuidString)]
    public sealed class LintersPackage : Package
    {
        /// <summary>
        /// LintersPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "33d7367e-a0ec-474c-b6ad-f7a990fde296";

        /// <summary>
        /// Initializes a new instance of the <see cref="LintersPackage"/> class.
        /// </summary>
        public LintersPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        #region Package Members

        [Export(typeof(ErrorListProvider))]
        internal static ErrorListProvider CurrentErrorListProvider { get; private set; }

        private DTE dte;

        private Solution solution;

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override async void Initialize()
        {
            base.Initialize();

            CurrentErrorListProvider = new ErrorListProvider(this);

            //Dte.Events.DocumentEvents.DocumentSaved
            //FileSystemWatcher

            this.dte = (DTE)GetService(typeof(DTE));
            this.solution = this.dte.Solution;

            this.dte.Events.SolutionEvents.Opened += SolutionOpened;
            this.dte.Events.TextEditorEvents.LineChanged += LineChanged;    
        }

        private void SolutionOpened() {
            foreach (Project project in this.solution.Projects)
            {
                var directory = Path.GetDirectoryName(project.FullName);

                //check if projeect has .ts files

                //check if project has tsconfig.json

                //run tslint
                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        WorkingDirectory = directory,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = "cmd.exe",
                        Arguments = "/c stylelint **.scss --formatter json"
                    };

                    var outputBuilder = new StringBuilder();
                    var errorBuilder = new StringBuilder();

                    process.OutputDataReceived += (s, e) => outputBuilder.AppendLine(e.Data);
                    process.ErrorDataReceived += (s, e) => errorBuilder.AppendLine(e.Data);

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();

                   JArray array = (JArray)JsonConvert.DeserializeObject(outputBuilder.ToString());

                    foreach (JObject obj in array) {
                        var error = new ErrorTask();
                        error.Text = "some error";
                        error.Line = 0;
                        error.Column = 0;
                        error.Document = obj.GetValue("source").ToString();
                        CurrentErrorListProvider.Tasks.Add(error);
                        CurrentErrorListProvider.Show();
                    }
                }

                //check if project has .css, .scss, or .sass files

                //check if project has .stylelintrc 

                //run stylelint
            }
        }

        public void LineChanged(TextPoint StartPoint, TextPoint EndPoint, int Hint)
        {

        }

        #endregion
    }
}
