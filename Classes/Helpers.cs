using ColorThiefDotNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WinYTM.Classes
{
    public class Helpers
    {
        public static double getPercent(double number1, double number2)
        {
            return (number1 / number2) * 100;
        }

        public class Cover : INotifyPropertyChanged
        {
            private CroppedBitmap? _bitmapResult;
            public CroppedBitmap? Bitmap
            {
                get => _bitmapResult;
                set { _bitmapResult = value; OnPropertyChanged(); }
            }

            private BitmapImage _source;
            private Int32Rect _area;
            private bool _isSmall;

            public event PropertyChangedEventHandler? PropertyChanged;

            public Cover(string url,bool isSmall, Int32Rect area = default)
            {
                _isSmall = isSmall;
                _area = area;
                _source = new BitmapImage();
                _source.BeginInit();
                _source.UriSource = new Uri(url);
                _source.CacheOption = BitmapCacheOption.OnLoad;

                _source.DownloadCompleted += HandleLoad;
                _source.DecodeFailed += (s, e) => { };

                _source.EndInit();


                handleHandler();
            }

            private async void handleHandler()
            {
                while (true)
                {
                    if (!_source.IsDownloading)
                    {
                        HandleLoad(null, EventArgs.Empty);
                        return;
                    }
                    await Task.Delay(100);
                }
            }

            private void HandleLoad(object? sender, EventArgs e)
            {
                if (_source.PixelWidth == 0) return;

                if (Bitmap != null) return;

                if (_area.IsEmpty)
                {
                    if (_isSmall)
                    {
                        _area = new Int32Rect(
                            (int)Math.Floor(_source.PixelWidth / 4.6d),
                            0,
                            (int)Math.Floor(_source.PixelWidth / 1.76d),
                            _source.PixelHeight
                        );
                    }
                    else
                    {
                        _area = new Int32Rect(
                            (int)Math.Floor(_source.PixelWidth / 4.5d),
                            _source.PixelWidth / 10,
                            (int)Math.Floor(_source.PixelWidth / 1.8d),
                            (int)Math.Floor(_source.PixelHeight / 1.36d)
                        );
                    }
                }

                try
                {
                    var cropped = new CroppedBitmap(_source, _area);
                    cropped.Freeze();
                    Application.Current.Dispatcher.Invoke(() => {
                        Bitmap = cropped;
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Crop failed: {ex.Message}");
                }
            }

            protected void OnPropertyChanged([CallerMemberName] string name = null!)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
