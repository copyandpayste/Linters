namespace Linters
{
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.Tagging;

    internal class LintTag : IErrorTag
    {
        private string toolTip;

        public LintTag(ITrackingSpan trackingSpan, string toolTip)
        {
            TrackingSpan = trackingSpan;
            this.toolTip = toolTip;
        }

        public string ErrorType
        {
            get
            {
                return PredefinedErrorTypeNames.Warning;
            }
        }

        public object ToolTipContent
        {
            get
            {
                return toolTip;
            }
        }

        public ITrackingSpan TrackingSpan { get; private set; }
    }
}
