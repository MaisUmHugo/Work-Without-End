using UnityEngine;
using UnityEngine.SceneManagement;

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

    private void Awake()
    {
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
    }

    public void AbrirPause()
    {
        JogoPausado = true;
        BloqueioGameplay.Definir(MotivoBloqueioGameplay.Pause, true);

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

        painelPause.SetActive(false);
        FundoCinza.SetActive(false);
        painelConfirmacao.SetActive(false);
        if (_painelControles != null) _painelControles.SetActive(false);
        //painelMenuInicial.SetActive(true);

        Debug.Log("Pause fechado");
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
        BloqueioGameplay.Definir(MotivoBloqueioGameplay.Pause, false);

        if (JogoPausado)
        {
            JogoPausado = false;
        }
    }
}
