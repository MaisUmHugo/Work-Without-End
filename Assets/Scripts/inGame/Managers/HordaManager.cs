using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HordaManager : MonoBehaviour
{
    [Header("Refer\u00EAncia ao Spawner")]
    public SpawnerManager spawnerManager;

    [Header("Barra de Progresso")]
    public Slider barraProgresso; 

    private int NumeroHorda, N_Entregas;
    [Header("Controle Hordas")]
    public int E_Necessarias;
    [SerializeField, Min(1)] private int entregasMaximasAntesLateGame = 15;
    [SerializeField, Min(1)] private int hordaRetomarAumentoEntregas = 25;
    [SerializeField, Min(1)] private int intervaloHordasAumentoEntregas = 10;
    [SerializeField, Min(1)] private int aumentoEntregasPorEtapa = 5;
    public TextMeshProUGUI TextoHorda;
    public TextMeshProUGUI TextoEntrega;
    [Header("Progressao de Dificuldade")]
    [SerializeField] private AnimationCurve curvaVelocidade = CriarCurvaVelocidadePadrao();
    [SerializeField] private AnimationCurve curvaIntervaloSpawn = CriarCurvaIntervaloSpawnPadrao();

    [Header("Late Game")]
    [SerializeField, Min(1)] private int hordaInicioLateGame = 50;
    [SerializeField, Min(1f)] private float velocidadeMinimaLateGame = 18f;
    [SerializeField, Min(1f)] private float velocidadeMaximaLateGame = 20f;
    [SerializeField, Min(0.01f)] private float mudancaMaximaVelocidadePorHorda = 0.5f;
    [SerializeField, Min(0.01f)] private float toleranciaAlvoLateGame = 0.05f;

    private bool lateGameInicializado;
    private float velocidadeAtualLateGame;
    private float velocidadeAlvoLateGame;
    private bool modoTesteMaximo;

    //private bool aguardandoInicio = false;

    [Header("Delay de In\u00EDcio das Hordas")]
    [Tooltip("Tempo (em segundos) antes da primeira horda come\u00E7ar ap\u00F3s carregar a cena.")]
    public float delayInicial;
    [Tooltip("Tempo de espera entre o fim de uma horda e o in\u00EDcio da pr\u00F3xima.")]
    public float delayEntreHordas;

    [Header("Parallax")]
    [SerializeField] private AnimationCurve curvaParallax = CriarCurvaParallaxPadrao();
    public bool aumentarParallax = true;
    private float multiplicadorParallax = 1f;


    private bool HordaMudou, Objetivo;
    public static HordaManager instance;
    [System.Serializable]
    public class TagsPorHorda
    {
        public int horda;
        public List<string> tagsPermitidas;
    }

    [Header("Configura\u00E7\u00E3o de Inimigos por Horda")]
    public List<TagsPorHorda> tagsPorHorda = new List<TagsPorHorda>();

    private void Start()
    {
        aumentarParallax = PlayerPrefs.GetInt("parallaxAumentar", 1) == 1;
        NumeroHorda = 1;
        AplicarDificuldadeDaHorda();
        AtualizarParallax();
        spawnerManager.DesativarSpawn();
        StartCoroutine(IniciarHordaComDelay(delayInicial));
    }
    private void Awake()
    {
        instance = this;
        GarantirConfiguracaoDificuldade();
    }
    private void OnValidate()
    {
        GarantirConfiguracaoDificuldade();
    }

    private void Update()
    {
        if (!BloqueioGameplay.Bloqueado)
            verificarhorda();
        TextoHorda.text = "Horda: " + NumeroHorda;
        TextoEntrega.text = $"Entregas:{N_Entregas}/{E_Necessarias}" ;
        AtualizarBarraProgresso();
        if (!BloqueioGameplay.Bloqueado && Keyboard.current != null)
        {
            bool shiftPressionado = Keyboard.current.leftShiftKey.isPressed
                || Keyboard.current.rightShiftKey.isPressed;

            if (shiftPressionado && Keyboard.current.tKey.wasPressedThisFrame)
                AlternarModoTesteMaximo();
            else if (Keyboard.current.nKey.wasPressedThisFrame)
                AvancarHordas(1);
            else if (Keyboard.current.mKey.wasPressedThisFrame)
                AvancarHordas(10);
        }
    }

    private IEnumerator IniciarHordaComDelay(float delay)
    {
        //aguardandoInicio = true;
        spawnerManager.DesativarSpawn();

        yield return new WaitForSeconds(delay);

        AtualizarInimigosPermitidos(); 
        spawnerManager.AtivarSpawn();
        //aguardandoInicio = false;
    }

    private void AtualizarBarraProgresso()
    {
        if (barraProgresso != null)
        {
            // Calcula o valor do progresso como uma porcentagem
            float progresso = (float)N_Entregas / (float)E_Necessarias;
            barraProgresso.value = progresso;  // Atualiza a barra de progresso
        }
    }
    private void Mudarcondicao()
    {
        if (HordaMudou)
        {
            AplicarDificuldadeDaHorda();
            HordaMudou = false;
            AtualizarParallax();
        }
    }
    private void AplicarDificuldadeDaHorda(bool avancarLateGame = true)
    {
        float multiplicadorHorda = CalcularMultiplicadorVelocidade(NumeroHorda, avancarLateGame);
        float multiplicador = modoTesteMaximo
            ? Mathf.Max(1f, velocidadeMaximaLateGame)
            : multiplicadorHorda;
        float intervaloSpawn = modoTesteMaximo
            ? spawnerManager.IntervaloMinimoSpawn
            : AvaliarCurva(curvaIntervaloSpawn, NumeroHorda, 5f);

        spawnerManager.DefinirDificuldade(multiplicador, intervaloSpawn);
    }

    private void AlternarModoTesteMaximo()
    {
        modoTesteMaximo = !modoTesteMaximo;
        AplicarDificuldadeDaHorda(false);
        spawnerManager.DefinirStressTestMaximo(modoTesteMaximo);

        if (modoTesteMaximo)
        {
            Debug.Log($"[DEBUG] Stress Test MAX ativado\nDificuldade global: {velocidadeMaximaLateGame:0.##}x\nSpawn: {spawnerManager.IntervaloMinimoSpawn:0.##}s");
        }
        else
        {
            Debug.Log($"[DEBUG] Stress Test MAX desativado\nRetornando a dificuldade da Horda {NumeroHorda}");
        }
    }

    private float CalcularMultiplicadorVelocidade(int horda, bool avancarLateGame = true)
    {
        if (horda < Mathf.Max(1, hordaInicioLateGame))
            return Mathf.Max(1f, AvaliarCurva(curvaVelocidade, horda, 1f));

        float minimoLateGame = Mathf.Min(velocidadeMinimaLateGame, velocidadeMaximaLateGame);
        float maximoLateGame = Mathf.Max(velocidadeMinimaLateGame, velocidadeMaximaLateGame);

        if (!lateGameInicializado)
        {
            velocidadeAtualLateGame = Mathf.Clamp(AvaliarCurva(curvaVelocidade, horda, minimoLateGame), minimoLateGame, maximoLateGame);
            velocidadeAlvoLateGame = Random.Range(minimoLateGame, maximoLateGame);
            lateGameInicializado = true;
            return velocidadeAtualLateGame;
        }

        if (!avancarLateGame)
            return velocidadeAtualLateGame;

        if (Mathf.Abs(velocidadeAtualLateGame - velocidadeAlvoLateGame) <= toleranciaAlvoLateGame)
        {
            velocidadeAlvoLateGame = Random.Range(minimoLateGame, maximoLateGame);
        }

        velocidadeAtualLateGame = Mathf.MoveTowards(velocidadeAtualLateGame, velocidadeAlvoLateGame, mudancaMaximaVelocidadePorHorda);
        return velocidadeAtualLateGame;
    }

    private static float AvaliarCurva(AnimationCurve curva, int horda, float valorPadrao)
    {
        if (curva == null || curva.length == 0)
            return valorPadrao;

        return curva.Evaluate(Mathf.Max(1, horda));
    }


    private void GarantirConfiguracaoDificuldade()
    {
        if (curvaVelocidade == null || curvaVelocidade.length == 0)
            curvaVelocidade = CriarCurvaVelocidadePadrao();

        if (curvaIntervaloSpawn == null || curvaIntervaloSpawn.length == 0)
            curvaIntervaloSpawn = CriarCurvaIntervaloSpawnPadrao();

        if (curvaParallax == null || curvaParallax.length == 0)
            curvaParallax = CriarCurvaParallaxPadrao();

        velocidadeMinimaLateGame = Mathf.Max(1f, velocidadeMinimaLateGame);
        velocidadeMaximaLateGame = Mathf.Max(velocidadeMinimaLateGame, velocidadeMaximaLateGame);
        mudancaMaximaVelocidadePorHorda = Mathf.Max(0.01f, mudancaMaximaVelocidadePorHorda);
        entregasMaximasAntesLateGame = Mathf.Max(1, entregasMaximasAntesLateGame);
        hordaRetomarAumentoEntregas = Mathf.Max(1, hordaRetomarAumentoEntregas);
        intervaloHordasAumentoEntregas = Mathf.Max(1, intervaloHordasAumentoEntregas);
        aumentoEntregasPorEtapa = Mathf.Max(1, aumentoEntregasPorEtapa);
    }
    private static AnimationCurve CriarCurvaVelocidadePadrao()
    {
        return new AnimationCurve(
            new Keyframe(1f, 1f),
            new Keyframe(5f, 1.8f),
            new Keyframe(10f, 3f),
            new Keyframe(15f, 4.5f),
            new Keyframe(20f, 6.5f),
            new Keyframe(25f, 8.5f),
            new Keyframe(30f, 11f),
            new Keyframe(35f, 13.5f),
            new Keyframe(40f, 16f),
            new Keyframe(45f, 18.5f),
            new Keyframe(50f, 20f));
    }

    private static AnimationCurve CriarCurvaIntervaloSpawnPadrao()
    {
        return new AnimationCurve(
            new Keyframe(1f, 4.5f),
            new Keyframe(5f, 4f),
            new Keyframe(10f, 3.4f),
            new Keyframe(20f, 2.7f),
            new Keyframe(30f, 2.25f),
            new Keyframe(40f, 1.9f),
            new Keyframe(50f, 1.7f),
            new Keyframe(60f, 1.6f));
    }

    private static AnimationCurve CriarCurvaParallaxPadrao()
    {
        return new AnimationCurve(
            new Keyframe(1f, 1f),
            new Keyframe(5f, 1.05f),
            new Keyframe(10f, 1.15f),
            new Keyframe(20f, 1.3f),
            new Keyframe(30f, 1.45f),
            new Keyframe(40f, 1.6f),
            new Keyframe(50f, 1.75f),
            new Keyframe(60f, 1.85f));
    }

    private void AtualizarParallax()
    {
        multiplicadorParallax = aumentarParallax
            ? Mathf.Max(1f, AvaliarCurva(curvaParallax, NumeroHorda, 1f))
            : 1f;

        foreach (Parallax parallax in FindObjectsByType<Parallax>(FindObjectsSortMode.None))
            parallax.AtualizarVelocidadeParallax(multiplicadorParallax);
    }

    private void verificarhorda()
    {
        if (N_Entregas >= E_Necessarias)
        {
            Objetivo = true;
        }
        if (Objetivo)
        {
            AlterarHorda();
        }
    }

    private bool trocandoHorda = false;

    private void AlterarHorda()
    {
        AvancarHordas(1);
    }

    private void AvancarHordas(int quantidade)
    {
        if (trocandoHorda || quantidade <= 0) return;

        trocandoHorda = true;

        for (int passo = 0; passo < quantidade; passo++)
        {
            NumeroHorda++;
            AtualizarEntregasNecessarias();

            if (passo < quantidade - 1)
                CalcularMultiplicadorVelocidade(NumeroHorda);
        }

        HordaMudou = true;
        Objetivo = false;
        N_Entregas = 0;

        Mudarcondicao();
        StartCoroutine(DelayProximaHorda());
        Debug.Log($"[HordaManager] Esperando {delayEntreHordas}s antes da proxima horda ({NumeroHorda})");
    }

    private void AtualizarEntregasNecessarias()
    {
        if (E_Necessarias < entregasMaximasAntesLateGame)
        {
            E_Necessarias++;
            return;
        }

        if (NumeroHorda < hordaRetomarAumentoEntregas)
            return;

        int hordasDesdeRetomada = NumeroHorda - hordaRetomarAumentoEntregas;
        if (hordasDesdeRetomada % intervaloHordasAumentoEntregas == 0)
            E_Necessarias += aumentoEntregasPorEtapa;
    }

    private IEnumerator DelayProximaHorda()
    {
        spawnerManager.DesativarSpawn();
        yield return new WaitForSeconds(delayEntreHordas);
        AtualizarInimigosPermitidos();
        spawnerManager.AtivarSpawn();
        trocandoHorda = false;
    }

    public void AumentarEntrega()
    {
        if (trocandoHorda) return;

        N_Entregas++;
        Debug.Log(N_Entregas);
    }
    private void AtualizarInimigosPermitidos()
    {
        // tenta encontrar uma configuracao para a horda atual
        TagsPorHorda config = tagsPorHorda.Find(t => t.horda == NumeroHorda);

        if (config != null && config.tagsPermitidas != null && config.tagsPermitidas.Count > 0)
        {
            // usa as tags configuradas normalmente
            spawnerManager.DefinirTagsPermitidas(config.tagsPermitidas);
            Debug.Log($"[HordaManager] Tags permitidas na horda {NumeroHorda}: {string.Join(", ", config.tagsPermitidas)}");
        }
        else
        {
            // nenhuma configuracao -> libera todas as tags para o SpawnerManager sortear a cada spawn
            if (spawnerManager.todasAsTagsEntregaveis != null && spawnerManager.todasAsTagsEntregaveis.Count > 0)
            {
                List<string> tagsAleatorias = new List<string>(spawnerManager.todasAsTagsEntregaveis);
                spawnerManager.DefinirTagsPermitidas(tagsAleatorias);

                Debug.Log($"[HordaManager] Nenhuma tag configurada para a horda {NumeroHorda}. Inimigos serao sorteados a cada spawn.");
            }
            else
            {
                Debug.LogWarning("[HordaManager] Nenhuma tag configurada e nenhuma tag poss\u00EDvel definida no SpawnerManager!");
            }
        }
    }

    public void DefinirTagsPermitidas(List<string> tags)
    {
        spawnerManager.tagsPermitidas = tags;
        Debug.Log($"[SpawnerManager] Tags permitidas atualizadas: {string.Join(", ", tags)}");
    }
    /* private void Final()
    {
        SceneManager.LoadScene("CenaFimDemo");
    }*/

    public void DefinirAumentoParallax(bool ativado)
    {
        //Debug.Log("Toggle mandou: " + ativado);

        aumentarParallax = ativado;
        PlayerPrefs.SetInt("parallaxAumentar", ativado ? 1 : 0);

        AtualizarParallax();
    }
}
