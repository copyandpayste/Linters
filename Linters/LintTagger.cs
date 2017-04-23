using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Shell;
using Linters;
using System.Collections;
using System.Threading;

internal class LintTagger : ITagger<IErrorTag>
{
    private static readonly Regex WordBoundaryPattern = new Regex(@"[^\$\w]", RegexOptions.Compiled);

    private ITextBuffer buffer;

    private LintErrorProvider errorListProvider;

    private string fileName;

    private IList<LintTag> tags;

    private CancellationTokenSource cancellationTokenSource;

    public LintTagger(ITextBuffer buffer, LintErrorProvider errorListProvider, string fileName)
    {
        this.buffer = buffer;
        this.errorListProvider = errorListProvider;
        this.fileName = fileName;

        tags = new List<LintTag>();
        PopulateTags();

        this.buffer.Changed += OnBufferChanged;
        this.errorListProvider.Changed += OnErrorListChange;
    }

    public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

    public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection snapshotSpans)
    {
        var list = new List<TagSpan<IErrorTag>>();

        if (tags.Count > 0)
        {
            foreach (var snapshotSpan in snapshotSpans)
            {
                foreach (var tag in tags)
                {
                    var snapshot = snapshotSpan.Snapshot;
                    var span = tag.TrackingSpan.GetSpan(snapshot);

                    if (span.IntersectsWith(snapshotSpan))
                    {
                        var tagSpan = new TagSpan<IErrorTag>(new SnapshotSpan(snapshot, span), tag);

                        list.Add(tagSpan);
                    }
                }
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
        var match = WordBoundaryPattern.Match(text, error.Column);

        if (match.Success)
        {
            length = match.Index - error.Column;
        }

        return new Span(start, length);
    }

    private void OnErrorListChange(object sender, EventArgs e)
    {
        if (IsRelevant(e))
        {
            PopulateTags();

            var handler = TagsChanged;

            if (handler != null)
            {
                var snapshot = buffer.CurrentSnapshot;
                var span = new SnapshotSpan(snapshot, new Span(0, snapshot.Length));

                handler(this, new SnapshotSpanEventArgs(span));
            }
        }
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

            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
        }, token);
    }

    private bool IsRelevant(EventArgs e)
    {
        //switch (e.Action)
        //{
        //    case ErrorListAction.ClearFile:
        //    case ErrorListAction.AddFile:
        //        return e.ContainsFile(this.fileName);
        //    case ErrorListAction.ClearAll:
        //        return true;
        //}

        return false;
    }

    private void PopulateTags()
    {
        tags.Clear();

        IList<ErrorTask> errors = this.errorListProvider.GetErrors(this.fileName);
        var snapshot = buffer.CurrentSnapshot;

        foreach (var error in errors)
        {
            if (error.Line > snapshot.LineCount)
            {
                continue;
            }

            var errorSpan = GetErrorSpan(snapshot, error);
            var trackingSpan = snapshot.CreateTrackingSpan(errorSpan, SpanTrackingMode.EdgeInclusive);

            tags.Add(new LintTag(trackingSpan, error.Text));
        }
    }
}
