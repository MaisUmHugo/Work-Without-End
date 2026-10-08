using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class PauseController : MonoBehaviour
{
    private INPUTS inputs;

    [Header("Painéis")]
    [SerializeField] private GameObject FundoCinza;
    [SerializeField] private GameObject painelPause;
    [SerializeField] private GameObject painelConfirmacao;
    [SerializeField] private GameObject _painelControles;
    //[SerializeField] private GameObject painelMenuInicial;

    public static bool JogoPausado { get; private set; }
    private System.Action acaoConfirmada; 
    private int _ultimoFrameFecharControles = -1;
    private class EstadoHudPause
    {
        public CanvasGroup Grupo;
        public float Alpha;
        public bool Interagivel;
        public bool BloqueiaRaycasts;
    }
    private readonly List<EstadoHudPause> _hudsOcultas = new List<EstadoHudPause>();
    private SpriteRenderer _miraOculta;
    private bool _miraEstavaVisivel;

    private void Awake()
    {
        JogoPausado = false;
        BloqueioGameplay.Definir(MotivoBloqueioGameplay.Pause, false);
        painelPause.SetActive(false);
        painelConfirmacao.SetActive(false);
        FundoCinza.SetActive(false);
        if (_painelControles != null) _painelControles.SetActive(false);
        inputs = new INPUTS();

        inputs.Gameplay.Pause.performed += ctx =>
        {
            if (_ultimoFrameFecharControles == Time.frameCount) return;
            if (JogoPausado)
            {
                if (_painelControles != null && _painelControles.activeSelf)
                    FecharControles();
                else
                    FecharPause();
            }
            else if (!BloqueioGameplay.EstaAtivo(MotivoBloqueioGameplay.Tutorial)
                && !BloqueioGameplay.EstaAtivo(MotivoBloqueioGameplay.GameOver))
                AbrirPause();
        };

        inputs.UI.Cancel.performed += ctx =>
        {
            if (_painelControles != null && _painelControles.activeSelf)
                FecharControles();
            else if (painelConfirmacao.activeSelf)
                BotaoConfirmarNao();
        };
    }

    private void OnEnable()
    {
        inputs.Enable();
    }

    private void OnDisable()
    {
        inputs.Disable();
        if (JogoPausado) FecharPause();
        else RestaurarHud();
    }

    public void AbrirPause()
    {
        if (JogoPausado) return;
        JogoPausado = true;
        BloqueioGameplay.Definir(MotivoBloqueioGameplay.Pause, true);
        OcultarHud();

        FundoCinza.SetActive(true);
        painelPause.SetActive(true);
        painelConfirmacao.SetActive(false);

        if (_painelControles != null) _painelControles.SetActive(false);

        FocoUI.SelecionarPrimeiroBotao(painelPause);
        Debug.Log("Pause aberto");
    }

    public void FecharPause()
    {
        JogoPausado = false;
        BloqueioGameplay.Definir(MotivoBloqueioGameplay.Pause, false);
        RestaurarHud();

        painelPause.SetActive(false);
        FundoCinza.SetActive(false);
        painelConfirmacao.SetActive(false);
        if (_painelControles != null) _painelControles.SetActive(false);
        //painelMenuInicial.SetActive(true);

        Debug.Log("Pause fechado");
    }

    private void OcultarHud()
    {
        _hudsOcultas.Clear();
        var grupos = new HashSet<GameObject>();
        foreach (MonoBehaviour componente in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!(componente is IHudTransicaoCenario hud)) continue;
            GameObject objeto = hud.GrupoHudTransicao;
            if (objeto == null || !grupos.Add(objeto)) continue;
            OcultarGrupoHud(objeto);
        }
        Mira mira = FindFirstObjectByType<Mira>(FindObjectsInactive.Include);
        if (mira != null && mira.cooldownUI != null && mira.cooldownUI.canvas != null
            && grupos.Add(mira.cooldownUI.canvas.gameObject))
            OcultarGrupoHud(mira.cooldownUI.canvas.gameObject);
        _miraOculta = mira != null ? mira.GetComponent<SpriteRenderer>() : null;
        if (_miraOculta != null)
        {
            _miraEstavaVisivel = _miraOculta.enabled;
            _miraOculta.enabled = false;
        }
    }

    private void OcultarGrupoHud(GameObject objeto)
    {
        CanvasGroup grupo = objeto.GetComponent<CanvasGroup>();
        if (grupo == null) grupo = objeto.AddComponent<CanvasGroup>();
        _hudsOcultas.Add(new EstadoHudPause { Grupo = grupo, Alpha = grupo.alpha,
            Interagivel = grupo.interactable, BloqueiaRaycasts = grupo.blocksRaycasts });
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;
    }

    private void RestaurarHud()
    {
        foreach (EstadoHudPause estado in _hudsOcultas)
        {
            if (estado.Grupo == null) continue;
            estado.Grupo.alpha = estado.Alpha;
            estado.Grupo.interactable = estado.Interagivel;
            estado.Grupo.blocksRaycasts = estado.BloqueiaRaycasts;
        }
        _hudsOcultas.Clear();
        if (_miraOculta != null) _miraOculta.enabled = _miraEstavaVisivel;
        _miraOculta = null;
    }

    // --- Botões ---

    public void AbrirControles()
    {
        if (_painelControles == null) return;
        painelPause.SetActive(false);
        _painelControles.SetActive(true);
        FocoUI.SelecionarPrimeiroBotao(_painelControles);
    }

    public void FecharControles()
    {
        if (_painelControles == null) return;
        _ultimoFrameFecharControles = Time.frameCount;
        _painelControles.SetActive(false);
        painelPause.SetActive(true);
        FocoUI.SelecionarPrimeiroBotao(painelPause);
    }
    public void BotaoContinuar()
    {
        FecharPause();
    }

    public void BotaoMenuPrincipal()
    {
        MostrarConfirmacao(() =>
        {
            JogoPausado = false;
            SceneManager.LoadScene("MenuPrincipal");
        });
    }

    public void BotaoReiniciar()
    {
        MostrarConfirmacao(() =>
        {
            JogoPausado = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        });
    }

    private void MostrarConfirmacao(System.Action acao)
    {
        painelPause.SetActive(false);
        painelConfirmacao.SetActive(true);

        FocoUI.SelecionarPrimeiroBotao(painelConfirmacao);
        acaoConfirmada = acao;
    }

    public void BotaoConfirmarSim()
    {
        acaoConfirmada?.Invoke();
        acaoConfirmada = null;
    }

    public void BotaoConfirmarNao()
    {
        painelConfirmacao.SetActive(false);
        painelPause.SetActive(true);

        FocoUI.SelecionarPrimeiroBotao(painelPause);
        acaoConfirmada = null;
    }

    private void OnDestroy()
    {
        RestaurarHud();
        BloqueioGameplay.Definir(MotivoBloqueioGameplay.Pause, false);

        if (JogoPausado)
        {
            JogoPausado = false;
        }
    }
}
