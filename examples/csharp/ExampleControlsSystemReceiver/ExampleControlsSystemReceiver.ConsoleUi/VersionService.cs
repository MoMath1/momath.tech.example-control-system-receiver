using System.Reflection;

namespace ExampleControlsSystemReceiver.ConsoleUi
{
    public static class VersionService
    {
        public static Version? GetAppVersion()
        {
            return Assembly.GetEntryAssembly()?.GetName().Version;
        }
    }
}