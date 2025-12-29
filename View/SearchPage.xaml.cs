using AngleSharp.Html.Dom;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Windows;
using System.Windows.Controls;
using WinYTM.Classes;
using YouTubeApi;
using YoutubeExplode;

namespace WinYTM.View
{
    public partial class SearchPage : Page
    {
        private IAsyncEnumerable<YoutubeExplode.Search.VideoSearchResult>? SearchResults => (Application.Current.MainWindow as MainWindow)?.SearchResults;
        private YoutubeExplode.YoutubeClient _youtube => (Application.Current.MainWindow as MainWindow)?._youtube!;
        private CancellationTokenSource _searchCTS => (Application.Current.MainWindow as MainWindow)?._searchCTS!;
        private Wpf.Ui.Controls.TextBox searchBox => (Application.Current.MainWindow as MainWindow)?.SearchBox!;
        public SearchPage()
        {
            InitializeComponent();
            searchBox.TextChanged += SearchBox_TextChanged;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _ = AddSearchResults(Results, _searchCTS.Token);
        }

        private async Task AddSearchResults(System.Windows.Controls.StackPanel container, CancellationToken token)
        {
            Dispatcher.Invoke(() => { container.Children.Clear(); });

            if (SearchResults != null)
            {
                await foreach (var result in SearchResults)
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
        }
    }
}
