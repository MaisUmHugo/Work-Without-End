using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.EventSystems.StandaloneInputModule;

public class GameOverController : MonoBehaviour
{
    [Header("Painéis")]
    public GameObject fundoCinza;
    public GameObject painelGameOver;
    public GameObject painelRanking;
    public GameObject painelConfirmacao;
    public GameObject hudCanvas;

    [Header("Grupos do Game Over")]
    public GameObject grupoSalvarNome;
    public GameObject grupoPadrao;

    [Header("Inputs")]
    public TMP_InputField inputNome;
    public TextMeshProUGUI textoPontuacaoGameOver;
    public TextMeshProUGUI textoPontuacaoRanking;



    private System.Action acaoConfirmada;
    private GameObject painelQueChamouConfirmacao;

    private void Start()
    {
        fundoCinza.SetActive(false);
        painelGameOver.SetActive(false);
        painelRanking.SetActive(false);
        painelConfirmacao.SetActive(false);

        VidaManager.instance.OnGameOver += MostrarGameOver;
    }

    private void OnDestroy()
    {
        if (VidaManager.instance != null)
            VidaManager.instance.OnGameOver -= MostrarGameOver;
    }

    private void MostrarGameOver()
    {
        Time.timeScale = 0f;

        hudCanvas.SetActive(false);
        fundoCinza.SetActive(true);
        painelGameOver.SetActive(true);

        grupoSalvarNome.SetActive(false);
        grupoPadrao.SetActive(false);

        int score = ScoreManager.instance.pontuacaoAtual;
        textoPontuacaoGameOver.text = "Pontuação Final: " + score;

        if (LeaderboardManager.instance.Top10(score))
            grupoSalvarNome.SetActive(true);
        else
            grupoPadrao.SetActive(true);
    }
    // NAVEGAÇÃO ENTRE PAINÉIS

    public void BotaoSalvarNome()
    {
        string nome = inputNome.text;
        if (string.IsNullOrWhiteSpace(nome)) nome = "---";

        int score = ScoreManager.instance.pontuacaoAtual;

        // Isso já salva no disco e atualiza a RAM
        LeaderboardManager.instance.AdicionarEntrada(nome, score);

        AbrirRanking();
    }

    public void AbrirRanking()
    {
        painelGameOver.SetActive(false);
        painelRanking.SetActive(true);

        int score = ScoreManager.instance.pontuacaoAtual;
        textoPontuacaoRanking.text = "Pontuação Final: " + score;

        Debug.Log("Chamando AtualizarRanking...");
        var exibir = painelRanking.GetComponentInChildren<ExibirRanking>(true);

        if (exibir == null)
            Debug.LogError("Não encontrou ExibirRanking no painel!");
        else
        {
            Debug.Log("ExibirRanking encontrado! Atualizando...");
            exibir.AtualizarRanking();
        }
    }

    public void VoltarParaGameOver()
    {
        painelRanking.SetActive(false);
        painelGameOver.SetActive(true);
        grupoPadrao.SetActive(true);
    }

    // BOTÕES GERAIS (USADOS EM AMBOS PAINÉIS)

    public void BotaoReiniciar(GameObject painelOrigem)
    {
        MostrarConfirmacao(() =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }, painelOrigem);
    }

    public void BotaoMenu(GameObject painelOrigem)
    {
        MostrarConfirmacao(() =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MenuPrincipal");
        }, painelOrigem);
    }

    private void MostrarConfirmacao(System.Action acao, GameObject painelOrigem)
    {
        acaoConfirmada = acao;
        painelQueChamouConfirmacao = painelOrigem;

        painelOrigem.SetActive(false);
        painelConfirmacao.SetActive(true);
    }

    public void BotaoConfirmarSim()
    {
        acaoConfirmada?.Invoke();
        acaoConfirmada = null;
    }

    public void BotaoConfirmarNao()
    {
        painelConfirmacao.SetActive(false);
        painelQueChamouConfirmacao.SetActive(true);
        acaoConfirmada = null;
    }
}
