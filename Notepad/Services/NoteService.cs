using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Notepad.Models;

namespace Notepad.Services
{
    public class NoteService
    {
        private static readonly string DataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Notepad");
        private static readonly string DataFile = Path.Combine(DataFolder, "notes.json");

        public List<Note> LoadAll()
        {
            try
            {
                if (!File.Exists(DataFile)) return new List<Note>();
                var json = File.ReadAllText(DataFile);
                return JsonSerializer.Deserialize<List<Note>>(json) ?? new List<Note>();
            }
            catch
            {
                return new List<Note>();
            }
        }

        public void SaveAll(IEnumerable<Note> notes)
        {
            Directory.CreateDirectory(DataFolder);
            var json = JsonSerializer.Serialize(notes, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(DataFile, json);
        }
    }
}
