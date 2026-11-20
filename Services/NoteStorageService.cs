
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using NotKosem.Models;
using System;

namespace NotKosem.Services
{
    public class NoteStorageService
    {
        private static readonly string FolderPath =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NotKosem"
            );

        private static readonly string FilePath =
            Path.Combine(FolderPath, "notes.json");

        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true
        };

        // Kayıtlı notları JSON dosyasından yükler.
        public static ObservableCollection<Note> LoadNotes()
        {
            if (!File.Exists(FilePath))
            {
                return new ObservableCollection<Note>();
            }

            string json = File.ReadAllText(FilePath);

            List<Note>? notes =
                JsonSerializer.Deserialize<List<Note>>(json);

            return new ObservableCollection<Note>(
                notes ?? new List<Note>()
            );
        }

        // Notları JSON dosyasına kaydeder.
        public static void SaveNotes(IEnumerable<Note> notes)
        {
            Directory.CreateDirectory(FolderPath);

            string json = JsonSerializer.Serialize(
                notes,
                Options
            );

            File.WriteAllText(FilePath, json);
        }
    }
}
