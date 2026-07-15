using System.IO;
using UnityEngine;

namespace Voidovia
{
    public static class DataLoader
    {
        public static bool TryLoadJson<T>(string fileName, out T data, out string error)
        {
            data = default;
            error = null;
            var path = Path.Combine(Application.streamingAssetsPath, "Data", fileName);
            if (!File.Exists(path))
            {
                error =
                    $"Missing {fileName}\nExpected at:\n{path}\n\nCheck Assets/StreamingAssets/Data/ is in the project.";
                Debug.LogError(error);
                return false;
            }

            try
            {
                var json = File.ReadAllText(path);
                data = JsonUtility.FromJson<T>(json);
                if (data == null)
                {
                    error = $"Failed to parse {fileName} (JsonUtility returned null).";
                    Debug.LogError(error);
                    return false;
                }

                return true;
            }
            catch (System.Exception ex)
            {
                error = $"Error reading {fileName}: {ex.Message}";
                Debug.LogError(error);
                return false;
            }
        }

        public static T LoadJson<T>(string fileName)
        {
            TryLoadJson<T>(fileName, out var data, out _);
            return data;
        }
    }
}
