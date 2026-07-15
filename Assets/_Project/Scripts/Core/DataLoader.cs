using System.IO;
using UnityEngine;

namespace Voidovia
{
    public static class DataLoader
    {
        public static T LoadJson<T>(string fileName)
        {
            var path = Path.Combine(Application.streamingAssetsPath, "Data", fileName);
            if (!File.Exists(path))
            {
                Debug.LogError($"Missing data file: {path}");
                return default;
            }

            var json = File.ReadAllText(path);
            return JsonUtility.FromJson<T>(json);
        }
    }
}
