namespace Nexus.Client
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Windows;
    using Microsoft.Win32;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string MutexGuid = "6af12c54-643b-4752-87d0-8335503010de";
        private const string EventGuid = "442694f2-4676-4ae4-a97b-12566f5a9f7f";
        private const string FileGuid = "167bd7e8-32c1-4327-bc91-d46b94704b48";

        private EventWaitHandle _eventWaitHandle;
        private MemoryMappedFile _memoryMappedFile;

        private Mutex _mutex;

        /// <summary>
        /// Main entry of NMM.
        /// </summary>
        /// <remarks>Uses single-instance code from https://stackoverflow.com/a/23730146 with some modification.</remarks>
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            _mutex = new Mutex(true, MutexGuid, out var firstInstance);
            _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, EventGuid);
            _memoryMappedFile =  MemoryMappedFile.CreateOrOpen(FileGuid, 65536, MemoryMappedFileAccess.ReadWrite);

            GC.KeepAlive(_mutex);

            if (firstInstance)
            {
                // Spawn a thread which will be waiting for our event
                var thread = new Thread(
                    () =>
                    {
                        while (_eventWaitHandle.WaitOne())
                        {
                            // Read arguments from other instance from memory mapped file.
                            using (var fileAccessor = _memoryMappedFile.CreateViewAccessor())
                            {
                                var length = fileAccessor.ReadUInt16(0);
                                var data = new byte[length];
                                fileAccessor.ReadArray(2, data, 0, data.Length);
                                var args = Encoding.UTF8.GetString(data).Split('\n');

                                Dispatcher?.BeginInvoke(
                                    (Action)(() => ((MainWindow)Current.MainWindow)?.BringToForeground(args)));
                            }
                        }
                    })
                {
                    // It is important mark it as background otherwise it will prevent app from exiting.
                    IsBackground = true
                };
                
                thread.Start();
            }
            else
            {
                // Put arguments in memory mapped file to be read by existing process.
                var data = Encoding.UTF8.GetBytes(string.Join("\n", e.Args));
                var fileAccessor = _memoryMappedFile.CreateViewAccessor();
                fileAccessor.Write(0, (ushort)data.Length);
                fileAccessor.WriteArray(2, data, 0, data.Length);

                // Notify other instance so it could bring itself to foreground.
                _eventWaitHandle.Set();

                // Terminate this instance.
                Shutdown();
            }

            // Start the actual application.

            var environmentInfo = new EnvironmentInfo(Client.Properties.Settings.Default);

            if (!Directory.Exists(environmentInfo.ApplicationPersonalDataFolderPath))
            {
                Directory.CreateDirectory(environmentInfo.ApplicationPersonalDataFolderPath);
            }

            EnableTracing(environmentInfo, true);

            var bootstrapper = new Bootstrapper(environmentInfo);

            bootstrapper.RunMainForm(e.Args);
        }

        /// <summary>
		/// Turns on tracing.
		/// </summary>
		/// <param name="environmentInfo">The application's environment info.</param>
		/// <param name="forceTrace">Whether to force the trace file to be written.</param>
		private static void EnableTracing(IEnvironmentInfo environmentInfo, bool forceTrace)
        {
            Trace.AutoFlush = true;
            var traceLogFilename = $"TraceLog{DateTime.Now:yyyyMMddHHmmss}.txt";

            TextWriterTraceListener traceListener = forceTrace ? new HeaderlessTextWriterTraceListener(Path.Combine(string.IsNullOrEmpty(environmentInfo.Settings.TraceLogFolder) ? environmentInfo.ApplicationPersonalDataFolderPath : environmentInfo.Settings.TraceLogFolder, traceLogFilename)) : new HeaderlessTextWriterTraceListener(new MemoryStream(), Path.Combine(String.IsNullOrEmpty(environmentInfo.Settings.TraceLogFolder) ? environmentInfo.ApplicationPersonalDataFolderPath : environmentInfo.Settings.TraceLogFolder, traceLogFilename));

            traceListener.Name = "DefaultListener";
            Trace.Listeners.Add(traceListener);
            Trace.TraceInformation("Trace file has been created: " + traceLogFilename);

            var status = new StringBuilder();
            status.AppendLine($"Mod Manager Version: {Assembly.GetExecutingAssembly().GetName().Version}{(environmentInfo.IsMonoMode ? "(mono)" : "")}");
            status.AppendLine($"OS version: {Environment.OSVersion}");

            status.AppendLine("Installed .NET Versions:");
            var installedVersions = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP");
            var versionNames = installedVersions?.GetSubKeyNames();

            if (versionNames != null)
            {
                foreach (var frameworkVersion in versionNames)
                {
                    var servicePack = installedVersions?.OpenSubKey(frameworkVersion)?.GetValue("SP", 0).ToString();
                    status.AppendLine($"\t{frameworkVersion} SP {servicePack}");
                }
            }

            using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\"))
            {
                if (ndpKey != null)
                {
                    var releaseKey = Convert.ToInt32(ndpKey.GetValue("Release"));
                    status.AppendLine($"\tv4.5: {DetermineDotNetVersion(releaseKey)}");
                }
                else
                {
                    status.AppendLine("\tv4.5: Not found.");
                }
            }

            status.AppendLine($"Tracing is forced: {forceTrace}");
            Trace.TraceInformation(status.ToString());
        }

        private static string DetermineDotNetVersion(int releaseKey)
        {
            if (releaseKey >= 381029)
            {
                return "4.6 or later";
            }

            if (releaseKey >= 379893)
            {
                return "4.5.2 or later";
            }

            if (releaseKey >= 378758)
            {
                return "4.5.1 or later";
            }

            if (releaseKey >= 378675)
            {
                return "4.5.1 with Windows 8.1";
            }

            if (releaseKey >= 378389)
            {
                return "4.5 or later";
            }

            return "No 4.5 or later version detected";
        }
    }
}
