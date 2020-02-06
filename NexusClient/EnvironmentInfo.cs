namespace Nexus.Client
{
    using System;
    using System.IO;
    using System.Windows.Forms;
    using Microsoft.Win32;
    using Settings;
    using Util;

    /// <summary>
    /// Provides information about the current program environment.
    /// </summary>
    public class EnvironmentInfo : IEnvironmentInfo
    {
        /// <inheritdoc />
        public string PersonalDataFolderPath { get; }

        /// <inheritdoc />
        public string ApplicationPersonalDataFolderPath { get; }

        /// <inheritdoc />
        public bool IsMonoMode => Type.GetType("Mono.Runtime") != null;

        /// <inheritdoc />
        public string TemporaryPath { get; }

        /// <inheritdoc />
        public string ProgramInfoDirectory => Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "data");

        /// <inheritdoc />
        public bool Is64BitProcess => IntPtr.Size == 8;

        /// <inheritdoc />
        public ISettings Settings { get; }

        /// <inheritdoc />
        public Version ApplicationVersion => new Version(CommonData.VersionString);

        /// <summary>
        /// A simple constructor that initializes the object with the given dependencies.
        /// </summary>
        /// <param name="settings">The application and user settings.</param>
        public EnvironmentInfo(ISettings settings)
        {
            Settings = settings;
            PersonalDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            
            if (string.IsNullOrEmpty(PersonalDataFolderPath))
            {
                PersonalDataFolderPath = Registry.GetValue(@"HKEY_CURRENT_USER\software\microsoft\windows\currentversion\explorer\user shell folders", "Personal", null).ToString();
            }

            if (string.IsNullOrEmpty(Settings.TempPathFolder))
            {
                TemporaryPath = Path.Combine(Path.GetTempPath(), Application.ProductName);
                
                if (!Directory.Exists(TemporaryPath))
                {
                    Directory.CreateDirectory(TemporaryPath);
                }
            }
            else
            {
                TemporaryPath = Settings.TempPathFolder;
            }

            ApplicationPersonalDataFolderPath = Path.Combine(PersonalDataFolderPath, settings.ModManagerName);
        }
    }
}
