using System.Windows;
using System.Windows.Input;

namespace wpf2048App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const int MinCellHeight = 50;
        public int MyMinWidth { get { return 4 * MinCellHeight; } }
        public int MyMinHeight { get { return (int)SystemParameters.CaptionHeight + MyMinWidth; } }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void gameboard_KeyDown(object sender, KeyEventArgs e)
        {
            gameboard.Game_KeyDown(sender, e);
        }

        private void click_Reset(object sender, RoutedEventArgs e)
        {
            gameboard.Restart();
        }
    }
}
