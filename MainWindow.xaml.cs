using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WinYTM.Classes;
using Wpf.Ui.Controls;
using YouTubeApi;
using YoutubeExplode;
using YoutubeExplode.Search;

namespace WinYTM
{
    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
    {
        public NavigationView navView => NavView;
        public MediaElement mediaPlayer => MediaPlayer;
        public bool isMediaPlaying { get; set; }
        public IAsyncEnumerable<YoutubeExplode.Search.VideoSearchResult>? SearchResults { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            Wpf.Ui.Appearance.ApplicationThemeManager.ApplySystemTheme();
            Loaded += MainWindow_Loaded;
            
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this, Wpf.Ui.Controls.WindowBackdropType.Mica);
            _ = NavViewBackButtonLoop();
        }

        private async Task NavViewBackButtonLoop()
        {
            while (true)
            {
                if (NavView.CanGoBack)
                {
                    NavigationBackButton.Foreground = new SolidColorBrush(Colors.White);
                    NavigationBackButton.IsHitTestVisible = true;
                }
                else
                {
                    NavigationBackButton.Foreground = new SolidColorBrush(Colors.Gray);
                    NavigationBackButton.IsHitTestVisible = false;
                }
                await Task.Delay(200);
            }
        }

        private void MediaControlButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.Button button)
            {
                switch (button.Name)
                {
                    case "MediaToggleButton":
                        isMediaPlaying = !isMediaPlaying;
                        button.Icon = isMediaPlaying ? new SymbolIcon(SymbolRegular.Pause28) { FontSize = 20 } : new SymbolIcon(SymbolRegular.Play28) { FontSize = 20 };
                        if (isMediaPlaying) { MediaPlayer.Play(); } else { MediaPlayer.Pause(); }
                        break;


                    case "MediaForwardButton":
                        break;


                    case "MediaBackButton":
                        break;
                }
            }
        }

        private void NavigationBackButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavView.CanGoBack) { NavView.GoBack(); }
        }

        public CancellationTokenSource _searchCTS = new CancellationTokenSource();

        public YoutubeClient _youtube = new();

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchCTS.Cancel();
            _searchCTS.Dispose();
            _searchCTS = new CancellationTokenSource();

            string searchQuery = SearchBox.Text;
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                SearchResults = null;
                return;
            }
            
            NavView.NavigateWithHierarchy(typeof(View.SearchPage));
            SearchResults = _youtube.Search.GetVideosAsync(searchQuery, _searchCTS.Token);
        }
    }
}