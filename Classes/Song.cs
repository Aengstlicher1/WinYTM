using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using WinYTM.View;
using Wpf.Ui.Abstractions.Controls;
using YouTubeApi;

namespace WinYTM.Classes
{
    public class Song
    {
        public required string Url { get; set; }
        public required string Title { get; set; }
        public required string Artist { get; set; }
        public required YouTube.Video Media { get; set; }
    }

    public class SongCard : Wpf.Ui.Controls.Button, INavigationAware
    {
        public Song Song { get; set; }


        private Helpers.Cover coverData;
        private bool isIndicatorEnabled;
        private MainWindow? MainWindow => Application.Current.MainWindow as MainWindow;
        private Wpf.Ui.Controls.SymbolIcon playStateIndicator;
        private Wpf.Ui.Controls.Image thumbnailImage;
        private CancellationTokenSource checkToken = new();
        public SongCard(Song song, bool IndicatorEnabled = true, bool DurationVisible = true)
        {
            isIndicatorEnabled = IndicatorEnabled;
            Song = song;
            var Grid = new Grid()
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = new SolidColorBrush(Colors.Transparent),

            };
            Grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            Grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            Grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            this.SetResourceReference(StyleProperty, typeof(Wpf.Ui.Controls.Button));

            this.CornerRadius = new CornerRadius(12);
            this.SetResourceReference(BackgroundProperty, "ControlFillColorDefaultBrush");
            this.Height = 50;
            this.MinWidth = 200;
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            this.VerticalContentAlignment = VerticalAlignment.Stretch;
            this.Margin = new Thickness(8, 4, 12, 4);
            this.SetResourceReference(BorderBrushProperty, "ControlStrokeColorDefaultBrush");
            this.Padding = new Thickness(0);
            this.Content = Grid;
            this.Click += SongCard_Click;
            this.MouseDoubleClick += SongCard_MouseDoubleClick;
            (Application.Current.MainWindow as MainWindow)!.Closing += Application_Closing;
            this.ContextMenu = new ContextMenu()
            {
                Items =
                {
                    new MenuItem()
                    {
                        Header = "Play",
                        Icon = new Wpf.Ui.Controls.SymbolIcon(Wpf.Ui.Controls.SymbolRegular.Play20)
                    },
                }
            };


            coverData = new Helpers.Cover(song.Media.Thumbnails.MediumResUrl!, true);

            var imageGrid = new Grid();

            playStateIndicator = new Wpf.Ui.Controls.SymbolIcon()
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Symbol = Wpf.Ui.Controls.SymbolRegular.Play28,
                Filled = true,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 24,
                Visibility = Visibility.Collapsed,
            };


            thumbnailImage = new Wpf.Ui.Controls.Image()
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Width = this.Height - 8,
                Height = this.Height - 8,
                Background = new SolidColorBrush(Colors.Transparent),
                Margin = new Thickness(4),
                CornerRadius = new CornerRadius(8),
            };

            Binding imageBind = new Binding("Bitmap");
            imageBind.Source = coverData;
            imageBind.Mode = BindingMode.OneWay;
            BindingOperations.SetBinding(thumbnailImage, Wpf.Ui.Controls.Image.SourceProperty, imageBind);
            Grid.SetColumn(thumbnailImage, 0);

            imageGrid.Children.Add(thumbnailImage);
            imageGrid.Children.Add(playStateIndicator);

            var title = song.Title;
            int length = (int)Math.Round(Application.Current.MainWindow.ActualWidth / 8.5d);


            var titleText = new TextBlock()
            {
                Text = title,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(8, 4, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            titleText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
            Grid.SetRow(titleText, 0);

            var artist = song;
            var artistText = new TextBlock()
            {
                Text = song.Artist,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(8, 0, 8, 4),
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            artistText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorTertiaryBrush");
            Grid.SetRow(artistText, 1);

            var textStack = new Grid()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            Grid.SetColumn(textStack, 1);
            textStack.RowDefinitions.Add(new RowDefinition());
            textStack.RowDefinitions.Add(new RowDefinition());

            textStack.Children.Add(titleText);
            textStack.Children.Add(artistText);
            if (DurationVisible)
            {
                string duration = song.Media.Duration.ToString()!.Substring(3);
                if (song.Media.Duration > new TimeSpan(1, 0, 0))
                {
                    duration = song.Media.Duration.ToString();
                }
                var durationText = new TextBlock()
                {
                    Text = duration,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 4, 0),
                };
                durationText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorTertiaryBrush");
                Grid.SetColumn(durationText, 2);
                Grid.Children.Add(durationText);
            }

            Grid.Children.Add(imageGrid);
            Grid.Children.Add(textStack);

            _ = checkCurrentSong(checkToken.Token);
        }

        private void Application_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            checkToken.Cancel();
        }

        private async Task checkCurrentSong(CancellationToken token)
        {
            if (isIndicatorEnabled)
            {
                while (true)
                {
                    try
                    {
                        token.ThrowIfCancellationRequested();
                        if (MainWindow!.currentSong!.Url == Song.Url)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                playStateIndicator.Visibility = Visibility.Visible;
                                thumbnailImage.Opacity = 0.4d;
                            });
                        }
                        else
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                playStateIndicator.Visibility = Visibility.Collapsed;
                                thumbnailImage.Opacity = 1.0d;
                            });
                        }
                    }
                    catch { }
                    await Task.Delay(50);
                }
            }
        }

        private async void SongCard_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWin)
            {
                mainWin.PauseSong();
                _ = mainWin.SetSong(this.Song);
            }
        }

        private void SongCard_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWin)
            {
                mainWin.PauseSong();
                _ = mainWin.SetSong(this.Song);
                mainWin.NavView.Navigate(typeof(FullSongPage));
            }
        }

        Task INavigationAware.OnNavigatedFromAsync()
        {
            checkToken.Cancel();
            return Task.CompletedTask;
        }

        Task INavigationAware.OnNavigatedToAsync()
        {
            return Task.CompletedTask;
        }
    }
}
