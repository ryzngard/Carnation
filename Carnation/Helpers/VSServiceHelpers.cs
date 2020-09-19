using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;

namespace Carnation
{
    internal static class VSServiceHelpers
    {
        public static Microsoft.VisualStudio.OLE.Interop.IServiceProvider GlobalServiceProvider { get; set; }

        public static TServiceInterface GetMefExport<TServiceInterface>(Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider = null) where TServiceInterface : class
        {
            serviceProvider = serviceProvider ?? GlobalServiceProvider;
            TServiceInterface value = null;
            var componentModel = GetService<IComponentModel, SComponentModel>(serviceProvider);

            if (componentModel != null)
            {
                value = componentModel.DefaultExportProvider.GetExportedValue<TServiceInterface>();
            }

            return value;
        }

        public static IEnumerable<TServiceInterface> GetMefExports<TServiceInterface>(Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider = null) where TServiceInterface : class
        {
            serviceProvider = serviceProvider ?? GlobalServiceProvider;
            IEnumerable<TServiceInterface> values = null;
            var componentModel = GetService<IComponentModel, SComponentModel>(serviceProvider);

            if (componentModel != null)
            {
                values = componentModel.DefaultExportProvider.GetExportedValues<TServiceInterface>();
            }

            return values;
        }

        public static TServiceInterface GetService<TServiceInterface, TService>(
            Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider = null)
            where TServiceInterface : class
            where TService : class
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            serviceProvider = serviceProvider ?? GlobalServiceProvider;
            return (TServiceInterface)GetService(serviceProvider, typeof(TService).GUID, false);
        }

        public static object GetService(
            Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider, Guid guidService, bool unique)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            var guidInterface = VSConstants.IID_IUnknown;
            var ptr = IntPtr.Zero;
            object service = null;

            if (serviceProvider.QueryService(ref guidService, ref guidInterface, out ptr) == 0 &&
                ptr != IntPtr.Zero)
            {
                try
                {
                    if (unique)
                    {
                        service = Marshal.GetUniqueObjectForIUnknown(ptr);
                    }
                    else
                    {
                        service = Marshal.GetObjectForIUnknown(ptr);
                    }
                }
                finally
                {
                    Marshal.Release(ptr);
                }
            }

            return service;
        }
    }
}
