using System;
using System.IO;
using UnityEngine;

namespace AIStartupTycoon.Core
{
 
    public static class SaveSystem
    {
        private const string SaveFileName = "savegame.json";

        private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static void Save(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to save: {e.Message}");
            }
        }

        /// <summary>Returns null if no save exists or it failed to parse - caller should treat as "new game".</summary>
        public static SaveData Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return null;
                string json = File.ReadAllText(SavePath);
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to load: {e.Message}");
                return null;
            }
        }

        public static bool HasSave() => File.Exists(SavePath);

        public static void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }
    }
}