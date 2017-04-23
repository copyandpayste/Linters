namespace Linters
{
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.Utilities;
    using Microsoft.VisualStudio.Shell;

    [Export(typeof(ITaggerProvider))]
    [ContentType("text")]
    [TagType(typeof(IErrorTag))]
    internal class LintTaggerProvider : ITaggerProvider
    {
        [Import(typeof(LintErrorProvider))]
        public LintErrorProvider ErrorListProvider { get; set; }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer)
            where T : ITag
        {
            var doc = GetBufferProperty<ITextDocument>(buffer);

            if (doc != null)
            {
                var fileName = doc.FilePath;
                return buffer.Properties.GetOrCreateSingletonProperty(() => (ITagger<T>)new LintTagger(buffer, ErrorListProvider, fileName));
            }

            return null;
        }

        private static TValue GetBufferProperty<TValue>(ITextBuffer buffer)
            where TValue : class
        {
            var key = typeof(TValue);
            var properties = buffer.Properties;

            if (properties.ContainsProperty(key))
            {
                return properties[key] as TValue;
            }

            return null;
        }
    }
}
