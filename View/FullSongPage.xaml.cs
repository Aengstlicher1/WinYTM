using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WinYTM.Classes;
using Wpf.Ui.Abstractions.Controls;

namespace WinYTM.View
{
    public partial class FullSongPage : Page, INavigationAware
    {
        private Song Song => (Application.Current.MainWindow as MainWindow)?.currentSong!;
        private Song? oldSong { get; set; }
        private CroppedBitmap Bitmap { get; set; } = new CroppedBitmap();
        public FullSongPage()
        {
            InitializeComponent();
            
            _ = UpdateLoop();
        }

        private async Task UpdateLoop(CancellationToken token = new ())
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
                if (oldSong != Song)
                {
                    Bitmap = new Helpers.Cover(Song.Media.Thumbnails.HighResUrl!, false).Bitmap!;
                    SongCover.Source = Bitmap;
                    oldSong = Song;
                }
            }
        }

        Task INavigationAware.OnNavigatedToAsync()
        {
            return Task.CompletedTask;
        }

        Task INavigationAware.OnNavigatedFromAsync()
        {
            return Task.CompletedTask;
        }
    }
}
