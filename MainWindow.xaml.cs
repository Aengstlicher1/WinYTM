using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using WinYTM.Classes;
using Wpf.Ui.Controls;
using YouTubeApi;
using YoutubeExplode;

namespace WinYTM
{
    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
    {
        public bool isMediaPlaying { get; set; }
        public YouTube.PaginatedResults? SearchResults { get; set; }
        public Song? currentSong { get; set; }
        public bool isRepeating { get; set; } = true;
        public bool isScrubbing => MediaDurationSlider.IsMouseCaptureWithin;

        private bool isDebugVisible { get; set; } = false;

        public MainWindow()
        {
            InitializeComponent();
            Wpf.Ui.Appearance.ApplicationThemeManager.ApplySystemTheme();
            Loaded += MainWindow_Loaded;
            MediaVolumeSlider.Value = MediaVolumeSlider.Maximum / 2;

            if (isDebugVisible)
            {
                DebugGrid.Visibility = Visibility.Visible;
                DebugLoop();
            }
        }

        private async void DebugLoop()
        {
            while (true)
            {
                // MediaPlayer
                DebugPlayerDuration.Text = "Duration: " + MediaPlayer.NaturalDuration;
                DebugPlayerPosition.Text = "Position: " + MediaPlayer.Position;
                DebugPlayerVolume.Text = "Volume: " + MediaPlayer.Volume;

                // Scrubber
                DebugScrubberValue.Text = "Value: " + MediaDurationSlider.Value;
                DebugScrubberMaximum.Text = "Maximum: " + MediaDurationSlider.Maximum;

                await Task.Delay(100);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this, Wpf.Ui.Controls.WindowBackdropType.Mica);
            VisualsLoop();
            ScrubberLoop();
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

            var streams = await YouTube.GetStreamInfo(song.Media.VideoId);

            MediaPlayer.Source = new Uri(streams[0].Url);

            MediaDurationSlider.Value = 0d;

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
            if (MediaPlayer.CanPause) MediaPlayer.Pause();
            isMediaPlaying = false;
            MediaToggleButton.Icon = new SymbolIcon(SymbolRegular.Play28) { FontSize = 20 };
        }

        private async void ScrubberLoop()
        {
            while (true)
            {
                if (isScrubbing)
                {
                    if (currentSong != null)
                    {
                        if (MediaPlayer.CanPause)
                        {
                            try
                            {
                                MediaPlayer.IsMuted = true;
                                var _val = MediaDurationSlider.Value;
                                var _duration = MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;

                                MediaPlayer.Position = new TimeSpan(0, 0, (int)Math.Floor((int)_duration * (_val / 100)));
                            }   catch { }
                        }
                    }
                }
                else
                {
                    if (currentSong != null)
                    {
                        if (MediaPlayer.CanPause)
                        {
                            try
                            {
                                MediaPlayer.IsMuted = false;
                                var _pos = MediaPlayer.Position.TotalSeconds;
                                var _duration = (int)MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;

                                MediaDurationSlider.Value = Helpers.getPercent(_pos, _duration);
                            }   catch { }
                        }
                    }
                }
                await Task.Delay(50);
            }
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

                    case "MediaRepeatButton":
                        MediaRepeatButton.Icon = !isRepeating
                            ? new SymbolIcon(SymbolRegular.ArrowRepeatAll24) { FontSize = 20 } 
                            : new SymbolIcon(SymbolRegular.ArrowRepeatAllOff24) { FontSize = 20 };
                        isRepeating = !isRepeating;
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
            MediaPlayer.Volume = e.NewValue / (10 + VolumePercent * 100);
        }

        private async void MediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (isRepeating)
            {
                await Task.Delay(100);
                MediaPlayer.Position = TimeSpan.Zero;
            }
            else
            {
                PauseSong();
            }
        }
    }
}