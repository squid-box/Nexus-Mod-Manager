namespace Nexus.Client.UI.GameModeSelection
{
    using System;
    using System.Windows;

    using Nexus.Client.Games;


	/// <summary>
	/// Interaction logic for GameModeSelectorView.xaml
	/// </summary>
	public partial class GameModeSelectorView : Window
	{
		private readonly GameModeSelectorViewModel _viewModel;

		public GameModeSelectorView(GameModeSelectorViewModel viewModel)
		{
			_viewModel = viewModel;
			DataContext = _viewModel;

			InitializeComponent();
		}

        public void Cancel(object sender, RoutedEventArgs args)
        {
            DialogResult = false;

            Close();
        }

        public void Accept(object sender, RoutedEventArgs args)
        {
			_viewModel.SelectedGameMode = GameModeList.SelectedItem as IGameModeDescriptor;
			_viewModel.SaveChoice();
            DialogResult = true;

            Close();
        }

        public void RescanGameModes(object sender, RoutedEventArgs args)
        {
            _viewModel.RescanRequested = true;
            DialogResult = true;

            Close();
        }
	}
}
