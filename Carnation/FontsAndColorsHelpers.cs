using System;
using System.Windows.Media;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Carnation
{
    internal static class FontsAndColorsHelper
    {
        private static readonly Guid TextEditorMEFItemsColorCategory = new Guid("75a05685-00a8-4ded-bae5-e7a50bfa929a");

        public static ClassificationGridItem TryGetItemForClassification(string classificationTypeName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var fontsAndColorStorage = ServiceProvider.GlobalProvider.GetService<SVsFontAndColorStorage, IVsFontAndColorStorage>();
            if (fontsAndColorStorage is null)
            {
                return null;
            }

            // Open Text Editor category for readonly access.
            if (fontsAndColorStorage.OpenCategory(TextEditorMEFItemsColorCategory, (uint)(__FCSTORAGEFLAGS.FCSF_READONLY | __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS | __FCSTORAGEFLAGS.FCSF_NOAUTOCOLORS)) != VSConstants.S_OK)
            {
                // We were unable to access color information.
                return null;
            }

            try
            {
                var colorItems = new ColorableItemInfo[1];
                if (fontsAndColorStorage.GetItem(classificationTypeName, colorItems) != VSConstants.S_OK)
                {
                    return null;
                }

                var colorItem = colorItems[0];

                var fontAndColorUtilities = (IVsFontAndColorUtilities)fontsAndColorStorage;
                var foregroundColorRef = colorItem.crForeground;

                if (fontAndColorUtilities.GetColorType(foregroundColorRef, out var foregroundColorType) != VSConstants.S_OK
                    || foregroundColorType != (int)__VSCOLORTYPE.CT_RAW)
                {
                    // If the color is not CT_RAW then foregroundColorRef doesn't
                    // regpresent the RGB values for the color and we can't interpret them.
                    return null;
                }

                var foregroundBytes = BitConverter.GetBytes(foregroundColorRef);
                var foreground = Color. FromRgb(foregroundBytes[0], foregroundBytes[1], foregroundBytes[2]);

                var backgroundColorRef = colorItem.crBackground;

                if (fontAndColorUtilities.GetColorType(backgroundColorRef, out var backgroundColorType) != VSConstants.S_OK
                    || backgroundColorType != (int)__VSCOLORTYPE.CT_RAW)
                {
                    // If the color is not CT_RAW then foregroundColorRef doesn't
                    // regpresent the RGB values for the color and we can't interpret them.
                    return null;
                }

                var backgroundBytes = BitConverter.GetBytes(backgroundColorRef);
                var background = Color.FromRgb(backgroundBytes[0], backgroundBytes[1], backgroundBytes[2]);

                return new ClassificationGridItem(classificationTypeName, foreground, background, string.Empty);
            }
            finally
            {
                fontsAndColorStorage.CloseCategory();
            }
        }

        internal static void SaveClassificationItem(ClassificationGridItem item)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var fontsAndColorStorage = ServiceProvider.GlobalProvider.GetService<SVsFontAndColorStorage, IVsFontAndColorStorage>();
            if (fontsAndColorStorage is null)
            {
                return;
            }

            // Open Text Editor to make changes. Make sure LOADDEFAULTS is passed so any default 
            // values can be modified as well.
            if (fontsAndColorStorage.OpenCategory(TextEditorMEFItemsColorCategory, (uint)(__FCSTORAGEFLAGS.FCSF_PROPAGATECHANGES | __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS)) != VSConstants.S_OK)
            {
                // We were unable to access color information.
                return;
            }

            try
            {
                var colorItems = new ColorableItemInfo[1];
                if (fontsAndColorStorage.GetItem(item.Classification, colorItems) != VSConstants.S_OK)
                {
                    return;
                }

                var colorItem = colorItems[0];

                colorItem.crForeground = BitConverter.ToUInt32(
                    new byte[] {
                        item.Foreground.R,
                        item.Foreground.G,
                        item.Foreground.B,
                        0 // Alpha
                    }, 0);

                colorItem.crBackground = BitConverter.ToUInt32(
                    new byte[] {
                        item.Background.R,
                        item.Background.G,
                        item.Background.B,
                        0 // Alpha
                    }, 0);

                if (fontsAndColorStorage.SetItem(item.Classification, new[] { colorItem }) != VSConstants.S_OK)
                {
                    throw new Exception();
                }
            }
            finally
            {
                fontsAndColorStorage.CloseCategory();
            }
        }
    }
}
