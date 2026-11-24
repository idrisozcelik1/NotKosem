using NotKosem.Models;
using System.IO;
using System.Text.Json;

namespace NotKosem.Services
{
    public static class CalendarStorageService
    {
        private static readonly string FolderPath =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NotKosem");

        private static readonly string FilePath =
            Path.Combine(FolderPath, "calendar.json");

        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true
        };

        public static List<CalendarEvent> LoadEvents()
        {
            if (!File.Exists(FilePath))
                return new List<CalendarEvent>();

            try
            {
                string json = File.ReadAllText(FilePath);

                return JsonSerializer.Deserialize<List<CalendarEvent>>(json)
                       ?? new List<CalendarEvent>();
            }
            catch
            {
                return new List<CalendarEvent>();
            }
        }

        public static void SaveEvents(List<CalendarEvent> events)
        {
            Directory.CreateDirectory(FolderPath);

            string json = JsonSerializer.Serialize(events, Options);

            File.WriteAllText(FilePath, json);
        }
    }
}