namespace Nexus.Client.Games
{
    using System.Drawing;
	using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Color = System.Drawing.Color;

    using Nexus.UI;

	/// <summary>
	/// Describes visual features of an application.
	/// </summary>
	public class Theme
	{
        /// <summary>
		/// Gets the icon to use for this theme.
		/// </summary>
		/// <value>The icon to use for this theme.</value>
		public Icon Icon
		{
			get;
        }

		/// <summary>
		/// Used for WPF views.
		/// </summary>
        public ImageSource IconImageSource
        {
            get
            {
                if (Icon == null)
                {
                    return null;
                }

                return Imaging.CreateBitmapSourceFromHIcon(
                    Icon.Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
        }

		/// <summary>
		/// Gets the theme's font sets.
		/// </summary>
		/// <value>The theme's font sets.</value>
		public FontSetGroup FontSets
		{
			get;
        }

		/// <summary>
		/// Gets the theme's primary color.
		/// </summary>
		/// <value>The theme's primary color.</value>
		public Color PrimaryColor
		{
			get;
        }

		/// <summary>
		/// A simple constructor that initializes the theme.
		/// </summary>
		/// <param name="icon">The icon to use for this theme.</param>
		/// <param name="primaryColor">The theme's primary color.</param>
		/// <param name="fontSetGroup">The theme's font sets.</param>
		public Theme(Icon icon, Color primaryColor, FontSetGroup fontSetGroup)
		{
			Icon = icon;
			PrimaryColor = primaryColor;
			FontSets = fontSetGroup ?? new FontSetGroup();
		}
	}
}