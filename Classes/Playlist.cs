using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using WinYTM.View;
using Wpf.Ui.Abstractions.Controls;
using YouTubeApi;
using static YouTubeApi.YouTube;

namespace WinYTM.Classes
{
    public class Playlist
    {
        public required YouTube.Playlist media { get; set; }
        public List<Song> Songs { get; set; } = new List<Song>();
    }
    public class PlaylistCard : Wpf.Ui.Controls.Button, INavigationAware
    {
        public Playlist Playlist { get; set; }


        private Helpers.Cover coverData;
        private bool isIndicatorEnabled;
        private MainWindow? MainWindow => Application.Current.MainWindow as MainWindow;
        private Wpf.Ui.Controls.SymbolIcon playStateIndicator;
        private Wpf.Ui.Controls.Image thumbnailImage;
        private CancellationTokenSource checkToken = new();
        public PlaylistCard(Playlist playlist, bool IndicatorEnabled = true, bool AmountVisible = true)
        {
            isIndicatorEnabled = IndicatorEnabled;
            Playlist = playlist;
            var Grid = new Grid()
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = new SolidColorBrush(Colors.Transparent),
            };
            Grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            Grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            Grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            Grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

            this.SetResourceReference(StyleProperty, typeof(Wpf.Ui.Controls.Button));

            this.CornerRadius = new CornerRadius(12);
            this.SetResourceReference(BackgroundProperty, "ControlFillColorDefaultBrush");
            this.Height = 140;
            this.Width = 120;
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
                        Icon = new Wpf.Ui.Controls.SymbolIcon(Wpf.Ui.Controls.SymbolRegular.Play20),
                    },
                }
            };

            setSongs();

            coverData = new Helpers.Cover(Playlist.media.Thumbnails.HighResUrl!, true);

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
                Width = 132,
                Height = 132,
                Margin = new Thickness(4),
                CornerRadius = new CornerRadius(8),
            };

            Binding imageBind = new Binding("Bitmap");
            imageBind.Source = coverData;
            imageBind.Mode = BindingMode.OneWay;
            BindingOperations.SetBinding(thumbnailImage, Wpf.Ui.Controls.Image.SourceProperty, imageBind);
            Grid.SetColumn(imageGrid, 0);
            Grid.SetColumnSpan(imageGrid, 3);
            Grid.SetRow(imageGrid, 0);

            imageGrid.Children.Add(thumbnailImage);
            imageGrid.Children.Add(playStateIndicator);

            var title = Playlist.media.Title;
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

            var author = Playlist.media.Author;
            var authorText = new TextBlock()
            {
                Text = author,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(8, 0, 8, 4),
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            authorText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorTertiaryBrush");
            Grid.SetRow(authorText, 1);

            var textStack = new Grid()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            textStack.RowDefinitions.Add(new RowDefinition());
            textStack.RowDefinitions.Add(new RowDefinition());
            Grid.SetColumn(textStack, 0);
            Grid.SetColumnSpan(textStack, 2);
            Grid.SetRow(textStack, 1);

            textStack.Children.Add(titleText);
            textStack.Children.Add(authorText);

            Grid.Children.Add(imageGrid);
            Grid.Children.Add(textStack);

            if (AmountVisible)
            {
                string amount = playlist.media.VideoCount.ToString()!;
                var amountText = new TextBlock()
                {
                    Text = amount,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 4, 0),
                };
                amountText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorTertiaryBrush");
                Grid.SetColumn(amountText, 2);
                Grid.SetRow(amountText, 1);
                Grid.Children.Add(amountText);
            }

            _ = checkCurrentSong(checkToken.Token);
        }

        private async void setSongs()
        {
            var paginatedResults = await YouTube.GetPlaylistVideos(Playlist.media.PlaylistId);

            Playlist.Songs.AddRange(await getSongs(paginatedResults.CurrentPage));
            while (!paginatedResults.AllPagesFetched)
            {
                Playlist.Songs.AddRange(await getSongs(await paginatedResults.GetNextPage()));
            }
        }

        private async Task<List<Song>> getSongs(YouTube.Page page)
        {
            var list = new List<Song>();
            foreach (var item in page.ContentItems)
            {
                if (item.Content is YouTube.Video music)
                {
                    list.Add(new Song()
                    {
                        Url = music.Url,
                        Title = music.Title,
                        Artist = music.Author.Split("•")[0].Trim(),
                        Media = music,
                    });
                }
            }
            return list;
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
                        if (MainWindow?.currentPlaylist?.media.PlaylistId == Playlist.media.PlaylistId)
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
                //TODO: start Playlist playback
            }
        }

        private void SongCard_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWin)
            {
                //TODO: start Playlist playback
                //TODO: enter Playlist view
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
