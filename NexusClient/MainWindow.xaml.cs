namespace Nexus.Client
{
    using System.Windows;

    /// <summary>
    /// Interaction logic for MainForm.xaml.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Show();
        }

        /// <summary>
        /// Brings <see cref="MainWindow"/> to foreground.
        /// </summary>
        public void BringToForeground(string[] args)
        {
            if (WindowState == WindowState.Minimized || Visibility == Visibility.Hidden)
            {
                Show();
                WindowState = WindowState.Normal;
            }

            // According to some sources these steps guarantee that an app will be brought to foreground.
            Activate();
            Topmost = true;
            Topmost = false;
            Focus();

            MessageBox.Show(string.Join(", ", args), "Forwarded arguments");
        }
    }
}
