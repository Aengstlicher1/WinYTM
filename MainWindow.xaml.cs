using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using WinYTM.Classes;
using Wpf.Ui.Controls;
using YouTubeApi;

namespace WinYTM
{
    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
    {
        public bool isMediaPlaying { get; set; }
        public bool isMediaLoaded { get; set; }
        public double last_volume { get; set; } = 0;
        public YouTube.PaginatedResults? SearchResults { get; set; }
        public Song? currentSong { get; set; }
        public bool isScrubbing => MediaDurationSlider.IsMouseCaptureWithin;
        public Config.AppConfig AppConfig { get; set; }

        private string? currentTempFile { get; set; } = null;
        private bool isDebugVisible { get; set; } = false;

        public MainWindow()
        {
            InitializeComponent();
            Wpf.Ui.Appearance.ApplicationThemeManager.ApplySystemTheme();
            Loaded += MainWindow_Loaded;

            if (isDebugVisible)
            {
                DebugGrid.Visibility = Visibility.Visible;
                DebugLoop();
            }

            if (!File.Exists(Config.Location))
            {
                Config.WriteSettings(new Config.AppConfig());
            }
            AppConfig = Config.ReadSettings();
            SetConfig();

            SaveConfigLoop();
        }

        private void SetConfig()
        {
            if (AppConfig.LastSong != null) _ = SetSong(AppConfig.LastSong, false);

            MediaRepeatButton.Icon = AppConfig.isMediaRepeating
                ? new SymbolIcon(SymbolRegular.ArrowRepeatAll24) { FontSize = 20 }
                : new SymbolIcon(SymbolRegular.ArrowRepeatAllOff24) { FontSize = 20 };

            MediaShuffleButton.Icon = AppConfig.isMediaShuffled
                ? new SymbolIcon(SymbolRegular.ArrowShuffle24) { FontSize = 20 }
                : new SymbolIcon(SymbolRegular.ArrowShuffleOff24) { FontSize = 20 };

            SetMediaVolumeButton();
            MediaVolumeSlider.Value = AppConfig.volume;
        }

        private async void SaveConfigLoop()
        {
            while (true)
            {
                AppConfig.LastSong = currentSong;
                AppConfig.volume = MediaVolumeSlider.Value;
                Config.WriteSettings(AppConfig);
                await Task.Delay(200);
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

        public async Task SetSong(Song song, bool autoStart = true)
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

            try { File.Delete(currentTempFile!); } catch { }
            currentTempFile = Path.GetTempFileName() + ".mp4";
            var client = new System.Net.Http.HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            var response = await client.GetAsync(streams[0].Url);

            using var FileStream = File.Create(currentTempFile);
            await response.Content.CopyToAsync(FileStream);

            MediaPlayer.Source = new Uri(currentTempFile);

            //MediaPlayer.Source = new Uri(streams[0].Url);
            MediaPlayer.MediaEnded += (s, e) =>
            {
                if (AppConfig.isMediaRepeating)
                {
                    Thread.Sleep(200);
                    MediaPlayer.Position = TimeSpan.Zero;
                }
                else
                {
                    PauseSong();
                }
            };


            isMediaLoaded = true;
            MediaDurationSlider.Value = 0d;
            string duration = song.Media.Duration.ToString().Substring(3);
            if (song.Media.Duration.Hours >= 1)
            {
                duration = song.Media.Duration.ToString();
            }
            MediaDurationFull.Text = duration;

            MediaSongGrid.Children.Clear();
            MediaSongGrid.Children.Add(new SongCard(song, false, false) { IsHitTestVisible = false, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness() });

            if (autoStart) StartSong();
        }

        public void StartSong()
        {
            if (!isMediaPlaying && isMediaLoaded)
            {
                MediaPlayer.Play();
                isMediaPlaying = true;
                MediaToggleButton.Icon = new SymbolIcon(SymbolRegular.Pause28) { FontSize = 20 };
            }
        }

        public void PauseSong()
        {
            if (MediaPlayer.CanPause)
            {
                MediaPlayer.Pause();
                isMediaPlaying = false;
                MediaToggleButton.Icon = new SymbolIcon(SymbolRegular.Play28) { FontSize = 20 };
            }
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
                                if (!AppConfig.isMediaMuted)
                                {
                                    MediaPlayer.IsMuted = true;
                                }
                                var _val = MediaDurationSlider.Value;
                                var _duration = MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;

                                MediaPlayer.Position = new TimeSpan(0, 0, (int)Math.Floor((int)_duration * (_val / 100)));
                            }
                            catch { }
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
                                if (!AppConfig.isMediaMuted)
                                {
                                    MediaPlayer.IsMuted = false;
                                }
                                var _pos = MediaPlayer.Position.TotalSeconds;
                                var _duration = (int)MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;

                                MediaDurationSlider.Value = Helpers.getPercent(_pos, _duration);
                            }
                            catch { }
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
                        MediaRepeatButton.Icon = !AppConfig.isMediaRepeating
                            ? new SymbolIcon(SymbolRegular.ArrowRepeatAll24) { FontSize = 20 }
                            : new SymbolIcon(SymbolRegular.ArrowRepeatAllOff24) { FontSize = 20 };
                        AppConfig.isMediaRepeating = !AppConfig.isMediaRepeating;
                        break;
                    case "MediaVolumeButton":
                        AppConfig.isMediaMuted = !AppConfig.isMediaMuted;
                        SetMediaVolumeButton();

                        break;
                    case "MediaShuffleButton":
                        AppConfig.isMediaShuffled = !AppConfig.isMediaShuffled;
                        MediaShuffleButton.Icon = AppConfig.isMediaShuffled
                            ? new SymbolIcon(SymbolRegular.ArrowShuffle24) { FontSize = 20 }
                            : new SymbolIcon(SymbolRegular.ArrowShuffleOff24) { FontSize = 20 };
                        break;
                }
            }
        }

        private void SetMediaVolumeButton()
        {
            MediaVolumeButton.Icon = AppConfig.isMediaMuted
                ? new SymbolIcon(SymbolRegular.Speaker024) { FontSize = 20 }
                : new SymbolIcon(SymbolRegular.Speaker224) { FontSize = 20 };
            MediaPlayer.IsMuted = AppConfig.isMediaMuted;

            if (AppConfig.isMediaMuted)
            {
                last_volume = MediaVolumeSlider.Value;
                MediaVolumeSlider.Value = 0;
            }
            else
            {
                if (last_volume == 0)
                {
                    MediaVolumeSlider.Value = 50;
                    return;
                }
                MediaVolumeSlider.Value = last_volume;
            }
        }

        private void NavigationBackButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavView.CanGoBack) { NavView.GoBack(); }
        }

        public CancellationTokenSource _searchCTS = new CancellationTokenSource();
        public event Action<CancellationToken>? OnSearchCompleted;
        public event Action<CancellationToken>? OnSearchCleared;

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _ = Search(SearchBox.Text);
        }

        private async Task Search(string searchQuery)
        {
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

                Dispatcher.Invoke(() => { NavView.Navigate(typeof(View.SearchPage)); });

                var results = await YouTube.SearchYouTubeMusic(searchQuery, YouTube.MusicSearchFilter.Songs, token);
                token.ThrowIfCancellationRequested();

                Dispatcher.Invoke(() => { SearchResults = results; });
                OnSearchCompleted!.Invoke(token);
            }
            catch (OperationCanceledException) { }
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            NavView.Navigate(typeof(View.SearchPage));
        }

        private void MediaVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MediaPlayer.Volume = e.NewValue / 1000;

            if (AppConfig.isMediaMuted && MediaPlayer.Volume != 0)
            {
                AppConfig.isMediaMuted = false;
                MediaVolumeButton.Icon = new SymbolIcon(SymbolRegular.Speaker224) { FontSize = 20 };
                MediaPlayer.IsMuted = false;
            }

            if (MediaPlayer.Volume == 0 && !AppConfig.isMediaMuted)
            {
                AppConfig.isMediaMuted = true;
                SetMediaVolumeButton();
            }
        }

        private void MediaDurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (currentSong != null)
            {
                MediaDurationCurrent.Text = new TimeSpan(0, 0, (int)(currentSong.Media.Duration.TotalSeconds * (e.NewValue / 100))).ToString().Remove(0, 3);
            }
        }

        private void MediaPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Debug.WriteLine("!!!!  Media Failed: " + e.ErrorException.Message + " !!!!");
        }
    }
}