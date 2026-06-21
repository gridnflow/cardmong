using UnityEngine;

namespace Cardmong.Data
{
    public static class LocalStorage
    {
        public static void Save(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }

        public static string Load(string key)
        {
            return PlayerPrefs.GetString(key, null);
        }

        public static void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        public static bool Has(string key)
        {
            return PlayerPrefs.HasKey(key);
        }
    }
}
