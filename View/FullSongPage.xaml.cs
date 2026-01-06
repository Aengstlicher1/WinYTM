using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WinYTM.Classes;


namespace WinYTM.View
{
    public partial class FullSongPage : Page
    {
        private Song Song => (Application.Current.MainWindow as MainWindow)?.currentSong!;
        private MainWindow MainWin => (Application.Current.MainWindow as MainWindow)!;
        private CroppedBitmap Bitmap { get; set; } = new CroppedBitmap();
        private static HttpClient client { get; set; } = new();
        public FullSongPage()
        {
            InitializeComponent();

            RootGrid.PreviewMouseWheel += (s, e) => { e.Handled = true; };

            UpdateLoop();

            loadLyrics();
        }

        private async void loadLyrics()
        {
            if (Song != null)
            {
                var lyrics = await GetLyricsAsync(Song.Artist, Song.Title);

            }
        }

        static async Task<LyricResponse?> GetLyricsAsync(string artist, string track)
        {
            try
            {
                string url = $"https://lrclib.net/api/get?artist_name={artist}&track_name={track}";
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string jsonString = await response.Content.ReadAsStringAsync();

                    var data = JsonSerializer.Deserialize<LyricResponse>(jsonString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return data;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lyrics not found: " + ex.Message);
                return null;
            }
            return null;
        }

        public class LyricResponse
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public string? TrackName { get; set; }
            public string? ArtistName { get; set; }
            public string? SyncedLyrics { get; set; }
            public string? PlainLyrics { get; set; }
        }

        private async void UpdateLoop(CancellationToken token = new ())
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                UpdateInfo();
                await Task.Delay(50);
            }
        }

        private async void UpdateInfo()
        {
            if (Song != null)
            {
                var _cover = new Helpers.Cover(Song.Media.Thumbnails.HighResUrl!, false);
                Bitmap = _cover.Bitmap!;
                SongCover.Source = Bitmap;
            }
        }
    }
}
