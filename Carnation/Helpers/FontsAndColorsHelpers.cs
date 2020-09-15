using System;
using System.Windows.Media;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Carnation
{
    internal static class FontsAndColorsHelper
    {
        private static readonly Guid TextEditorMEFItemsColorCategory = new Guid("75a05685-00a8-4ded-bae5-e7a50bfa929a");
        private static readonly FontFamily DefaultFontFamily = new FontFamily("Consolas");
        private static readonly double DefaultFontSize = 13.0;
        private static readonly (FontFamily, double) DefaultFontInfo = (DefaultFontFamily, DefaultFontSize);
        private static readonly (Color, Color) DefaultTextColors = (Colors.Black, Colors.White);

        private static IVsFontAndColorStorage s_fontsAndColorStorage;
        private static IVsUIShell2 s_vsUIShell2;

        public static (FontFamily, double) GetEditorFontInfo()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var fontsAndColorStorage = ServiceProvider.GlobalProvider.GetService<SVsFontAndColorStorage, IVsFontAndColorStorage>();
            if (fontsAndColorStorage is null)
            {
                return DefaultFontInfo;
            }

            // Open Text Editor category for readonly access.
            if (fontsAndColorStorage.OpenCategory(TextEditorMEFItemsColorCategory, (uint)(__FCSTORAGEFLAGS.FCSF_READONLY | __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS | __FCSTORAGEFLAGS.FCSF_NOAUTOCOLORS)) != VSConstants.S_OK)
            {
                return DefaultFontInfo;
            }

            try
            {
                var logFont = new LOGFONTW[1];
                var fontInfo = new FontInfo[1];
                if (fontsAndColorStorage.GetFont(logFont, fontInfo) != VSConstants.S_OK)
                {
                    return DefaultFontInfo;
                }

                var fontFamily = fontInfo[0].bFaceNameValid == 1
                    ? new FontFamily(fontInfo[0].bstrFaceName)
                    : DefaultFontFamily;

                var fontSize = fontInfo[0].bPointSizeValid == 1
                    ? Math.Abs(logFont[0].lfHeight)
                    : DefaultFontSize;

                return (fontFamily, fontSize);
            }
            finally
            {
                fontsAndColorStorage.CloseCategory();
            }
        }

        private static void EnsureInitialized()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (s_fontsAndColorStorage is null)
            {
                s_fontsAndColorStorage = ServiceProvider.GlobalProvider.GetService<SVsFontAndColorStorage, IVsFontAndColorStorage>();
                s_vsUIShell2 = ServiceProvider.GlobalProvider.GetService<SVsUIShell, IVsUIShell2>();
            }
        }

        public static ClassificationGridItem TryGetItemForClassification((string, string) classificationTypeNames, Color defaultForeground, Color defaultBackground)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EnsureInitialized();

            var (classificationTypeName, definitionName) = classificationTypeNames;

            // Open Text Editor category for readonly access.
            if (s_fontsAndColorStorage.OpenCategory(TextEditorMEFItemsColorCategory, (uint)(__FCSTORAGEFLAGS.FCSF_READONLY | __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS | __FCSTORAGEFLAGS.FCSF_NOAUTOCOLORS)) != VSConstants.S_OK)
            {
                // We were unable to access color information.
                return null;
            }

            try
            {
                var colorItems = new ColorableItemInfo[1];
                if (s_fontsAndColorStorage.GetItem(classificationTypeName, colorItems) != VSConstants.S_OK &&
                    s_fontsAndColorStorage.GetItem(definitionName, colorItems) != VSConstants.S_OK)
                {
                    return null;
                }

                var colorItem = colorItems[0];

                var foreground = TryGetColor(colorItem.crForeground, defaultForeground);
                if (foreground == null)
                {
                    return null;
                }

                var background = TryGetColor(colorItem.crBackground, defaultBackground);
                if (background == null)
                {
                    return null;
                }

                var isBold = ((FONTFLAGS)colorItem.dwFontFlags).HasFlag(FONTFLAGS.FF_BOLD);

                return new ClassificationGridItem(classificationTypeName, definitionName, foreground.Value, background.Value, isBold);
            }
            finally
            {
                s_fontsAndColorStorage.CloseCategory();
            }
        }

        internal static (Color, Color) GetPlainTextColors()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EnsureInitialized();

            // Open Text Editor category for readonly access.
            if (s_fontsAndColorStorage.OpenCategory(TextEditorMEFItemsColorCategory, (uint)(__FCSTORAGEFLAGS.FCSF_READONLY | __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS | __FCSTORAGEFLAGS.FCSF_NOAUTOCOLORS)) != VSConstants.S_OK)
            {
                return DefaultTextColors;
            }

            try
            {
                var fontAndColorUtilities = (IVsFontAndColorUtilities)s_fontsAndColorStorage;

                Color? foreground = null;
                if (fontAndColorUtilities.EncodeIndexedColor(COLORINDEX.CI_USERTEXT_FG, out var foregroundCR) == VSConstants.S_OK)
                {
                    foreground = TryGetColor(foregroundCR, null);
                }

                Color? background = null;
                if (fontAndColorUtilities.EncodeIndexedColor(COLORINDEX.CI_USERTEXT_BK, out var backgroundCR) == VSConstants.S_OK)
                {
                    background = TryGetColor(backgroundCR, null);
                }

                return (foreground ?? DefaultTextColors.Item1, background ?? DefaultTextColors.Item2);
            }
            finally
            {
                s_fontsAndColorStorage.CloseCategory();
            }
        }

        private static Color? TryGetColor(uint colorRef, Color? defaultColor)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var fontAndColorUtilities = (IVsFontAndColorUtilities)s_fontsAndColorStorage;

            if (fontAndColorUtilities.GetColorType(colorRef, out var colorType) != VSConstants.S_OK)
            {
                return null;
            }

            uint? win32Color = null;

            if (colorType == (int)__VSCOLORTYPE.CT_INVALID)
            {
                return defaultColor;
            }
            else if (colorType == (int)__VSCOLORTYPE.CT_RAW)
            {
                win32Color = colorRef;
            }
            else if (colorType == (int)__VSCOLORTYPE.CT_COLORINDEX)
            {
                var encodedIndex = new COLORINDEX[1];
                if (fontAndColorUtilities.GetEncodedIndex(colorRef, encodedIndex) == VSConstants.S_OK &&
                    fontAndColorUtilities.GetRGBOfIndex(encodedIndex[0], out var decoded) == VSConstants.S_OK)
                {
                    win32Color = encodedIndex[0] == COLORINDEX.CI_USERTEXT_BK
                        ? decoded & 0x00ffffff
                        : decoded | 0xff000000;
                }
            }
            else if (colorType == (int)__VSCOLORTYPE.CT_SYSCOLOR)
            {
                if (fontAndColorUtilities.GetEncodedSysColor(colorRef, out var sysColor) == VSConstants.S_OK)
                {
                    win32Color = (uint)sysColor;
                }
            }
            else if (colorType == (int)__VSCOLORTYPE.CT_VSCOLOR)
            {
                if (fontAndColorUtilities.GetEncodedVSColor(colorRef, out var vsSysColor) == VSConstants.S_OK &&
                    s_vsUIShell2.GetVSSysColorEx(vsSysColor, out var rgbColor) == VSConstants.S_OK)
                {
                    win32Color = rgbColor;
                }
            }

            return win32Color.HasValue
                ? (Color?)FromWin32Color((int)win32Color.Value)
                : null;

            Color FromWin32Color(int color)
            {
                var drawingColor = System.Drawing.ColorTranslator.FromWin32(color);
                return Color.FromRgb(drawingColor.R, drawingColor.G, drawingColor.B);
            }
        }

        internal static void SaveClassificationItem(ClassificationGridItem item)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EnsureInitialized();

            // Open Text Editor to make changes. Make sure LOADDEFAULTS is passed so any default 
            // values can be modified as well.
            if (s_fontsAndColorStorage.OpenCategory(TextEditorMEFItemsColorCategory, (uint)(__FCSTORAGEFLAGS.FCSF_PROPAGATECHANGES | __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS)) != VSConstants.S_OK)
            {
                // We were unable to access color information.
                return;
            }

            try
            {
                var itemName = item.Classification;
                var colorItems = new ColorableItemInfo[1];
                if (s_fontsAndColorStorage.GetItem(item.Classification, colorItems) != VSConstants.S_OK)
                {
                    itemName = item.DefinitionName;
                    if (s_fontsAndColorStorage.GetItem(item.DefinitionName, colorItems) != VSConstants.S_OK)
                    {
                        return;
                    }
                }

                var colorItem = colorItems[0];

                colorItem.crForeground = ToWin32Color(item.Foreground);
                colorItem.crBackground = ToWin32Color(item.Background);

                colorItem.dwFontFlags = item.IsBold
                    ? (uint)FONTFLAGS.FF_BOLD
                    : (uint)FONTFLAGS.FF_DEFAULT;

                if (s_fontsAndColorStorage.SetItem(itemName, new[] { colorItem }) != VSConstants.S_OK)
                {
                    throw new Exception();
                }
            }
            finally
            {
                s_fontsAndColorStorage.CloseCategory();
            }

            uint ToWin32Color(Color color)
            {
                return (uint)(color.R | color.G << 8 | color.B << 16);
            }
        }

        internal static void ResetClassificationItem(ClassificationGridItem item, Color defaultForeground, Color defaultBackground)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EnsureInitialized();

            // Open Text Editor to make changes.
            if (s_fontsAndColorStorage.OpenCategory(TextEditorMEFItemsColorCategory, (uint)(__FCSTORAGEFLAGS.FCSF_PROPAGATECHANGES | __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS | __FCSTORAGEFLAGS.FCSF_NOAUTOCOLORS)) != VSConstants.S_OK)
            {
                return;
            }

            try
            {
                var itemName = item.Classification;
                var colorItems = new ColorableItemInfo[1];
                if (s_fontsAndColorStorage.GetItem(item.Classification, colorItems) != VSConstants.S_OK)
                {
                    itemName = item.DefinitionName;
                    if (s_fontsAndColorStorage.GetItem(item.DefinitionName, colorItems) != VSConstants.S_OK)
                    {
                        return;
                    }
                }

                if (((IVsFontAndColorStorage2)s_fontsAndColorStorage).RevertItemToDefault(itemName) != VSConstants.S_OK)
                {
                    throw new Exception();
                }

                if (s_fontsAndColorStorage.GetItem(itemName, colorItems) != VSConstants.S_OK)
                {
                    return;
                }

                var colorItem = colorItems[0];

                var foreground = TryGetColor(colorItem.crForeground, defaultForeground);
                if (foreground == null)
                {
                    return;
                }

                var background = TryGetColor(colorItem.crBackground, defaultBackground);
                if (background == null)
                {
                    return;
                }

                var isBold = ((FONTFLAGS)colorItem.dwFontFlags).HasFlag(FONTFLAGS.FF_BOLD);

                item.Foreground = foreground.Value;
                item.Background = background.Value;
                item.IsBold = isBold;
            }
            finally
            {
                s_fontsAndColorStorage.CloseCategory();
            }
        }

        internal static void ResetAllClassificationItems()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EnsureInitialized();

            // Open Text Editor to make changes.
            if (s_fontsAndColorStorage.OpenCategory(TextEditorMEFItemsColorCategory, (uint)(__FCSTORAGEFLAGS.FCSF_PROPAGATECHANGES)) != VSConstants.S_OK)
            {
                return;
            }

            try
            {
                if (((IVsFontAndColorStorage2)s_fontsAndColorStorage).RevertAllItemsToDefault() != VSConstants.S_OK)
                {
                    throw new Exception();
                }
            }
            finally
            {
                s_fontsAndColorStorage.CloseCategory();
            }
        }
    }
}
