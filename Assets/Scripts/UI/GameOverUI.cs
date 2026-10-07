using System.Collections;
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

    [Header("Resumo da partida")]
    [SerializeField] private TextMeshProUGUI _textoResumoPartida;



    private System.Action acaoConfirmada;
    private GameObject painelQueChamouConfirmacao;
    private Coroutine _balancoIniciais;
    private Vector2 _posicaoInputAntesBalanco;

    private void Awake()
    {
        if (inputNome == null) return;
        inputNome.characterLimit = 3;
        inputNome.onValidateInput = ValidarIniciais;
    }

    private char ValidarIniciais(string texto, int indice, char caractere)
    {
        // O TMP ja desconta o trecho selecionado antes de chamar esta validacao.
        if (texto.Length < 3) return caractere;
        if (_balancoIniciais != null)
        {
            StopCoroutine(_balancoIniciais);
            ((RectTransform)inputNome.transform).anchoredPosition = _posicaoInputAntesBalanco;
        }
        _balancoIniciais = StartCoroutine(BalancarIniciais());
        return '\0';
    }

    private IEnumerator BalancarIniciais()
    {
        RectTransform retangulo = (RectTransform)inputNome.transform;
        _posicaoInputAntesBalanco = retangulo.anchoredPosition;
        const float duracao = 0.28f;
        float tempo = 0f;
        while (tempo < duracao && inputNome.gameObject.activeInHierarchy)
        {
            float deslocamento = Mathf.Sin(tempo * 70f) * 5f * (1f - tempo / duracao);
            retangulo.anchoredPosition = _posicaoInputAntesBalanco + Vector2.right * deslocamento;
            tempo += Time.unscaledDeltaTime;
            yield return null;
        }
        retangulo.anchoredPosition = _posicaoInputAntesBalanco;
        _balancoIniciais = null;
    }

    private void OnDisable()
    {
        if (_balancoIniciais == null) return;
        StopCoroutine(_balancoIniciais);
        if (inputNome != null)
            ((RectTransform)inputNome.transform).anchoredPosition = _posicaoInputAntesBalanco;
        _balancoIniciais = null;
    }

    private void Start()
    {
        fundoCinza.SetActive(false);
        painelGameOver.SetActive(false);
        painelRanking.SetActive(false);
        painelConfirmacao.SetActive(false);
        BloqueioGameplay.Definir(MotivoBloqueioGameplay.GameOver, false);

        VidaManager.instance.OnGameOver += MostrarGameOver;
    }

    private void OnDestroy()
    {
        BloqueioGameplay.Definir(MotivoBloqueioGameplay.GameOver, false);

        if (VidaManager.instance != null)
            VidaManager.instance.OnGameOver -= MostrarGameOver;
    }

    private void MostrarGameOver()
    {
        BloqueioGameplay.Definir(MotivoBloqueioGameplay.GameOver, true);

        hudCanvas.SetActive(false);
        fundoCinza.SetActive(true);
        painelGameOver.SetActive(true);

        grupoSalvarNome.SetActive(false);
        grupoPadrao.SetActive(false);

        int score = ScoreManager.instance.pontuacaoAtual;
        textoPontuacaoGameOver.text = "Pontuação Final: " + score;

        if (_textoResumoPartida != null)
            _textoResumoPartida.text = ScoreManager.instance.ObterResumoPartida();

        if (LeaderboardManager.instance.Top10(score))
            grupoSalvarNome.SetActive(true);
        else
            grupoPadrao.SetActive(true);
        FocoUI.SelecionarPrimeiroBotao(painelGameOver);
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
        FocoUI.SelecionarPrimeiroBotao(painelRanking);

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
        grupoSalvarNome.SetActive(false);
        grupoPadrao.SetActive(true);
        FocoUI.SelecionarPrimeiroBotao(grupoPadrao);
    }

    // BOTÕES GERAIS (USADOS EM AMBOS PAINÉIS)

    public void BotaoReiniciar(GameObject painelOrigem)
    {
        MostrarConfirmacao(() =>
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }, painelOrigem);
    }

    public void BotaoMenu(GameObject painelOrigem)
    {
        MostrarConfirmacao(() =>
        {
            SceneManager.LoadScene("MenuPrincipal");
        }, painelOrigem);
    }

    private void MostrarConfirmacao(System.Action acao, GameObject painelOrigem)
    {
        acaoConfirmada = acao;
        painelQueChamouConfirmacao = painelOrigem;

        painelOrigem.SetActive(false);
        painelConfirmacao.SetActive(true);
        FocoUI.SelecionarPrimeiroBotao(painelConfirmacao);
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
        FocoUI.SelecionarPrimeiroBotao(painelQueChamouConfirmacao);
        acaoConfirmada = null;
    }
}
