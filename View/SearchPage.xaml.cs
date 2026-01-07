using System.Windows;
using System.Windows.Controls;
using WinYTM.Classes;
using YouTubeApi;


namespace WinYTM.View
{
    public partial class SearchPage : Page
    {
        private YouTube.PaginatedResults SearchResults => (Application.Current.MainWindow as MainWindow)!.SearchResults!;
        private CancellationTokenSource _searchCTS => (Application.Current.MainWindow as MainWindow)!._searchCTS!;

        public SearchPage()
        {
            InitializeComponent();

            var mainWin = Application.Current.MainWindow as MainWindow;
            mainWin!.OnSearchCompleted += MainWin_OnSearchCompleted;
            mainWin!.OnSearchCleared += SearchPage_OnSearchCleared;

            _ = AddSearchResults(Results, _searchCTS.Token);
        }

        private void SearchPage_OnSearchCleared(CancellationToken obj)
        {
            Results.Children.Clear();
        }

        private void MainWin_OnSearchCompleted(CancellationToken obj)
        {
            _ = AddSearchResults(Results, _searchCTS.Token);
        }

        private async Task AddSearchResults(StackPanel container, CancellationToken token)
        {
            Dispatcher.Invoke(() => { container.Children.Clear(); });

            if (SearchResults == null) { return; }

            foreach (var result in SearchResults.CurrentPage.ContentItems)
            {
                token.ThrowIfCancellationRequested();

                if (result.Content is YouTube.Video music)
                {
                    var song = new Song() { Title = music.Title, Url = music.Url, Media = music, Artist = music.Author.Split("•")[0].Trim() };

                    token.ThrowIfCancellationRequested();

                    if (!token.IsCancellationRequested)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            container.Children.Add(new SongCard(song));
                        });
                    }
                }
            }
        }
    }
}
