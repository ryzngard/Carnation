using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Windows.Media;
using Carnation.Helpers;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using static Carnation.ClassificationProvider;

namespace Carnation
{
    internal static class FontsAndColorsHelper
    {
        private static readonly Guid TextEditorCategory = new Guid("A27B4E24-A735-4D1D-B8E7-9716E1E3D8E0");
        private static readonly Guid TextEditorLanguageServiceCategory = new Guid("E0187991-B458-4F7E-8CA9-42C9A573B56C");
        private static readonly Guid TextEditorMEFItemsCategory = new Guid("75A05685-00A8-4DED-BAE5-E7A50BFA929A");
        private static readonly Guid TextEditorManagerCategory = new Guid("58E96763-1D3B-4E05-B6BA-FF7115FD0B7B");
        private static readonly Guid TextEditorMarkerCategory = new Guid("FF349800-EA43-46C1-8C98-878E78F46501");

        internal static readonly (FontFamily FontFamily, double FontSize) DefaultFontInfo = (new FontFamily("Consolas"), 13.0);
        private static readonly (Color Foreground, Color Background) DefaultTextColors = (Colors.Black, Colors.White);

        private static IVsFontAndColorStorage s_fontsAndColorStorage;
        private static IVsFontAndColorDefaultsProvider s_fontsAndColorDefaultsProvider;
        private static IVsUIShell2 s_vsUIShell2;

        private const uint InvalidColorRef = 0xff000000;

        private static bool IsUpdating = false;

        private static readonly Guid[] s_categories = new[]
        {
            TextEditorManagerCategory,
            TextEditorLanguageServiceCategory,
            TextEditorMEFItemsCategory,
            // TextEditorMarkerCategory,
            // TextEditorCategory,
        };

        public static ImmutableDictionary<Guid, ImmutableArray<AllColorableItemInfo>> GetTextEditorInfos()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EnsureInitialized();

            var loadedItemNames = new HashSet<string>();

            return s_categories.ToImmutableDictionary(catagory => catagory, catagory => GetCategoryItems(catagory, loadedItemNames));

            static ImmutableArray<AllColorableItemInfo> GetCategoryItems(Guid category, HashSet<string> loadedItemNames)
            {
                if (s_fontsAndColorDefaultsProvider.GetObject(category, out var obj) != VSConstants.S_OK)
                {
                    return ImmutableArray<AllColorableItemInfo>.Empty;
                }

                var fontAndColorDefaults = (IVsFontAndColorDefaults)obj;
                if (fontAndColorDefaults.GetItemCount(out var count) != VSConstants.S_OK)
                {
                    return ImmutableArray<AllColorableItemInfo>.Empty;
                }

                var builder = ImmutableArray.CreateBuilder<AllColorableItemInfo>();

                var items = new AllColorableItemInfo[1];
                for (var index = 0; index < count; index++)
                {

                    if (fontAndColorDefaults.GetItem(index, items) == VSConstants.S_OK
                        && !loadedItemNames.Contains(items[0].bstrName))
                    {
                        builder.Add(items[0]);
                        loadedItemNames.Add(items[0].bstrName);
                    }
                }

                return builder.ToImmutable();
            }
        }

        public static (Color Foreground, Color Background) GetPlainTextColors()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EnsureInitialized();

            if (s_fontsAndColorStorage.OpenCategory(TextEditorManagerCategory, (uint)(__FCSTORAGEFLAGS.FCSF_READONLY | __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS)) != VSConstants.S_OK)
            {
                return DefaultTextColors;
            }

