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
using System.Windows.Threading;
using System.Threading.Tasks;

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

        [Export(typeof(ErrorListProvider))]
        internal static ErrorListProvider CurrentErrorListProvider { get; private set; }

        private DTE dte;

        private Solution solution;

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            CurrentErrorListProvider = new ErrorListProvider(this);
            dte = (DTE)GetService(typeof(DTE));
            solution = dte.Solution;

            // Delay execution until VS is idle.
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                // Then execute in a background thread.
                System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        await LintAsync();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                });
            }), DispatcherPriority.ApplicationIdle, null);
        }

        public async System.Threading.Tasks.Task LintAsync()
        {
            await System.Threading.Tasks.Task.Run(() => {
                foreach (Project project in solution.Projects)
                {
                    //var tsLinter = new TsLinter(CurrentErrorListProvider);
                    var styleLinter = new StyleLinter(CurrentErrorListProvider);
                    //tsLinter.Run(project);
                    styleLinter.Run(project);
                }
            });
        }
    }
}
