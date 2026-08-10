using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class ComoJogarController : MonoBehaviour
{
    [Header("Objetos do Popup")]
    public GameObject fundoCinza;
    public GameObject painelComoJogar;
    public Toggle toggleNaoMostrar;
    public VideoPlayer videoTutorial;

    [Header("HUD")]
    public GameObject hudCanvas;

    private const string PREF_NAO_MOSTRAR = "ComoJogar_Desativado";

    private void Start()
    {
        // Se o jogador marcou "não mostrar novamente", não abre mais
        if (PlayerPrefs.GetInt(PREF_NAO_MOSTRAR, 0) == 1)
        {
            hudCanvas.SetActive(true);
            Time.timeScale = 1f;
            return;
        }

        // Caso contrário, abre sempre que a fase começar
        AbrirTutorial();
    }

    private void AbrirTutorial()
    {
        Time.timeScale = 0f;

        fundoCinza.SetActive(true);
        painelComoJogar.SetActive(true);

        hudCanvas.SetActive(false);

        if (videoTutorial != null)
        {
            videoTutorial.gameObject.SetActive(true);
            videoTutorial.Play();
        }
    }


    public void BotaoComecar()
    {
        if (toggleNaoMostrar.isOn)
            PlayerPrefs.SetInt(PREF_NAO_MOSTRAR, 1);

        painelComoJogar.SetActive(false);
        fundoCinza.SetActive(false);

        if (videoTutorial != null)
            videoTutorial.Stop();

        hudCanvas.SetActive(true);

        Time.timeScale = 1f;
    }
}
