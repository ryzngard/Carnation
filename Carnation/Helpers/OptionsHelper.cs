using System.Threading.Tasks;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;

namespace Carnation.Helpers
{
    internal static class OptionsHelper
    {
        internal const string GeneralSettingsCollectionName = "CarnationSettings";

        internal static async Task<SettingsStore> GetReadonlySettingsStoreAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var serviceProvder = MainWindowCommand.Instance.ServiceProvider;
            var svc = await serviceProvder.GetServiceAsync(typeof(SVsSettingsManager)) as IVsSettingsManager;

            var settingsManager = new ShellSettingsManager(svc);
            return settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);
        }

        internal static async Task<WritableSettingsStore> GetWritableSettingsStoreAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var serviceProvder = MainWindowCommand.Instance.ServiceProvider;
            var svc = await serviceProvder.GetServiceAsync(typeof(SVsSettingsManager)) as IVsSettingsManager;

            var settingsManager = new ShellSettingsManager(svc);
            return settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
        }

        internal static void WriteBoolean(this WritableSettingsStore settingsStore, string collectionName, string propertyName, bool value)
        {
            if (!settingsStore.CollectionExists(collectionName))
            {
                settingsStore.CreateCollection(collectionName);
            }

            settingsStore.SetBoolean(collectionName, propertyName, value);
        }

        internal static bool TryGetBoolean(this SettingsStore settingsStore, string collectionName, string propertyName, out bool value)
        {
            if (settingsStore.CollectionExists(collectionName))
            {
                value = settingsStore.GetBoolean(collectionName, propertyName);
                return true;
            }

            value = default;
            return false;
        }

        internal static bool TryGetInt(this SettingsStore settingsStore, string collectionName, string propertyName, out int value)
        {
            if (settingsStore.CollectionExists(collectionName))
            {
                value = settingsStore.GetInt32(collectionName, propertyName);
                return true;
            }

            value = default;
            return false;
        }
    }
}
