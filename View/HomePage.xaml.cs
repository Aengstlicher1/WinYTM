using System.Diagnostics;
using System.Windows.Controls;
using WinYTM.Classes;
using YouTubeApi;

namespace WinYTM.View
{
    public partial class HomePage : Page
    {
        public HomePage()
        {
            InitializeComponent();

            _ = getPageData();
        }

        private async Task getPageData()
        {
            var featuredPlaylists = await YouTubeApi.YouTube.SearchYouTubeMusic("test", YouTube.MusicSearchFilter.Albums);

            foreach (var item in featuredPlaylists.CurrentPage.ContentItems)
            {
                if (item.Content is YouTube.Playlist playlist)
                {
                    var playlistCard = new PlaylistCard(new Playlist() { media = playlist });
                    Dispatcher.Invoke(() =>{ WrapTest.Children.Add(playlistCard); });
                }
            }
        }
    }
}
