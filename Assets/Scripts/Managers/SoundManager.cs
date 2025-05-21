using System;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    private static SoundManager _instance;
    public static SoundManager Instance { get { return _instance; } }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip[] backgroundMusic;

    [Serializable]
    public class SoundEffect
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.75f;
        [Range(0.5f, 1.5f)] public float pitchVariation = 1f;
    }

    [Header("Card Sounds")]
    [SerializeField] private SoundEffect cardHover;
    [SerializeField] private SoundEffect cardSelect;
    [SerializeField] private SoundEffect cardDeal;
    [SerializeField] private SoundEffect cardAttack;
    [SerializeField] private SoundEffect cardDamage;
    [SerializeField] private SoundEffect cardDestroy;

    [Header("Character Sounds")]
    [SerializeField] private SoundEffect heroAttack;
    [SerializeField] private SoundEffect enemyAttack;
    [SerializeField] private SoundEffect characterDeath;
    [SerializeField] private SoundEffect healthRestore;

    [Header("UI Sounds")]
    [SerializeField] private SoundEffect buttonClick;
    [SerializeField] private SoundEffect levelUp;
    [SerializeField] private SoundEffect victory;
    [SerializeField] private SoundEffect defeat;

    [Header("General Sounds")]
    [SerializeField] private SoundEffect[] soundEffects;

    [Header("Audio Settings")]
    [SerializeField] private float defaultSfxVolume = 0.75f;
    [SerializeField] private float defaultMusicVolume = 0.5f;
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;

    private Dictionary<string, SoundEffect> soundEffectDictionary;
    private int currentMusicIndex = -1;

    private void Awake()
    {
        // Singleton pattern implementation
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize audio sources if not assigned
            if (musicSource == null)
            {
                GameObject musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.volume = defaultMusicVolume;
            }

            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SFXSource");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.volume = defaultSfxVolume;
            }

            // Set up the sound effects dictionary
            soundEffectDictionary = new Dictionary<string, SoundEffect>();
            foreach (SoundEffect sfx in soundEffects)
            {
                if (!string.IsNullOrEmpty(sfx.name) && sfx.clip != null)
                {
                    soundEffectDictionary[sfx.name] = sfx;
                }
            }

            // Load saved volume settings
            LoadSavedVolumeSettings();
        }
    }

    private void Start()
    {
        // Play background music if available
        if (backgroundMusic != null && backgroundMusic.Length > 0)
        {
            PlayRandomMusic();
        }
    }

    private void LoadSavedVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        // Apply loaded volumes
        UpdateVolumes();
    }

    private void UpdateVolumes()
    {
        musicSource.volume = defaultMusicVolume * musicVolume * masterVolume;
        sfxSource.volume = defaultSfxVolume * sfxVolume * masterVolume;
    }

    #region Music Control

    public void PlayMusic(int index)
    {
        if (backgroundMusic == null || index < 0 || index >= backgroundMusic.Length)
            return;

        currentMusicIndex = index;
        musicSource.clip = backgroundMusic[index];
        musicSource.Play();
    }

    public void PlayRandomMusic()
    {
        if (backgroundMusic == null || backgroundMusic.Length == 0)
            return;

        int newIndex;
        do
        {
            newIndex = UnityEngine.Random.Range(0, backgroundMusic.Length);
        } while (newIndex == currentMusicIndex && backgroundMusic.Length > 1);

        PlayMusic(newIndex);
    }

    public void PauseMusic()
    {
        if (musicSource.isPlaying)
            musicSource.Pause();
    }

    public void ResumeMusic()
    {
        if (!musicSource.isPlaying && musicSource.clip != null)
            musicSource.UnPause();
    }

    public void StopMusic()
    {
        musicSource.Stop();
        currentMusicIndex = -1;
    }

    public void FadeOutMusic(float duration = 1.0f)
    {
        StartCoroutine(FadeMusicCoroutine(0f, duration));
    }

    public void FadeInMusic(float targetVolume, float duration = 1.0f)
    {
        StartCoroutine(FadeMusicCoroutine(targetVolume, duration, true));
    }

    private System.Collections.IEnumerator FadeMusicCoroutine(float targetVolume, float duration, bool playIfStopped = false)
    {
        if (playIfStopped && !musicSource.isPlaying && musicSource.clip != null)
        {
            musicSource.volume = 0f;
            musicSource.Play();
        }

        float startVolume = musicSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
            yield return null;
        }

        musicSource.volume = targetVolume;

        if (targetVolume <= 0f)
            musicSource.Stop();
    }

    #endregion

    #region Sound Effects

    // Play a sound effect by name from the dictionary
    public void PlaySFX(string sfxName)
    {
        if (soundEffectDictionary.TryGetValue(sfxName, out SoundEffect sfx))
        {
            PlaySound(sfx);
        }
    }

    // Play card sounds
    public void PlayCardHover()
    {
        if (cardHover != null && cardHover.clip != null)
            PlaySound(cardHover);
    }

    public void PlayCardSelect()
    {
        if (cardSelect != null && cardSelect.clip != null)
            PlaySound(cardSelect);
    }

    public void PlayCardDeal()
    {
        if (cardDeal != null && cardDeal.clip != null)
            PlaySound(cardDeal);
    }

    public void PlayCardAttack()
    {
        if (cardAttack != null && cardAttack.clip != null)
            PlaySound(cardAttack);
    }

    public void PlayCardDamage()
    {
        if (cardDamage != null && cardDamage.clip != null)
            PlaySound(cardDamage);
    }

    public void PlayCardDestroy()
    {
        if (cardDestroy != null && cardDestroy.clip != null)
            PlaySound(cardDestroy);
    }

    // Play character sounds
    public void PlayHeroAttack()
    {
        if (heroAttack != null && heroAttack.clip != null)
            PlaySound(heroAttack);
    }

    public void PlayEnemyAttack()
    {
        if (enemyAttack != null && enemyAttack.clip != null)
            PlaySound(enemyAttack);
    }

    public void PlayCharacterDeath()
    {
        if (characterDeath != null && characterDeath.clip != null)
            PlaySound(characterDeath);
    }

    public void PlayHealthRestore()
    {
        if (healthRestore != null && healthRestore.clip != null)
            PlaySound(healthRestore);
    }

    // Play UI sounds
    public void PlayButtonClick()
    {
        if (buttonClick != null && buttonClick.clip != null)
            PlaySound(buttonClick);
    }

    public void PlayLevelUp()
    {
        if (levelUp != null && levelUp.clip != null)
            PlaySound(levelUp);
    }

    public void PlayVictory()
    {
        if (victory != null && victory.clip != null)
            PlaySound(victory);
    }

    public void PlayDefeat()
    {
        if (defeat != null && defeat.clip != null)
            PlaySound(defeat);
    }

    // Play a sound effect with pitch variation
    private void PlaySound(SoundEffect soundEffect)
    {
        if (soundEffect == null || soundEffect.clip == null || sfxSource == null)
            return;

        // Apply pitch variation if configured
        if (soundEffect.pitchVariation != 1f)
        {
            float randomPitch = UnityEngine.Random.Range(
                1f / soundEffect.pitchVariation,
                soundEffect.pitchVariation
            );
            sfxSource.pitch = randomPitch;
        }
        else
        {
            sfxSource.pitch = 1f;
        }

        // Calculate final volume based on settings
        float finalVolume = soundEffect.volume * sfxVolume * masterVolume;

        // Play the sound at the configured volume
        sfxSource.PlayOneShot(soundEffect.clip, finalVolume);
    }

    #endregion

    #region Volume Control

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateVolumes();

        // Save to player prefs
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateVolumes();

        // Save to player prefs
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateVolumes();

        // Save to player prefs
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.Save();
    }

    #endregion
}