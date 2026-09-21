using EfudaIkki.Core;
using UnityEngine;

public sealed class PlayerPrefsProgressRepository : IProgressRepository
{
    private readonly string keyPrefix;

    // An optional namespace allows integration tests to use real PlayerPrefs
    // without reading, changing, or clearing the player's actual progress.
    public PlayerPrefsProgressRepository(string keyPrefix = "")
    {
        this.keyPrefix = keyPrefix ?? throw new System.ArgumentNullException(nameof(keyPrefix));
    }

    public string GetString(string key, string defaultValue)
    {
        return PlayerPrefs.GetString(keyPrefix + key, defaultValue);
    }

    public int GetInt(string key, int defaultValue)
    {
        return PlayerPrefs.GetInt(keyPrefix + key, defaultValue);
    }

    public void SetString(string key, string value)
    {
        PlayerPrefs.SetString(keyPrefix + key, value);
    }

    public void SetInt(string key, int value)
    {
        PlayerPrefs.SetInt(keyPrefix + key, value);
    }

    public void Save()
    {
        PlayerPrefs.Save();
    }
}
