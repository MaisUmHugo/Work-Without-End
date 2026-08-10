using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager instance;

    [Header("Configurações da Cutscene")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string cenaMenu = "MenuPrincipal";
    [SerializeField] private bool podePular = true;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer não atribuído na Cutscene!");
            Invoke(nameof(CarregarProximaCena), 5f);
            return;
        }

        // Diz ao video player para usar o AudioSource do AudioManager
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, AudioManager.instance.cutsceneSource);

        // Preparação do vídeo
        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += (vp) =>
        {
            vp.Play();
        };

        videoPlayer.loopPointReached += OnVideoEnd;
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        CarregarProximaCena();
    }

    private void CarregarProximaCena()
    {
        SceneManager.LoadScene(cenaMenu);
    }

    public void PularCutscene()
    {
        CarregarProximaCena();
    }
}
