using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

public class ComoJogarController : MonoBehaviour
{
    [Header("Objetos do Popup")]
    public GameObject fundoCinza;
    public GameObject painelComoJogar;
    public Toggle toggleNaoMostrar;
    public VideoPlayer videoTutorial;
    [Header("Slides do tutorial")]
    [SerializeField] private GameObject _primeiroSlide;
    [SerializeField] private GameObject _segundoSlide;
    [SerializeField] private VideoPlayer _videoSegundoSlide;

    [Header("HUD")]
    public GameObject hudCanvas;

    private const string PREF_NAO_MOSTRAR = "ComoJogar_Desativado";

    private void Start()
    {
        if (PlayerPrefs.GetInt(PREF_NAO_MOSTRAR, 0) == 1)
        {
            painelComoJogar.SetActive(false);
            fundoCinza.SetActive(false);
            hudCanvas.SetActive(true);
            BloqueioGameplay.Definir(MotivoBloqueioGameplay.Tutorial, false);
            return;
        }

        AbrirTutorial();
    }

    private void AbrirTutorial()
    {
        BloqueioGameplay.Definir(MotivoBloqueioGameplay.Tutorial, true);

        fundoCinza.SetActive(true);
        painelComoJogar.SetActive(true);
        hudCanvas.SetActive(false);
        if (_primeiroSlide != null) _primeiroSlide.SetActive(true);
        if (_segundoSlide != null) _segundoSlide.SetActive(false);
        FocoUI.SelecionarPrimeiroBotao(_primeiroSlide != null ? _primeiroSlide : painelComoJogar);

        if (videoTutorial != null)
        {
            videoTutorial.gameObject.SetActive(true);
            videoTutorial.Play();
        }
    }

    public void ProximoSlide()
    {
        if (_segundoSlide == null) return;
        if (videoTutorial != null) videoTutorial.Stop();
        if (_primeiroSlide != null) _primeiroSlide.SetActive(false);
        _segundoSlide.SetActive(true);
        Canvas.ForceUpdateCanvases();
        foreach (TMP_Text texto in _segundoSlide.GetComponentsInChildren<TMP_Text>())
            texto.ForceMeshUpdate();
        if (_videoSegundoSlide != null && _videoSegundoSlide.clip != null)
            _videoSegundoSlide.Play();
        FocoUI.SelecionarPrimeiroBotao(_segundoSlide);
    }

    public void BotaoComecar()
    {
        if (_primeiroSlide != null && _primeiroSlide.activeSelf) return;
        if (toggleNaoMostrar.isOn)
            PlayerPrefs.SetInt(PREF_NAO_MOSTRAR, 1);

        painelComoJogar.SetActive(false);
        fundoCinza.SetActive(false);

        if (videoTutorial != null)
            videoTutorial.Stop();
        if (_videoSegundoSlide != null) _videoSegundoSlide.Stop();

        hudCanvas.SetActive(true);
        BloqueioGameplay.Definir(MotivoBloqueioGameplay.Tutorial, false);
    }

    private void OnDestroy()
    {
        BloqueioGameplay.Definir(MotivoBloqueioGameplay.Tutorial, false);
    }
}
