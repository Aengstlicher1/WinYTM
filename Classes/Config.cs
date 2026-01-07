using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace WinYTM.Classes
{
    public class Config
    {
        public class AppConfig
        {
            public Song? LastSong { get; set; } = null;
            public bool isMediaRepeating { get; set; } = true;
            public bool isMediaShuffled { get; set; } = false;
            public bool isMediaMuted { get; set; } = false;
            public double volume { get; set; } = 50;
        }

        public static string Location { get; } = @$"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\WinYTM\config.json";

        public static AppConfig ReadSettings()
        {
            if (!File.Exists(Location))
            {
                throw new FileNotFoundException(Location);
            }
            string jsonString = File.ReadAllText(Location);

            AppConfig Config = JsonSerializer.Deserialize<AppConfig>(jsonString)!;
            return Config;
        }
        public static void WriteSettings(AppConfig config)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };

            string jsonString = JsonSerializer.Serialize(config, options);
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(Location)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(Location)!);
                }
                File.WriteAllText(Location, jsonString);
            }
            catch (Exception ex) { Debug.WriteLine($"Unable to save config: " + ex.Message); }
        }
    }
}
