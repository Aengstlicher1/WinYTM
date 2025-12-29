using AngleSharp.Html.Dom;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Windows.Controls;
using WinYTM.Classes;
using YoutubeExplode;

namespace WinYTM.View
{
    public partial class SearchPage : Page
    {
        private YoutubeClient _youtube = new();
        
        public SearchPage()
        {
            InitializeComponent();
        }

        CancellationTokenSource _searchCTS = new CancellationTokenSource();

        private async Task Search(string search, StackPanel container, CancellationToken token)
        {
            Dispatcher.Invoke(() =>{ container.Children.Clear(); });

            var searchResults = _youtube.Search.GetVideosAsync(search, token);

            await foreach (var result in searchResults)
            {
                token.ThrowIfCancellationRequested();

                var media = await _youtube.Videos.GetAsync(result.Url);
                var song = new Song() { Url = result.Url, Title = result.Title, Media = media };

                token.ThrowIfCancellationRequested();

                Dispatcher.Invoke(() =>
                {
                    container.Children.Add(new SongCard(song));
                });
            }
        }

        private async void safeSearch(string search, StackPanel container)
        {
            try
            {
                await Task.Run(() => Search(search, container, _searchCTS.Token), _searchCTS.Token );
            }
            catch (OperationCanceledException ex)
            {
                Debug.WriteLine("A Task was cancelled: " + ex.Message);
            }
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchCTS.Cancel();
            _searchCTS.Dispose();

            _searchCTS = new CancellationTokenSource();

            string searchQuery = SearchBox.Text;
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                Results.Children.Clear();
                return;
            }

            await Task.Delay(300);
            safeSearch(searchQuery, Results);
        }
    }
}
