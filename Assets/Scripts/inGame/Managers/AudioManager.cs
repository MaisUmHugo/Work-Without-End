using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Referências")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioSource bgmSource;
    public AudioSource cutsceneSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Músicas")]
    [SerializeField] private AudioClip musicaMenu;
    //[SerializeField] private AudioClip cutsceneAudio;
    [SerializeField] private AudioClip musicaJogo;
    [SerializeField] private AudioClip musicaGameOver;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Cria AudioSources se necessário
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }

        if (cutsceneSource == null)
        {
            cutsceneSource = gameObject.AddComponent<AudioSource>();
            cutsceneSource.playOnAwake = false;
            cutsceneSource.loop = false;
        }
    }

    private void Start()
    {
        ConectarNosGrupos();
        AudioSettings.AplicarVolumesIniciais(mixer);
    }

    private void ConectarNosGrupos()
    {
        bgmSource.outputAudioMixerGroup = mixer.FindMatchingGroups("BGM")[0];
        sfxSource.outputAudioMixerGroup = mixer.FindMatchingGroups("SFX")[0];
        cutsceneSource.outputAudioMixerGroup = mixer.FindMatchingGroups("Cutscene")[0];
    }


    // MÚSICAS 
    public void TocarMusica(AudioClip clip, bool loop = true)
    {
        if (clip == null || (bgmSource.clip == clip && bgmSource.isPlaying))
            return;

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.time = 0f;
        bgmSource.Play();
    }

    public void TocarMusicaMenu() => TocarMusica(musicaMenu);
    public void TocarMusicaJogo() => TocarMusica(musicaJogo);
    public void TocarMusicaGameOver() => TocarMusica(musicaGameOver, false);

    public void PararMusica()
    {
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    // EFEITOS 
    public void TocarSFX(AudioClip clip)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip);
    }

    // VOLUME 
    public void AjustarVolume(string parametro, float valor)
    {
        AudioSettings.AplicarVolume(mixer, parametro, valor);
    }

    public AudioMixer Mixer => mixer;
}
