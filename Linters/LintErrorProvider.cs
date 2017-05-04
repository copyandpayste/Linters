using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using EnvironmentConstants = EnvDTE.Constants;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Linters
{
    public class LintErrorProvider : ErrorListProvider
    {
        public IServiceProvider ServiceProvider { get; set; }

        public DTE2 Environment { get; set; }

        public IVsTextManager TextManager { get; set; }

        public event EventHandler Changed;

        public LintErrorProvider(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            ServiceProvider = serviceProvider;
            Environment = ServiceProvider.GetService<DTE, DTE2>();
            TextManager = ServiceProvider.GetService<VsTextManagerClass, IVsTextManager>();
        }

        public List<ErrorTask> GetErrors(params string[] fileNames)
        {
            return Tasks
                .OfType<ErrorTask>()
                .Where(x => fileNames.Contains(x.Document, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        public List<ErrorTask> GetErrors(Project project)
        {
            return Tasks
                .OfType<ErrorTask>()
                .Where(x => true)
                .ToList();
        }

        public List<ErrorTask> GetErrors()
        {
            return Tasks
                .OfType<ErrorTask>()
                .Where(x => true)
                .ToList();
        }

        public void AddErrors(params ErrorTask[] errors) {
            SuspendRefresh();
            foreach (ErrorTask error in errors) {
                Tasks.Add(error);
            }
            ResumeRefresh();
            OnChanged(EventArgs.Empty);
        }

        public void ClearErrors(params string[] fileNames)
        {
            List<ErrorTask> tasks;
            if (fileNames == null || fileNames.Length == 0)
            {
                tasks = GetErrors();
            }
            else
            {
                tasks = GetErrors(fileNames);
            }

            if (tasks.Count == 0)
            {
                return;
            }

            SuspendRefresh();
            foreach (var task in tasks)
            {
                Tasks.Remove(task);
            }
            ResumeRefresh();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private static bool TryGetWindowFrame(IServiceProvider serviceProvider, string fileName, out IVsWindowFrame windowFrame)
        {
            IVsUIShellOpenDocument openDocument;

            if (!serviceProvider.TryGetService(out openDocument))
            {
                windowFrame = null;
                return false;
            }

            IOleServiceProvider oleServiceProvider;
            IVsUIHierarchy hierarchy;
            uint itemid;
            var viewKind = new Guid(EnvironmentConstants.vsViewKindTextView);

            if (ErrorHandler.Failed(openDocument.OpenDocumentViaProject(
                fileName,
                ref viewKind,
                out oleServiceProvider,
                out hierarchy,
                out itemid,
                out windowFrame)))
            {
                return false;
            }

            return windowFrame != null;
        }

        private static bool TryGetTextLines(IVsWindowFrame windowFrame, out IVsTextLines textLines)
        {
            object docData;
            windowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out docData);
            textLines = docData as IVsTextLines;

            if (textLines != null)
            {
                return true;
            }

            var bufferProvider = docData as IVsTextBufferProvider;

            if (bufferProvider != null)
            {
                if (!ErrorHandler.Failed(bufferProvider.GetTextBuffer(out textLines)))
                {
                    return textLines != null;
                }
            }

            return false;
        }

        private List<ErrorItem> GetExistingErrors(string fileName)
        {
            var list = new List<ErrorItem>();
            var errorItems = Environment.ToolWindows.ErrorList.ErrorItems;

            if (errorItems.Count > 0)
            {
                for (int i = 1; i <= errorItems.Count; i += 1)
                {
                    var item = errorItems.Item(i);

                    if (fileName.Equals(item.FileName, StringComparison.OrdinalIgnoreCase))
                    {
                        list.Add(errorItems.Item(i));
                    }
                }
            }

            return list;
        }

        public void OnChanged(EventArgs e) {
            Changed?.Invoke(this, e);
        }

        public void OnTaskNavigate(object sender, EventArgs e)
        {
            var task = (ErrorTask)sender;

            IVsWindowFrame windowFrame;
            if (!TryGetWindowFrame(ServiceProvider, task.Document, out windowFrame))
            {
                return;
            }

            IVsTextLines textLines;
            if (!TryGetTextLines(windowFrame, out textLines))
            {
                return;
            }

            var viewKind = new Guid(EnvironmentConstants.vsViewKindTextView);

            TextManager.NavigateToLineAndColumn(
                textLines,
                ref viewKind,
                task.Line,
                task.Column,
                task.Line,
                task.Column);
        }
    }
}