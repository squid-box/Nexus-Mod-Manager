namespace Nexus.Client.UI.GameModeSelection
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;

    using Nexus.Client.Games;

	/// <summary>
	/// A dummy game mode descriptor that is used as a placeholder
	/// for the rescan item in the game list.
	/// </summary>
	internal class RescanGameModeDescriptor : IGameModeDescriptor
	{
        internal const string RescanInstalledGames = "__rescaninstalledgames";

        /// <inheritdoc />
		public string Name => "Rescan Installed Games";

        /// <inheritdoc />
		public string ModeId => RescanInstalledGames;

        /// <inheritdoc />
		public string[] GameExecutables => throw new NotImplementedException();

        /// <inheritdoc />
		public string InstallationPath => throw new NotImplementedException();

		/// <inheritdoc />
		public string SecondaryInstallationPath => throw new NotImplementedException();

        /// <inheritdoc />
		public virtual IEnumerable<string> PluginExtensions => throw new NotImplementedException();

        /// <inheritdoc />
		public virtual IEnumerable<string> StopFolders => throw new NotImplementedException();

        /// <inheritdoc />
		public string ExecutablePath => throw new NotImplementedException();

        /// <inheritdoc />
		public string[] OrderedCriticalPluginNames => throw new NotImplementedException();

        /// <inheritdoc />
		public string[] OrderedOfficialPluginNames => throw new NotImplementedException();

        /// <inheritdoc />
		public string[] OrderedOfficialUnmanagedPluginNames => throw new NotImplementedException();

        /// <inheritdoc />
		public string RequiredToolName => throw new NotImplementedException();

        /// <inheritdoc />
		public string[] OrderedRequiredToolFileNames => throw new NotImplementedException();

        /// <inheritdoc />
		public string RequiredToolErrorMessage => throw new NotImplementedException();

        /// <inheritdoc />
		public Theme ModeTheme => new Theme(null, Color.Black, null);

        /// <inheritdoc />
		public virtual string CriticalFilesErrorMessage => null;

        /// <inheritdoc />
		public virtual string PluginDirectory => throw new NotImplementedException();
    }
}
