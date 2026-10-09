using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

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
    [SerializeField] private GameObject _seletorMiraControle;
    [Tooltip("Desligue para a BGS: o tutorial aparece em toda partida e ignora a preferência salva. Ligue para permitir ocultá-lo na versão do itch.")]
    [SerializeField] private bool _permitirOcultarTutorial = true;

    [Header("HUD")]
    public GameObject hudCanvas;

    private const string PREF_NAO_MOSTRAR = "ComoJogar_Desativado";

    private void Awake()
    {
        if (videoTutorial != null)
        {
            videoTutorial.playOnAwake = false;
            videoTutorial.prepareCompleted += ReproduzirVideoPreparado;
        }
        if (_videoSegundoSlide != null)
        {
            _videoSegundoSlide.playOnAwake = false;
            _videoSegundoSlide.prepareCompleted += ReproduzirVideoPreparado;
        }
    }

    private void Start()
    {
        if (toggleNaoMostrar != null)
            toggleNaoMostrar.gameObject.SetActive(_permitirOcultarTutorial);

        if (_permitirOcultarTutorial && PlayerPrefs.GetInt(PREF_NAO_MOSTRAR, 0) == 1)
        {
            painelComoJogar.SetActive(false);
            fundoCinza.SetActive(false);
            hudCanvas.SetActive(true);
            BloqueioGameplay.Definir(MotivoBloqueioGameplay.Tutorial, false);
            return;
        }

        AbrirTutorial();
    }

    private void Update()
    {
        if (_primeiroSlide != null && _primeiroSlide.activeInHierarchy)
            AtualizarSeletorMira();
        RestaurarFocoControle();
    }

    private void RestaurarFocoControle()
    {
        Gamepad controle = Gamepad.current;
        EventSystem sistema = EventSystem.current;
        if (controle == null || sistema == null || !painelComoJogar.activeInHierarchy) return;

        GameObject slide = _primeiroSlide != null && _primeiroSlide.activeInHierarchy
            ? _primeiroSlide : _segundoSlide != null && _segundoSlide.activeInHierarchy
            ? _segundoSlide : painelComoJogar;
        GameObject selecionado = sistema.currentSelectedGameObject;
        Selectable elemento = selecionado != null ? selecionado.GetComponent<Selectable>() : null;
        if (elemento != null && elemento.isActiveAndEnabled && elemento.IsInteractable()
            && elemento.transform.IsChildOf(slide.transform)) return;

        if (controle.leftStick.ReadValue().sqrMagnitude > 0.25f
            || controle.dpad.ReadValue().sqrMagnitude > 0.25f
            || controle.buttonSouth.wasPressedThisFrame || controle.buttonEast.wasPressedThisFrame)
            SelecionarSlide(slide);
    }

    private void SelecionarSlide(GameObject slide)
    {
        FocoUI.SelecionarPrimeiroBotao(slide);
        if (slide != _primeiroSlide || _seletorMiraControle == null
            || !_seletorMiraControle.activeInHierarchy) return;

        Slider seletor = _seletorMiraControle.GetComponent<Slider>();
        Button proximo = _primeiroSlide.GetComponentInChildren<Button>();
        if (seletor == null || proximo == null || !proximo.IsInteractable()) return;

        // O eixo horizontal ajusta a mira; a seta permanece acessivel no outro eixo.
        Navigation navegacaoSeletor = seletor.navigation;
        navegacaoSeletor.mode = Navigation.Mode.Explicit;
        navegacaoSeletor.selectOnUp = proximo;
        navegacaoSeletor.selectOnDown = proximo;
        seletor.navigation = navegacaoSeletor;
        Navigation navegacaoBotao = proximo.navigation;
        navegacaoBotao.selectOnLeft = seletor;
        navegacaoBotao.selectOnUp = seletor;
        proximo.navigation = navegacaoBotao;
    }

    private void AtualizarSeletorMira()
    {
        if (_seletorMiraControle == null) return;
        bool mostrar = Gamepad.current != null;
        if (_seletorMiraControle.activeSelf == mostrar) return;
        _seletorMiraControle.SetActive(mostrar);
        SelecionarSlide(_primeiroSlide);
    }

    private void AbrirTutorial()
    {
        BloqueioGameplay.Definir(MotivoBloqueioGameplay.Tutorial, true);

        fundoCinza.SetActive(true);
        painelComoJogar.SetActive(true);
        hudCanvas.SetActive(false);
        if (_primeiroSlide != null) _primeiroSlide.SetActive(true);
        if (_segundoSlide != null) _segundoSlide.SetActive(false);
        AtualizarSeletorMira();
        SelecionarSlide(_primeiroSlide != null ? _primeiroSlide : painelComoJogar);

        if (videoTutorial != null)
        {
            videoTutorial.gameObject.SetActive(true);
            if (videoTutorial.clip != null) videoTutorial.Prepare();
        }
        if (_videoSegundoSlide != null && _videoSegundoSlide.clip != null)
        {
            _videoSegundoSlide.gameObject.SetActive(true);
            _videoSegundoSlide.Prepare();
        }
    }

    private void ReproduzirVideoPreparado(VideoPlayer video)
    {
        if (!video.isPrepared || !painelComoJogar.activeInHierarchy) return;
        bool primeiroAtivo = _primeiroSlide == null || _primeiroSlide.activeInHierarchy;
        bool segundoAtivo = _segundoSlide != null && _segundoSlide.activeInHierarchy;
        if ((video == videoTutorial && primeiroAtivo) || (video == _videoSegundoSlide && segundoAtivo))
            video.Play();
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
            ReproduzirVideoPreparado(_videoSegundoSlide);
        SelecionarSlide(_segundoSlide);
    }

    public void BotaoComecar()
    {
        if (_primeiroSlide != null && _primeiroSlide.activeSelf) return;
        if (_permitirOcultarTutorial && toggleNaoMostrar != null && toggleNaoMostrar.isOn)
        {
            PlayerPrefs.SetInt(PREF_NAO_MOSTRAR, 1);
            PlayerPrefs.Save();
        }

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
        if (videoTutorial != null)
            videoTutorial.prepareCompleted -= ReproduzirVideoPreparado;
        if (_videoSegundoSlide != null)
            _videoSegundoSlide.prepareCompleted -= ReproduzirVideoPreparado;
        BloqueioGameplay.Definir(MotivoBloqueioGameplay.Tutorial, false);
    }
}
