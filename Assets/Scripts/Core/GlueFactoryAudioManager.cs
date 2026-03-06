using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class GlueFactoryAudioManager : MonoBehaviour
{
    private static GlueFactoryAudioManager instance;

    [SerializeField] private GlueFactoryAudioConfig config;
    [SerializeField] private GlueFactoryGameManager game;

    private readonly Dictionary<string, float> lastPlayTimeById = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
    private readonly List<AudioSource> sfxSources = new List<AudioSource>();
    private AudioSource musicSource;
    private int nextSfxSource;

    public static GlueFactoryAudioManager Instance => instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        EnsureAudioSources();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        UnbindGameEvents();
    }

    public void Configure(GlueFactoryAudioConfig audioConfig, GlueFactoryGameManager gameManager)
    {
        config = audioConfig;
        game = gameManager;

        if (config == null)
        {
            config = ScriptableObject.CreateInstance<GlueFactoryAudioConfig>();
        }

        config.EnsureDefaults();
        EnsureAudioSources();
        BindGameEvents();
        ApplyMusicState();
    }

    public static void PlaySfx(string id)
    {
        if (instance == null)
        {
            return;
        }

        instance.Play(id);
    }

    public void Play(string id)
    {
        if (!CanPlay(id, out var entry))
        {
            return;
        }

        var source = NextSfxSource();
        if (source == null)
        {
            return;
        }

        source.clip = entry.clip;
        source.pitch = UnityEngine.Random.Range(Mathf.Min(entry.pitchRange.x, entry.pitchRange.y), Mathf.Max(entry.pitchRange.x, entry.pitchRange.y));
        source.volume = config.masterVolume * config.sfxVolume * Mathf.Clamp01(entry.volume);
        source.loop = false;
        source.Play();

        if (entry.cooldownSeconds > 0f)
        {
            lastPlayTimeById[id] = Time.unscaledTime;
        }
    }

    private bool CanPlay(string id, out GlueFactoryAudioConfig.SoundEntry entry)
    {
        entry = null;
        if (config == null || !config.enableSounds || string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        if (!config.TryGetSound(id, out entry) || entry == null || entry.clip == null)
        {
            return false;
        }

        if (entry.cooldownSeconds <= 0f)
        {
            return true;
        }

        if (!lastPlayTimeById.TryGetValue(id, out var last))
        {
            return true;
        }

        return Time.unscaledTime - last >= entry.cooldownSeconds;
    }

    private void EnsureAudioSources()
    {
        if (musicSource == null)
        {
            var go = new GameObject("MusicSource", typeof(AudioSource));
            go.transform.SetParent(transform, false);
            musicSource = go.GetComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
        }

        var targetPool = config != null ? Mathf.Clamp(config.sfxSourcePoolSize, 1, 24) : 8;
        while (sfxSources.Count < targetPool)
        {
            var go = new GameObject("SfxSource_" + sfxSources.Count, typeof(AudioSource));
            go.transform.SetParent(transform, false);
            var src = go.GetComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f;
            sfxSources.Add(src);
        }
    }

    private AudioSource NextSfxSource()
    {
        if (sfxSources.Count == 0)
        {
            return null;
        }

        nextSfxSource = (nextSfxSource + 1) % sfxSources.Count;
        return sfxSources[nextSfxSource];
    }

    private void BindGameEvents()
    {
        UnbindGameEvents();
        if (game == null)
        {
            return;
        }

        game.OnManualProduced += HandleManualProduced;
        game.OnMachineProduced += HandleMachineProduced;
        game.OnToast += HandleToast;
        game.OnResetState += HandleResetState;
    }

    private void UnbindGameEvents()
    {
        if (game == null)
        {
            return;
        }

        game.OnManualProduced -= HandleManualProduced;
        game.OnMachineProduced -= HandleMachineProduced;
        game.OnToast -= HandleToast;
        game.OnResetState -= HandleResetState;
    }

    private void HandleManualProduced(double _)
    {
        Play("manual_produce");
    }

    private void HandleMachineProduced(int _, double __)
    {
        Play("auto_produce");
    }

    private void HandleResetState()
    {
        Play("reset");
    }

    private void HandleToast(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var lower = message.ToLowerInvariant();
        if (lower.Contains("upgraded") || lower.Contains("upgrade"))
        {
            Play("upgrade");
            return;
        }

        if (lower.Contains("installed") || lower.Contains("selected"))
        {
            Play("machine_install");
            return;
        }

        if (lower.Contains("saved"))
        {
            Play("save");
            return;
        }

        if (lower.Contains("deleted") || lower.Contains("reset"))
        {
            Play("reset");
            return;
        }

        if (lower.Contains("failed") || lower.Contains("need") || lower.Contains("missing"))
        {
            Play("error");
            return;
        }

        if (lower.Contains("locked") || lower.Contains("maxed"))
        {
            Play("warning");
        }
    }

    private void ApplyMusicState()
    {
        if (musicSource == null || config == null)
        {
            return;
        }

        if (!config.enableSounds || config.backgroundMusic == null || !config.playMusicOnStart)
        {
            musicSource.Stop();
            return;
        }

        musicSource.clip = config.backgroundMusic;
        musicSource.loop = config.loopMusic;
        musicSource.volume = config.masterVolume * config.musicVolume;
        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }
}

