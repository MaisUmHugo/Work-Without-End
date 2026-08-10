using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    private INPUTS inputs;

    [Header("Painéis")]
    [SerializeField] private GameObject painelMenuInicial;
    [SerializeField] private GameObject painelOpcoes;
    [SerializeField] private GameObject painelConfirmacao;
    [SerializeField] private GameObject painelRanking;
    [SerializeField] private GameObject fundoCinza;

    [Header("Cenas")]
    [SerializeField] private string CenaJogar;
    [SerializeField] private string CenaCutscene;
    [SerializeField] private string CenaFim;
    [SerializeField] private string CenaMenu;
    [SerializeField] private string CenaCreditos;

    private System.Action acaoPersonalizada;

    private enum TipoConfirmacao { Nenhuma, Sair, CarregarCena, Personalizada }
    private TipoConfirmacao tipoConfirm = TipoConfirmacao.Nenhuma;
    private string cenaParaCarregar;

    private void Awake()
    {
        inputs = new INPUTS();
        inputs.UI.Cancel.performed += OnCancelPerformed;
    }

    private void OnEnable()
    {
        inputs.Enable();
        inputs.Gameplay.Disable();
        inputs.UI.Enable();
    }

    private void OnDisable()
    {
        inputs.UI.Cancel.performed -= OnCancelPerformed;
        inputs.Disable();
    }

    private void Start()
    {
        AudioManager.instance.TocarMusicaMenu();

        if (painelConfirmacao != null)
            painelConfirmacao.SetActive(false);
    }

    // Botões(UI)
   
    public void Jogar()
    {
        AudioManager.instance.PararMusica();
        //SceneManager.LoadScene(CenaCutscene);
        SceneManager.LoadScene(CenaJogar);
    }

    public void Cutscene()
    {
        AudioManager.instance.PararMusica();
        AudioManager.instance.cutsceneSource.Stop();
        SceneManager.LoadScene(CenaCutscene);
    }

    public void Creditos()
    {
        SceneManager.LoadScene(CenaCreditos);
    }

    public void VoltarMenu()
    {
        // Se quiser mostrar confirmação antes de sair, use MostrarConfirmacaoCarregarCena(CenaMenu)
        SceneManager.LoadScene(CenaMenu);
    }

    public void AbrirOpcoes()
    {
        painelMenuInicial.SetActive(false);
        fundoCinza.SetActive(true);
        painelOpcoes.SetActive(true);
    }

    public void FecharOpcoes()
    {
        painelOpcoes.SetActive(false);
        fundoCinza.SetActive(false);
        painelMenuInicial.SetActive(true);
    }

    public void AbrirRanking()
    {
        painelMenuInicial.SetActive(false);
        fundoCinza.SetActive(true);
        painelRanking.SetActive(true);
    }

    public void FecharRanking()
    {
        painelRanking.SetActive(false);
        fundoCinza.SetActive(false);
        painelMenuInicial.SetActive(true);
    }

    public void SairJogo()
    {
        MostrarConfirmacaoSair();
    }

    // mostra confirmação para carregar uma cena específica
    public void MostrarConfirmacaoCarregarCena(string nomeCena)
    {
        tipoConfirm = TipoConfirmacao.CarregarCena;
        cenaParaCarregar = nomeCena;
        MostrarPainelConfirmacao();
    }

    // mostra confirmação para sair
    public void MostrarConfirmacaoSair()
    {
        tipoConfirm = TipoConfirmacao.Sair;
        MostrarPainelConfirmacao();
    }

    // mostra confirmação com ação custom 
    public void MostrarConfirmacaoPersonalizada(System.Action acao)
    {
        tipoConfirm = TipoConfirmacao.Personalizada;
        acaoPersonalizada = acao;
        MostrarPainelConfirmacao();
    }

    private void MostrarPainelConfirmacao()
    {
        painelMenuInicial.SetActive(false);
        fundoCinza.SetActive(true);
        painelConfirmacao.SetActive(true);
    }

    private void LimparConfirmacao()
    {
        painelConfirmacao.SetActive(false);
        fundoCinza.SetActive(false);
        painelMenuInicial.SetActive(true);
        acaoPersonalizada = null;
        tipoConfirm = TipoConfirmacao.Nenhuma;
        cenaParaCarregar = null;
    }

    public void BotaoConfirmarSim()
    {
        Debug.Log("Confirmou, confirmou" + tipoConfirm);

        switch (tipoConfirm)
        {
            case TipoConfirmacao.Sair:
                Debug.Log("Jogo indo de arrasta intencionalmente");
                Application.Quit();
                break;

            case TipoConfirmacao.CarregarCena:
                if (!string.IsNullOrEmpty(cenaParaCarregar))
                {
                    SceneManager.LoadScene(cenaParaCarregar);
                }
                break;

            case TipoConfirmacao.Personalizada:
                acaoPersonalizada?.Invoke();
                break;

            case TipoConfirmacao.Nenhuma:
            default:
                Debug.LogWarning("[Menu] Confirmar sem ação definida!");
                break;
        }

        LimparConfirmacao();
    }

    public void BotaoConfirmarNao()
    {
        Debug.Log("Confirmou no.");
        LimparConfirmacao();
    }
    private void OnCancelPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        // fecha opções se estiver nelas, senão fecha o confirm se aberto
        if (painelOpcoes != null && painelOpcoes.activeSelf)
            FecharOpcoes();
        else if (painelConfirmacao != null && painelConfirmacao.activeSelf)
            BotaoConfirmarNao();
    }
}
