using System;
using Microsoft.VisualStudio.Shell;

namespace Carnation
{
    internal partial class ClassificationProvider
    {
        public class ClassificationGridItem : ColorItemBase
        {
            private Guid _category;
            public Guid Category
            {
                get => _category;
                set => SetProperty(ref _category, value);
            }

            private string _definitionName;
            public string DefinitionName
            {
                get => _definitionName;
                set => SetProperty(ref _definitionName, value);
            }

            public string Classification => ClassificationNameMap.ContainsKey(DefinitionName)
                ? ClassificationNameMap[DefinitionName]
                : DefinitionName;

            public string Sample => "Sample Text";

            public ClassificationGridItem(
                Guid category,
                string definitionName,
                uint foregroundColorRef,
                uint backgroundColorRef,
                bool isBold,
                bool isForegroundEditable,
                bool isBackgroundEditable,
                bool isBoldEditable)
                : base(foregroundColorRef, backgroundColorRef, isBold, isForegroundEditable, isBackgroundEditable, isBoldEditable)
            {
                _category = category;
                _definitionName = definitionName;

                PropertyChanged += (s, o) =>
                {
                    if (IsUpdating)
                    { 
                        return;
                    }

                    ThreadHelper.ThrowIfNotOnUIThread();

                    switch (o.PropertyName)
                    {
                        case nameof(Foreground):
                        case nameof(Background):
                        case nameof(IsBold):
                            FontsAndColorsHelper.SaveClassificationItem(this);
                            break;
                    }
                };
            }

            public override string ToString()
                => Classification;
        }
    }
}
