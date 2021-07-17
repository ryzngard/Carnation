using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Carnation
{
    internal static class ClassificationHelpers
    {
        private static readonly HashSet<string> s_ignoredClassifications = new() { "(TRANSIENT)", "formal language" };

        public static ImmutableDictionary<string, string> GetClassificationNameMap()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return VSServiceHelpers.GetMefExports<EditorFormatDefinition>()
                .Select(definition => definition.GetType())
                .Where(type => type.GetCustomAttribute<UserVisibleAttribute>()?.UserVisible == true)
                .Select(type => (type.GetCustomAttribute<NameAttribute>()?.Name, type.GetCustomAttribute<ClassificationTypeAttribute>()?.ClassificationTypeNames))
                .Where(names => !string.IsNullOrEmpty(names.Name))
                .ToImmutableDictionary(t => t.Name, t => t.ClassificationTypeNames ?? t.Name);
        }

        public static ImmutableArray<string> GetClassificationsForSpan(IWpfTextView view, Span span)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // If the cursor is after the last character return empty array.
            if (span.Start == view.TextBuffer.CurrentSnapshot.Length)
            {
                return ImmutableArray<string>.Empty;
            }

            // The span to classify must have a length.
            var snapshotSpan = span.Length == 0
                ? new SnapshotSpan(view.TextSnapshot, span.Start, 1)
                : new SnapshotSpan(view.TextSnapshot, span);

            var classificationService = VSServiceHelpers.GetMefExport<IClassifierAggregatorService>();
            var classifier = (IAccurateClassifier)classificationService.GetClassifier(view.TextBuffer);

            var classifiedSpans = classifier.GetAllClassificationSpans(snapshotSpan, CancellationToken.None);

            var classifications = ImmutableArray.CreateBuilder<string>();
            foreach (var classifiedSpan in classifiedSpans)
            {
                CollectClassifications(classifiedSpan.ClassificationType, classifications);
            }

            return classifications.ToImmutable();

            static void CollectClassifications(IClassificationType classificationType, ImmutableArray<string>.Builder classifications)
            {
                var baseClassificationTypes = classificationType.BaseTypes.ToArray();
                if (baseClassificationTypes.Length <= 1 &&
                    !s_ignoredClassifications.Contains(classificationType.Classification) &&
                    !classifications.Contains(classificationType.Classification))
                {
                    classifications.Add(classificationType.Classification);
                }

                foreach (var baseClassificationType in baseClassificationTypes)
                {
                    CollectClassifications(baseClassificationType, classifications);
                }
            }
        }
    }
}
