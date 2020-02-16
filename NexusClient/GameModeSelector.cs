namespace Nexus.Client
{
    using System.Diagnostics;
    using System.Linq;
	using System.Text;
	using System.Windows.Forms;
	
	using Nexus.Client.Games;
    using Nexus.Client.UI.GameModeSelection;

	/// <summary>
	/// Chooses the current game mode.
	/// </summary>
	/// <remarks>
	/// This selector checks the following to select the current game mode:
	/// -command line
	/// -remembered game mode in settings
	/// -ask the user
	/// </remarks>
	public class GameModeSelector
	{
		#region Properties

        /// <summary>
		/// Gets or sets the game modes factories for supported games.
		/// </summary>
		/// <value>The game modes factories for supported games.</value>
		protected GameModeRegistry SupportedGameModes { get; set; }

		/// <summary>
		/// Gets or sets the game modes factories for installed games.
		/// </summary>
		/// <value>The game modes factories for installed games.</value>
		protected GameModeRegistry InstalledGameModes { get; set; }

        /// <summary>
        /// Gets whether a rescan of install games was requested.
        /// </summary>
        /// <value>Whether a rescan of install games was requested.</value>
        public bool RescanRequested => _gameModeSelectorViewModel != null && _gameModeSelectorViewModel.RescanRequested;

        #endregion

        private readonly IEnvironmentInfo _environmentInfo;

        private GameModeSelectorViewModel _gameModeSelectorViewModel;

		/// <summary>
		/// A simple constructor that initializes the object with the given dependencies.
		/// </summary>
		/// <param name="supportedGameModes">The games modes supported by the mod manager.</param>
		/// <param name="installedGameModes">The game modes factories for installed games.</param>
		/// <param name="environmentInfo">The application's environment info.</param>
		public GameModeSelector(GameModeRegistry supportedGameModes, GameModeRegistry installedGameModes, IEnvironmentInfo environmentInfo)
		{
			SupportedGameModes = supportedGameModes;
			InstalledGameModes = installedGameModes;
            _environmentInfo = environmentInfo;
		}

		/// <summary>
		/// Selects the current game mode.
		/// </summary>
		/// <param name="requestedGameMode">The id of the game mode we want to select.</param>
		/// <param name="changeDefaultGameMode">Whether the users has requested a change to the default game mode.</param>
		/// <returns>The <see cref="IGameModeFactory"/> that can build the game mode selected by the user.</returns>
		public IGameModeFactory SelectGameMode(string requestedGameMode, bool changeDefaultGameMode)
		{
			Trace.Write("Determining Game Mode: ");

			var selectedGameModeId = _environmentInfo.Settings.RememberGameMode ? _environmentInfo.Settings.RememberedGameMode : null;
			
            if (!string.IsNullOrEmpty(requestedGameMode))
			{
				Trace.Write($"(Requested Mode: {requestedGameMode}) ");
				selectedGameModeId = requestedGameMode;
			}

			if (changeDefaultGameMode || string.IsNullOrEmpty(selectedGameModeId))
			{
				Trace.Write("(From Selection Form) ");

				var gameModeInfos = InstalledGameModes.RegisteredGameModes.ToList();

                _gameModeSelectorViewModel = new GameModeSelectorViewModel(gameModeInfos, _environmentInfo.Settings);
				var gameModeSelectorView = new GameModeSelectorView(_gameModeSelectorViewModel);

                gameModeSelectorView.ShowDialog();

                if (gameModeSelectorView.DialogResult == true)
                {
                    if (RescanRequested)
                    {
						Trace.WriteLine("Rescan game modes.");
                        return null;
                    }

					selectedGameModeId = _gameModeSelectorViewModel.SelectedGameMode?.ModeId;
                }
                else
                {
                    Trace.WriteLine("None");

					return null;
                }
			}

			Trace.WriteLine(selectedGameModeId);

			if (!InstalledGameModes.IsRegistered(selectedGameModeId))
			{
				var stbError = new StringBuilder();
				
                if (!SupportedGameModes.IsRegistered(selectedGameModeId))
                {
                    stbError.AppendFormat("Unrecognized Game Mode: {0}", selectedGameModeId);
                }
                else
				{
					stbError.AppendFormat("{0} is not set up to work with {1}", _environmentInfo.Settings.ModManagerName, SupportedGameModes.GetGameMode(selectedGameModeId).GameModeDescriptor.Name).AppendLine();
					stbError.AppendFormat("If {0} is installed, rescan for installed games from the Change Game toolbar item.", SupportedGameModes.GetGameMode(selectedGameModeId).GameModeDescriptor.Name).AppendLine();
				}
				
                Trace.TraceError(stbError.ToString());
				MessageBox.Show(stbError.ToString(), "Unrecognized Game Mode", MessageBoxButtons.OK, MessageBoxIcon.Error);
				
                return null;
			}

			return InstalledGameModes.GetGameMode(selectedGameModeId);
		}
	}
}
