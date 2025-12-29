using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using YouTubeApi;
using WinYTM.View;

namespace WinYTM.Classes
{
    public class Song
    {
        public required string Url { get; set; }
        public required string Title { get; set; }
        public required YouTube.Video Media { get; set; }
    }

    public class SongCard : Wpf.Ui.Controls.Button
    {
        public Song Song { get; set; }
        public SongCard(Song song)
        {
            Song = song;
            var Grid = new Grid()
            {
                VerticalAlignment  = VerticalAlignment.Stretch,
                Background = new SolidColorBrush(Colors.Transparent),

            };
            Grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            Grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star)});
            Grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            this.SetResourceReference(StyleProperty, typeof(Wpf.Ui.Controls.Button));

            this.CornerRadius = new CornerRadius(12);
            this.SetResourceReference(Wpf.Ui.Controls.Button.BackgroundProperty, "ControlFillColorDefaultBrush");
            this.Height = 60;
            this.MinWidth = 200;
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            this.VerticalContentAlignment = VerticalAlignment.Stretch;
            this.Margin = new Thickness(8,4,8,4);
            this.SetResourceReference(Wpf.Ui.Controls.Button.BorderBrushProperty, "ControlStrokeColorDefaultBrush");
            this.Padding = new Thickness(0);
            this.Content = Grid;
            this.Click += SongCard_Click;
            this.MouseDoubleClick += SongCard_MouseDoubleClick;

            var thumbnailBitmap = new BitmapImage();
            thumbnailBitmap.BeginInit();
            thumbnailBitmap.UriSource = new Uri(song.Media.Thumbnails.LowResUrl!);
            thumbnailBitmap.EndInit();

            var thumbnailImage = new Wpf.Ui.Controls.Image()
            {
                Source = thumbnailBitmap,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Width = this.Height - 8,
                Height = this.Height - 8,
                Background = new SolidColorBrush(Colors.Transparent),
                Margin = new Thickness(4),
                CornerRadius = new CornerRadius(6),
            };
            Grid.SetColumn(thumbnailImage, 0);

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

            var artist = song.Media.Author.Split("•");
            var artistText = new TextBlock()
            {
                Text = artist[0],
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

            var duration = song.Media.Duration.ToString()!.Substring(3);
            var durationText = new TextBlock()
            {
                Text = duration,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0),
            };
            durationText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorTertiaryBrush");
            Grid.SetColumn(durationText, 2);

            Grid.Children.Add(thumbnailImage);
            Grid.Children.Add(textStack);
            Grid.Children.Add(durationText);
        }

        private void SongCard_Click(object sender, RoutedEventArgs e)
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
    }
}
