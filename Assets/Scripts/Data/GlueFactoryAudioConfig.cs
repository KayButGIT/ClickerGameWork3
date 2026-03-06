using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GlueFactory/Audio Config", fileName = "GlueFactoryAudio")]
public sealed class GlueFactoryAudioConfig : ScriptableObject
{
    [Serializable]
    public sealed class SoundEntry
    {
        public string id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        public Vector2 pitchRange = Vector2.one;
        [Min(0f)] public float cooldownSeconds;
    }

    [Header("Global")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.35f;
    public bool enableSounds = true;

    [Header("Music")]
    public AudioClip backgroundMusic;
    public bool playMusicOnStart = true;
    public bool loopMusic = true;

    [Header("SFX")]
    [Range(1, 24)] public int sfxSourcePoolSize = 8;
    public List<SoundEntry> sounds = new List<SoundEntry>();

    public void EnsureDefaults()
    {
        EnsureEntry("ui_click", 0.35f);
        EnsureEntry("tab_switch", 0.35f);
        EnsureEntry("manual_produce", 0.55f);
        EnsureEntry("auto_produce", 0.28f);
        EnsureEntry("upgrade", 0.7f);
        EnsureEntry("machine_install", 0.8f);
        EnsureEntry("save", 0.55f);
        EnsureEntry("reset", 0.8f);
        EnsureEntry("warning", 0.55f);
        EnsureEntry("error", 0.7f);
    }

    public bool TryGetSound(string id, out SoundEntry entry)
    {
        entry = null;
        if (sounds == null || string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        var key = id.Trim();
        for (var i = 0; i < sounds.Count; i++)
        {
            var item = sounds[i];
            if (item == null || string.IsNullOrWhiteSpace(item.id))
            {
                continue;
            }

            if (string.Equals(item.id.Trim(), key, StringComparison.OrdinalIgnoreCase))
            {
                entry = item;
                return true;
            }
        }

        return false;
    }

    private void EnsureEntry(string id, float defaultVolume)
    {
        if (TryGetSound(id, out _))
        {
            return;
        }

        sounds.Add(new SoundEntry
        {
            id = id,
            volume = Mathf.Clamp01(defaultVolume),
            pitchRange = Vector2.one,
            cooldownSeconds = 0f
        });
    }
}

