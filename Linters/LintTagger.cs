using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Shell;
using Linters;
using System.Collections;
using System.Threading;
using System.Diagnostics;
using EnvDTE;

internal class LintTagger : ITagger<IErrorTag>
{
    private ITextBuffer buffer;

    private LintErrorProvider errorListProvider;

    private string fileName;

    private CancellationTokenSource cancellationTokenSource;

    public LintTagger(ITextBuffer buffer, LintErrorProvider errorListProvider, string fileName)
    {
        this.buffer = buffer;
        this.errorListProvider = errorListProvider;
        this.fileName = fileName;
        this.buffer.Changed += OnBufferChanged;
        this.errorListProvider.Changed += OnErrorListChange;
    }

    public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

    public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection snapshotSpans)
    {
        var list = new List<TagSpan<IErrorTag>>();

        IList<ErrorTask> errors = this.errorListProvider.GetErrors(this.fileName);
        var snapshot = buffer.CurrentSnapshot;



        foreach (SnapshotSpan snapshotSpan in snapshotSpans)
        {
            foreach (var error in errors)
            {
                if (error.Line > snapshotSpan.Snapshot.LineCount)
                {
                    continue;
                }

                var lintTag = new LintTag(error.Text);

                Span errorSpan = GetErrorSpan(snapshotSpan.Snapshot, error);

                var tagSpan = new TagSpan<IErrorTag>(new SnapshotSpan(snapshotSpan.Snapshot, errorSpan), lintTag);

                list.Add(tagSpan);
            }
        }

        return list;
    }

    private static Span GetErrorSpan(ITextSnapshot snapshot, ErrorTask error)
    {
        var line = snapshot.GetLineFromLineNumber(error.Line);
        var text = line.GetText();

        if (text.Length < error.Column)
        {
            return new Span(line.End.Position, 1);
        }

        var start = line.Start.Position + error.Column;
        var length = line.End.Position - start;

        return new Span(start, length);
    }

    public void InvokeTagsChanged() {
        var handler = TagsChanged;

        if (handler != null)
        {
            var snapshot = buffer.CurrentSnapshot;
            var span = new SnapshotSpan(snapshot, new Span(0, snapshot.Length));

            handler(this, new SnapshotSpanEventArgs(span));
        }
    }

    private void OnErrorListChange(object sender, EventArgs e)
    {
        InvokeTagsChanged();
    }

    private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
    {
        if (cancellationTokenSource != null)
        {
            cancellationTokenSource.Cancel();
        }

        cancellationTokenSource = new CancellationTokenSource();
        UpdateErrorsWithDelay(e.After, cancellationTokenSource.Token);
    }

    private void UpdateErrorsWithDelay(ITextSnapshot snapshot, CancellationToken token)
    {
        System.Threading.Tasks.Task.Run(async () =>
        {
            await System.Threading.Tasks.Task.Delay(500);

            if (token.IsCancellationRequested)
            {
                return;
            }

            //TODO: use separate provider for each project/linter
            foreach (Project project in errorListProvider.Environment.Solution.Projects)
            {
                //var tsLinter = new TsLinter(CurrentErrorListProvider);
                var styleLinter = new StyleLinter(errorListProvider);
                //tsLinter.Run(project);
                styleLinter.Run(project);
            }

            InvokeTagsChanged();
        }, token);
    }
}
