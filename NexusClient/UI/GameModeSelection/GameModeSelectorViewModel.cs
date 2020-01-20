namespace Nexus.Client.UI.GameModeSelection
{
	using System.Collections.Generic;

	using Games;
	using Settings;

	public class GameModeSelectorViewModel
	{
        private readonly ISettings _settings;

		public GameModeSelectorViewModel(IEnumerable<IGameModeDescriptor> gameModes, ISettings settings)
		{
			GameModes = gameModes;
			_settings = settings;
		}

		/// <summary>
		/// Whether or not the Remember Selected GameMode checkbox is checked.
		/// </summary>
		public bool RememberSelectedGameMode { get; set; }

		/// <summary>
		/// The currently selected GameMode ID.
		/// </summary>
		public string SelectedGameModeId { get; set; }

		public IEnumerable<IGameModeDescriptor> GameModes { get; }

		/// <summary>
		/// Sets the selected game mode.
		/// </summary>
		/// <remarks>
		/// This makes the mod manager remember the selected game.
		/// </remarks>
		public void SaveChoice()
		{
            if (SelectedGameModeId.Equals(RescanGameModeDescriptor.RescanInstalledGames))
            {
                SelectedGameModeId = null;
            }
            else
            {
				_settings.RememberGameMode = RememberSelectedGameMode;
                _settings.RememberedGameMode = SelectedGameModeId;
                _settings.Save();
			}
        }
	}
}