            try
            {
                var colorItems = new ColorableItemInfo[1];
                if (s_fontsAndColorStorage.GetItem("Plain Text", colorItems) != VSConstants.S_OK)
                {
                    return DefaultTextColors;
                }

                var colorItem = colorItems[0];

                var foreground = TryGetColor(colorItem.crForeground) ?? DefaultTextColors.Foreground;
                var background = TryGetColor(colorItem.crBackground) ?? DefaultTextColors.Background;

                return (foreground, background);
            }
            finally
            {
                s_fontsAndColorStorage.CloseCategory();
            }
        }

        public static (FontFamily FontFamily, double FontSize) GetEditorFontInfo(bool scaleFontSize = true)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EnsureInitialized();

            var fontsAndColorStorage = ServiceProvider.GlobalProvider.GetService<SVsFontAndColorStorage, IVsFontAndColorStorage>();
            if (fontsAndColorStorage is null)
            {
                return DefaultFontInfo;
            }

            // Open Text Editor category for readonly access.
            if (fontsAndColorStorage.OpenCategory(TextEditorMEFItemsCategory, (uint)(__FCSTORAGEFLAGS.FCSF_READONLY | __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS)) != VSConstants.S_OK)
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
                    : DefaultFontInfo.FontFamily;
                _ = fontInfo[0].bPointSizeValid == 1
                    ? scaleFontSize
                        ? Math.Abs(logFont[0].lfHeight) * GetDipsPerPixel()
                        : fontInfo[0].wPointSize
                    : DefaultFontInfo.FontSize;

                return (fontFamily, DefaultFontInfo.FontSize);
            }
            finally
            {
                fontsAndColorStorage.CloseCategory();
            }
        }

        private static double GetDipsPerPixel()
        {
            var dc = UnsafeNativeMethods.GetDC(IntPtr.Zero);
            if (dc != IntPtr.Zero)
            {
                // Getting the DPI from the desktop is bad, but some callers just have no context for what monitor they are on.
                double fallbackDpi = UnsafeNativeMethods.GetDeviceCaps(dc, UnsafeNativeMethods.LOGPIXELSX);
                UnsafeNativeMethods.ReleaseDC(IntPtr.Zero, dc);

                return fallbackDpi / 96.0;
            }

            return 1;
        }

        private static void EnsureInitialized()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (s_fontsAndColorStorage is null)
            {
                s_fontsAndColorStorage = ServiceProvider.GlobalProvider.GetService<SVsFontAndColorStorage, IVsFontAndColorStorage>();
                s_vsUIShell2 = ServiceProvider.GlobalProvider.GetService<SVsUIShell, IVsUIShell2>();
                s_fontsAndColorDefaultsProvider = (IVsFontAndColorDefaultsProvider)ServiceProvider.GlobalProvider.GetService(Guid.Parse("DAF27B38-80B3-4C58-8133-AFD41C36C79A")) ?? throw new Exception("Could not retrieve IVsFontAndColorDefaultsProvider.");
            }
        }

        public static ClassificationGridItem TryGetClassificationItemForInfo(Guid category, AllColorableItemInfo allColorableItemInfo)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EnsureInitialized();

            if (s_fontsAndColorStorage.OpenCategory(category, (uint)(__FCSTORAGEFLAGS.FCSF_READONLY | __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS)) != VSConstants.S_OK)
            {
                // We were unable to access color information.
                return null;
            }

            try
            {
                var definitionName = allColorableItemInfo.bstrName;

                var colorItems = new ColorableItemInfo[1];
                if (s_fontsAndColorStorage.GetItem(definitionName, colorItems) != VSConstants.S_OK)
                {
                    return null;
                }

                var colorItem = colorItems[0];
                var isBold = ((FONTFLAGS)colorItem.dwFontFlags).HasFlag(FONTFLAGS.FF_BOLD);
                var isForegroundEditable = ((__FCITEMFLAGS)allColorableItemInfo.fFlags).HasFlag(__FCITEMFLAGS.FCIF_ALLOWFGCHANGE);
                var isBackgroundEditable = ((__FCITEMFLAGS)allColorableItemInfo.fFlags).HasFlag(__FCITEMFLAGS.FCIF_ALLOWBGCHANGE);
                var isBoldEditable = ((__FCITEMFLAGS)allColorableItemInfo.fFlags).HasFlag(__FCITEMFLAGS.FCIF_ALLOWBOLDCHANGE);

                return new ClassificationGridItem(
                    category,
                    definitionName,
                    colorItem.crForeground,
                    colorItem.crBackground,
                    allColorableItemInfo.crAutoForeground,
                    allColorableItemInfo.crAutoBackground,
                    isBold,
                    isForegroundEditable,
                    isBackgroundEditable,
                    isBoldEditable);
            }
            finally
            {
                s_fontsAndColorStorage.CloseCategory();
            }
        }

        public static Color? TryGetColor(uint colorRef)
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
                return null;
            }
            else if (colorType == (int)__VSCOLORTYPE.CT_AUTOMATIC)
            {
                return null;
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
                    if (encodedIndex[0] is COLORINDEX.CI_SYSTEXT_BK or
                        COLORINDEX.CI_SYSTEXT_FG)
                    {
                        return null;
                    }

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
                ? FromWin32Color((int)win32Color.Value)
                : null;

            static Color FromWin32Color(int color)
            {
                var drawingColor = System.Drawing.ColorTranslator.FromWin32(color);
                return Color.FromRgb(drawingColor.R, drawingColor.G, drawingColor.B);
            }
        }

        internal static void SaveClassificationItem(ClassificationGridItem item)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EnsureInitialized();

            IsUpdating = true;

            // Some classifications exist in the LanguageService and MEFItems category. This ensures they are kept in sync.
            if (item.Category != TextEditorMEFItemsCategory)
            {
                TryUpdateItem(TextEditorMEFItemsCategory, item);
            }

            TryUpdateItem(item.Category, item);

            IsUpdating = false;

            static void TryUpdateItem(Guid category, ClassificationGridItem item)
            {

                // Make sure LOADDEFAULTS is passed so any default values can be modified as well.
                if (s_fontsAndColorStorage.OpenCategory(category, (uint)(__FCSTORAGEFLAGS.FCSF_PROPAGATECHANGES | __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS)) != VSConstants.S_OK)
                {
                    // We were unable to access color information.
                    return;
                }

                try
                {
                    var colorItems = new ColorableItemInfo[1];
                    if (s_fontsAndColorStorage.GetItem(item.DefinitionName, colorItems) != VSConstants.S_OK)
                    {
                        return;
                    }

                    var colorItem = colorItems[0];

                    colorItem.crForeground = item.ForegroundColorRef;
                    colorItem.crBackground = item.BackgroundColorRef;

                    colorItem.dwFontFlags = item.IsBold
                        ? (uint)FONTFLAGS.FF_BOLD
                        : (uint)FONTFLAGS.FF_DEFAULT;

                    if (s_fontsAndColorStorage.SetItem(item.DefinitionName, new[] { colorItem }) != VSConstants.S_OK)
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

        internal static uint GetColorRef(Color color, Color defaultColor)
        {
            return (color == defaultColor)
                ? InvalidColorRef
                : ToWin32Color(color);

            static uint ToWin32Color(Color color)
            {
                return (uint)(color.R | (color.G << 8) | (color.B << 16));
            }
        }

        internal static void RefreshClassificationItem(ClassificationGridItem item, AllColorableItemInfo allColorableItemInfo)
        {
            if (IsUpdating)
            {
                return;
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            EnsureInitialized();

            if (s_fontsAndColorStorage.OpenCategory(item.Category, (uint)(__FCSTORAGEFLAGS.FCSF_READONLY | __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS)) != VSConstants.S_OK)
            {
                return;
            }

            try
            {
                var colorItems = new ColorableItemInfo[1];
                if (s_fontsAndColorStorage.GetItem(item.DefinitionName, colorItems) != VSConstants.S_OK)
                {
                    return;
                }

                var colorItem = colorItems[0];

                item.AutoForegroundColorRef = allColorableItemInfo.crAutoForeground;
                item.AutoBackgroundColorRef = allColorableItemInfo.crAutoBackground;
                item.ForegroundColorRef = colorItem.crForeground;
                item.BackgroundColorRef = colorItem.crBackground;
                item.IsBold = ((FONTFLAGS)colorItem.dwFontFlags).HasFlag(FONTFLAGS.FF_BOLD);
            }
            finally
            {
                s_fontsAndColorStorage.CloseCategory();
            }
        }

        internal static void ResetClassificationItem(ClassificationGridItem item)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EnsureInitialized();

            if (s_fontsAndColorStorage.OpenCategory(item.Category, (uint)(__FCSTORAGEFLAGS.FCSF_PROPAGATECHANGES | __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS)) != VSConstants.S_OK)
            {
                return;
            }

            try
            {
                var colorItems = new ColorableItemInfo[1];
                if (s_fontsAndColorStorage.GetItem(item.DefinitionName, colorItems) != VSConstants.S_OK)
                {
                    return;
                }

                if (((IVsFontAndColorStorage2)s_fontsAndColorStorage).RevertItemToDefault(item.DefinitionName) != VSConstants.S_OK)
                {
                    throw new Exception();
                }

                if (s_fontsAndColorStorage.GetItem(item.DefinitionName, colorItems) != VSConstants.S_OK)
                {
                    return;
                }

                var colorItem = colorItems[0];

                item.ForegroundColorRef = colorItem.crForeground;
                item.BackgroundColorRef = colorItem.crBackground;
                item.IsBold = ((FONTFLAGS)colorItem.dwFontFlags).HasFlag(FONTFLAGS.FF_BOLD);
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

            foreach (var category in s_categories)
            {
                ResetCategoryItems(category);
            }

            return;

            static void ResetCategoryItems(Guid category)
            {
                if (s_fontsAndColorStorage.OpenCategory(category, (uint)__FCSTORAGEFLAGS.FCSF_PROPAGATECHANGES) != VSConstants.S_OK)
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
}
