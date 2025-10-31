using System;
using System.IO;
using System.Text.Json;

namespace GameProject0
{
    public static class SaveManager
    {
        // We'll store the save file in a user-specific folder
        private static string _savePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GameProject0"
        );
        private static string _saveFile = Path.Combine(_savePath, "save.json");

        /// <summary>
        /// Saves the current game state to a JSON file.
        /// </summary>
        public static void Save(GameState state)
        {
            try
            {
                // Ensure the directory exists
                Directory.CreateDirectory(_savePath);

                // Serialize the game state to a JSON string
                string json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });

                // Write the string to the file
                File.WriteAllText(_saveFile, json);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error saving game: {e.Message}");
            }
        }

        /// <summary>
        /// Loads the game state from the JSON file.
        /// </summary>
        /// <returns>The loaded GameState, or null if no save exists.</returns>
        public static GameState Load()
        {
            if (!File.Exists(_saveFile))
            {
                Console.WriteLine("No save file found.");
                return null;
            }

            try
            {
                // Read the JSON string from the file
                string json = File.ReadAllText(_saveFile);

                // Deserialize the JSON back into a GameState object
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