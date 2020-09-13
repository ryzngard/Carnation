using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;

namespace Carnation
{
    internal static class VSServiceHelpers
    {
        public static Microsoft.VisualStudio.OLE.Interop.IServiceProvider GlobalServiceProvider { get; set; }

        public static TServiceInterface GetMefService<TServiceInterface>(Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider = null) where TServiceInterface : class
        {
            serviceProvider = serviceProvider ?? GlobalServiceProvider;
            TServiceInterface service = null;
            var componentModel = GetService<IComponentModel, SComponentModel>(serviceProvider);

            if (componentModel != null)
            {
                service = componentModel.GetService<TServiceInterface>();
            }

            return service;
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
