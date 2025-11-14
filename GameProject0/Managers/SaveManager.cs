using System;
using System.IO;
using System.Text.Json;

namespace GameProject0
{
    public static class SaveManager
    {
        // Define save path in AppData
        private static string _savePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GameProject0"
        );
        private static string _saveFile = Path.Combine(_savePath, "save.json");

        // Save GameState object to JSON
        public static void Save(GameState state)
        {
            try
            {
                Directory.CreateDirectory(_savePath);
                string json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_saveFile, json);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error saving game: {e.Message}");
            }
        }

        // Load GameState object from JSON
        public static GameState Load()
        {
            if (!File.Exists(_saveFile))
            {
                Console.WriteLine("No save file found.");
                return null;
            }

            try
            {
                string json = File.ReadAllText(_saveFile);
                return JsonSerializer.Deserialize<GameState>(json);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error loading game: {e.Message}");
                return null;
            }
        }
    }
}