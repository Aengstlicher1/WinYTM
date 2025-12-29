using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WinYTM.Classes;
using Wpf.Ui.Controls;
using YouTubeApi;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace WinYTM
{
    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
    {
        public bool isMediaPlaying { get; set; }
        public YouTube.PaginatedResults? SearchResults { get; set; }
        public YoutubeExplode.YoutubeClient YTClient { get; } = new();

        public Song? currentSong;

        public MainWindow()
        {
            InitializeComponent();
            Wpf.Ui.Appearance.ApplicationThemeManager.ApplySystemTheme();
            Loaded += MainWindow_Loaded;
            MediaVolumeSlider.Value = MediaVolumeSlider.Maximum / 2;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this, Wpf.Ui.Controls.WindowBackdropType.Mica);
            VisualsLoop();
        }

        public async Task SetSong(Song song)
        {
            if (currentSong != null)
            {
                if (currentSong.Url == song.Url)
                {
                    MediaPlayer.Position = TimeSpan.Zero;
                    StartSong();
                    return;
                }
            }

            currentSong = song;

            var streamManifest = await YTClient.Videos.Streams.GetManifestAsync(song.Url);
            var streamInfo = streamManifest.GetAudioStreams().GetWithHighestBitrate();

            MediaPlayer.Source = new Uri(streamInfo.Url);

            StartSong();
        }

        public void StartSong()
        {
            MediaPlayer.Play();
            isMediaPlaying = true;
            MediaToggleButton.Icon = new SymbolIcon(SymbolRegular.Pause28) { FontSize = 20 };
        }

        public void PauseSong()
        {
            MediaPlayer.Pause();
            isMediaPlaying = false;
            MediaToggleButton.Icon = new SymbolIcon(SymbolRegular.Play28) { FontSize = 20 };
        }

        private async void VisualsLoop()
        {
            while (true)
            {
                if (NavView.CanGoBack)
                {
                    NavigationBackButton.SetResourceReference(ForegroundProperty, "TextFillColorPrimaryBrush");
                    NavigationBackButton.IsHitTestVisible = true;
                }
                else
                {
                    NavigationBackButton.SetResourceReference(ForegroundProperty, "TextFillColorDisabledBrush");
                    NavigationBackButton.IsHitTestVisible = false;
                }
                await Task.Delay(50);
            }
        }

        private void MediaControlButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.Button button)
            {
                switch (button.Name)
                {
                    case "MediaToggleButton":
                        if (!isMediaPlaying) { StartSong(); } else { PauseSong(); }
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
        public event Action<CancellationToken>? OnSearchCompleted;
        public event Action<CancellationToken>? OnSearchCleared;

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchQuery = SearchBox.Text;

            _searchCTS.Cancel();
            _searchCTS = new CancellationTokenSource();
            var token = _searchCTS.Token;

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                SearchResults = null;
                OnSearchCleared!.Invoke(token);
                return;
            }

            try
            {
                await Task.Delay(50, token);

                NavView.Navigate(typeof(View.SearchPage));

                var results = await YouTube.SearchYouTubeMusic(searchQuery, YouTube.MusicSearchFilter.Songs, token);
                token.ThrowIfCancellationRequested();

                SearchResults = results;
                OnSearchCompleted!.Invoke(token);
            }
            catch (OperationCanceledException) { }
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            NavView.Navigate(typeof(View.SearchPage));
        }

        public double VolumePercent = 1d;

        private void MediaVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MediaPlayer.Volume = e.NewValue / (10 * VolumePercent);
        }
    }
}