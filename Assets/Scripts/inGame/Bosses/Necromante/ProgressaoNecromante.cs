using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class ProgressaoNecromante : MonoBehaviour
{
    public enum EstadoProgressao
    {
        HordasCidade,
        PrimeiroEncontro,
        TransicaoFloresta,
        HordasFloresta,
        EncontroFinal,
        Concluido,
        GameOver
    }

    [Header("Referencias da cena")]
    [SerializeField] private HordaManager _hordas;
    [SerializeField] private ControladorTransicaoCenario _transicao;
    [SerializeField] private ControladorEncontroNecromante _encontro;
    [SerializeField] private VidaManager _vidaJogador;

    [Header("Encontros apos concluir a horda")]
    [SerializeField, Min(1)] private int _hordaPrimeiroEncontro = 10;
    [SerializeField, Min(2)] private int _hordaEncontroFinal = 20;
    [SerializeField, Min(1f)] private float _dificuldadeEncontroFinal = 1.15f;

    [Header("Pontuacao dos encontros")]
    [SerializeField, Min(0)] private int _pontosFuga = 5000;
    [SerializeField, Min(0)] private int _pontosDerrotaDefinitiva = 10000;

    [Header("Apresentacao da entrada")]
    [SerializeField, Min(0f)] private float _duracaoFadeHud = 0.3f;
    [SerializeField, Min(0.01f)] private float _duracaoTeleporteEntrada = 2.4f;
    [SerializeField, Min(0f)] private float _duracaoFadeHudBoss = 0.6f;

    [Header("Hordas apos a derrota definitiva")]
    [SerializeField, Min(1)] private int _hordasPorCenario = 10;
    [SerializeField, Min(0f)] private float _respiroAposDerrotaDefinitiva = 8f;
    private int _proximaHordaTrocaCenario;

    [Header("Atalhos de teste")]
    [SerializeField, Tooltip("Somente no Editor e Development Build. Shift+6/7: Cidade/Floresta; Shift+8/9: encontros; Shift+F1: fase 3; Shift+F2: fuga e transicao; Shift+F3: morte.")]
    private bool _ativarCheats = true;

    [SerializeField] private EstadoProgressao _estado;
    private ControladorBoss _boss;
    private bool _preparado;
    private bool _encontroTestePendente;
    private TipoEncontroNecromante _tipoEncontroTeste;
    private Coroutine _apresentacaoEntrada;
    private Coroutine _fadeSaidaHudBoss;
    private Coroutine _apresentacaoSaida;
    private CanvasGroup _hudClassica;
    private float _alphaHudClassica;
    private CanvasGroup _hudBoss;
    private float _alphaHudBoss;
    private IntegradorHUDBoss _integradorHud;
    private ControleAnimatorNecromante _controleVisual;
    private bool _apresentacaoEmAndamento;
    private Coroutine _testeEncerramento;

    public bool ApresentandoEntrada => _apresentacaoEmAndamento;

    public EstadoProgressao EstadoAtual => _estado;
    public bool EncontroEmAndamento => _estado == EstadoProgressao.PrimeiroEncontro
        || _estado == EstadoProgressao.EncontroFinal;

    private void Start()
    {
        // Recupera ligacoes ausentes em cenas de integracao antigas, preservando as atribuidas.
        if (_hordas == null) _hordas = HordaManager.instance;
        if (_vidaJogador == null) _vidaJogador = VidaManager.instance;
        if (_transicao == null)
            _transicao = FindFirstObjectByType<ControladorTransicaoCenario>();
        if (_encontro == null)
            _encontro = FindFirstObjectByType<ControladorEncontroNecromante>(FindObjectsInactive.Include);

        if (_hordas == null || _transicao == null || _encontro == null || _vidaJogador == null)
        {
            Debug.LogError($"Necromante: referencias ausentes na progressao. Hordas: {_hordas != null}; "
                + $"transicao: {_transicao != null}; encontro: {_encontro != null}; vida do jogador: {_vidaJogador != null}.", this);
            enabled = false;
            return;
        }
        _boss = _encontro.GetComponent<ControladorBoss>();
        if (_boss == null)
        {
            Debug.LogError("Necromante: o encontro precisa ter um ControladorBoss.", this);
            enabled = false;
            return;
        }
        _hordas.DefinirTrocaCenarioPorHorda(false);
        _boss.EncerrarExecucao();
        _encontro.gameObject.SetActive(false);
        _transicao.SolicitarCenario(0);
        _estado = EstadoProgressao.HordasCidade;
        _hordas.HordaConcluida += AoConcluirHorda;
        _encontro.EncerramentoIniciado += AoIniciarEncerramento;
        _encontro.EncontroEncerrado += AoConcluirEncontro;
        _vidaJogador.OnGameOver += AoGameOver;
        _preparado = true;
    }

    private void Update()
    {
        if (!_preparado || _estado == EstadoProgressao.GameOver) return;
        if (_vidaJogador.vidasAtuais <= 0 || BloqueioGameplay.EstaAtivo(MotivoBloqueioGameplay.GameOver))
        {
            AoGameOver();
            return;
        }
        ProcessarCheats();
        if (_encontroTestePendente)
        {
            int cenario = _tipoEncontroTeste == TipoEncontroNecromante.PrimeiroEncontro ? 0 : 1;
            if (!PodeIniciar() || _transicao.CenarioAtual != cenario) return;
            _encontroTestePendente = false;
            IniciarEncontro(_tipoEncontroTeste);
            return;
        }
    }

    private void ProcessarCheats()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Keyboard teclado = Keyboard.current;
        if (!_ativarCheats || teclado == null || BloqueioGameplay.BloqueadoSemTransicao
            || (!teclado.leftShiftKey.isPressed && !teclado.rightShiftKey.isPressed)) return;

        if (teclado.f1Key.wasPressedThisFrame)
            SolicitarTesteFase3();
        else if (teclado.f2Key.wasPressedThisFrame)
            SolicitarTesteFuga();
        else if (teclado.f3Key.wasPressedThisFrame)
            SolicitarTesteMorte();
        else if (teclado.digit8Key.wasPressedThisFrame)
            SolicitarEncontroTeste(TipoEncontroNecromante.PrimeiroEncontro);
        else if (teclado.digit9Key.wasPressedThisFrame)
            SolicitarEncontroTeste(TipoEncontroNecromante.EncontroFinal);
        else if (!_encontroTestePendente && teclado.digit6Key.wasPressedThisFrame)
            _transicao.SolicitarCenario(0);
        else if (!_encontroTestePendente && teclado.digit7Key.wasPressedThisFrame)
            _transicao.SolicitarCenario(1);
#endif
    }

    public bool SolicitarEncontroTeste(TipoEncontroNecromante tipo)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!_preparado || !_ativarCheats || _estado == EstadoProgressao.GameOver
            || _vidaJogador.vidasAtuais <= 0 || BloqueioGameplay.BloqueadoSemTransicao) return false;

        CancelarTesteEncerramento();
        CancelarApresentacaoEntrada();
        CancelarApresentacaoSaida();
        _hordas.DefinirSuspensoPorBoss(true, false);
        _boss.EncerrarExecucao();
        _encontro.gameObject.SetActive(false);
        _tipoEncontroTeste = tipo;
        _encontroTestePendente = true;
        _estado = tipo == TipoEncontroNecromante.PrimeiroEncontro
            ? EstadoProgressao.HordasCidade : EstadoProgressao.HordasFloresta;
        _transicao.SolicitarCenario(tipo == TipoEncontroNecromante.PrimeiroEncontro ? 0 : 1);
        Debug.Log($"[CHEAT] Preparando {tipo}; o boss inicia apos a transicao de cenario.", this);
        return true;
#else
        return false;
#endif
    }

    public bool SolicitarTesteFase3() => SolicitarTesteEncerramento(TipoEncontroNecromante.EncontroFinal, false);

    public bool SolicitarTesteFuga() => SolicitarTesteEncerramento(TipoEncontroNecromante.PrimeiroEncontro, true);

    public bool SolicitarTesteMorte() => SolicitarTesteEncerramento(TipoEncontroNecromante.EncontroFinal, true);

    private bool SolicitarTesteEncerramento(TipoEncontroNecromante tipo, bool concluirEncontro)
    {
        if (!SolicitarEncontroTeste(tipo)) return false;
        _testeEncerramento = StartCoroutine(TestarEncerramento(concluirEncontro));
        return true;
    }

    private IEnumerator TestarEncerramento(bool concluirEncontro)
    {
        // Aguarda a entrada real: o ControladorBoss inicializa a vida no FixedUpdate.
        while (_encontroTestePendente || _apresentacaoEmAndamento || !_boss.PodeAtualizar)
            yield return null;

        VidaBoss vida = _boss.GetComponent<VidaBoss>();
        ControladorFasesNecromante fases = _boss.GetComponent<ControladorFasesNecromante>();
        if (vida == null || fases == null)
        {
            Debug.LogWarning("[CHEAT] O encontro precisa de vida e fases configuradas.", this);
            _testeEncerramento = null;
            yield break;
        }

        // Usa o mesmo dano e os mesmos eventos da partida para preservar HUD, animacoes e pontuacao.
        vida.DefinirVulneravel(true);
        vida.TentarReceberDano(vida.VidaAtual);

        if (concluirEncontro && _encontro.TipoEncontro == TipoEncontroNecromante.EncontroFinal)
        {
            // A fase 3 recupera uma barra propria e reinicia o controlador antes da morte.
            yield return new WaitForFixedUpdate();
            while (!_boss.PodeAtualizar) yield return null;
            if (fases.FaseAtual == FaseBoss.Fase3)
            {
                vida.DefinirVulneravel(true);
                vida.TentarReceberDano(vida.VidaAtual);
            }
        }
        _testeEncerramento = null;
    }

    private void CancelarTesteEncerramento()
    {
        if (_testeEncerramento != null) StopCoroutine(_testeEncerramento);
        _testeEncerramento = null;
    }

    private void AoConcluirHorda(int numeroHorda)
    {
        if (_estado == EstadoProgressao.HordasCidade && numeroHorda >= _hordaPrimeiroEncontro)
            TentarIniciarPrimeiroEncontro();
        else if (_estado == EstadoProgressao.HordasFloresta && numeroHorda >= _hordaEncontroFinal)
            TentarIniciarEncontroFinal();
        else if (_estado == EstadoProgressao.Concluido && numeroHorda >= _proximaHordaTrocaCenario)
        {
            _transicao.SolicitarCenario(1 - _transicao.CenarioSolicitado);
            _proximaHordaTrocaCenario = numeroHorda + _hordasPorCenario;
        }
    }

    public bool TentarIniciarPrimeiroEncontro()
    {
        if (!PodeIniciar() || _estado != EstadoProgressao.HordasCidade
            || _transicao.CenarioAtual != 0) return false;
        IniciarEncontro(TipoEncontroNecromante.PrimeiroEncontro);
        return true;
    }

    public bool TentarIniciarEncontroFinal()
    {
        if (!PodeIniciar() || _estado != EstadoProgressao.HordasFloresta
            || _transicao.CenarioAtual != 1) return false;
        IniciarEncontro(TipoEncontroNecromante.EncontroFinal);
        return true;
    }

    private bool PodeIniciar()
    {
        return _preparado && _vidaJogador.vidasAtuais > 0 && !BloqueioGameplay.Bloqueado
            && !_transicao.EmTransicao && _transicao.CenarioAtual == _transicao.CenarioSolicitado;
    }

    private void IniciarEncontro(TipoEncontroNecromante tipo)
    {
        _estado = tipo == TipoEncontroNecromante.PrimeiroEncontro
            ? EstadoProgressao.PrimeiroEncontro : EstadoProgressao.EncontroFinal;
        _hordas.DefinirSuspensoPorBoss(true, false);
        _apresentacaoEntrada = StartCoroutine(ApresentarEncontro(tipo));
    }

    private IEnumerator ApresentarEncontro(TipoEncontroNecromante tipo)
    {
        _apresentacaoEmAndamento = true;
        _integradorHud = _encontro.GetComponent<IntegradorHUDBoss>();
        _controleVisual = _encontro.GetComponent<ControleAnimatorNecromante>();
        HUDManager hud = FindFirstObjectByType<HUDManager>();
        _hudClassica = hud != null && hud.GrupoHudTransicao != null
            ? hud.GrupoHudTransicao.GetComponent<CanvasGroup>() : null;
        if (_hudClassica == null && hud != null && hud.GrupoHudTransicao != null)
            _hudClassica = hud.GrupoHudTransicao.AddComponent<CanvasGroup>();
        _alphaHudClassica = _hudClassica != null ? _hudClassica.alpha : 1f;
        _hudBoss = null;

        try
        {
            yield return FadeEntrada(_duracaoFadeHud, false);
            _hordas.OcultarHudHorda();
            _integradorHud?.DefinirHudVisivel(false);
            _encontro.gameObject.SetActive(true);
            // Cancela o inicio automatico do prefab antes de apresentar a chegada.
            _encontro.PrepararEncontro(tipo, tipo == TipoEncontroNecromante.EncontroFinal,
                tipo == TipoEncontroNecromante.EncontroFinal ? _dificuldadeEncontroFinal : 1f);
            ScoreManager.instance?.RegistrarInicioNecromante(tipo);
            _controleVisual?.ApresentarEntrada(0f);

            float tempo = 0f;
            while (tempo < _duracaoTeleporteEntrada)
            {
                if (PodeAvancarApresentacao())
                {
                    tempo += Time.unscaledDeltaTime;
                    _controleVisual?.ApresentarEntrada(tempo / _duracaoTeleporteEntrada);
                }
                yield return null;
            }

            _controleVisual?.ConcluirEntrada();
            _integradorHud?.DefinirHudVisivel(true);
            if (_integradorHud != null && _integradorHud.HudAtual != null)
            {
                _integradorHud.HudAtual.DefinirTextoVidaVisivel(false);
                GameObject painel = _integradorHud.HudAtual.GrupoHudTransicao;
                _hudBoss = painel.GetComponent<CanvasGroup>();
                if (_hudBoss == null) _hudBoss = painel.AddComponent<CanvasGroup>();
                _alphaHudBoss = _hudBoss.alpha;
                _hudBoss.alpha = 0f;
            }
            yield return FadeEntrada(_duracaoFadeHudBoss, true);
            // Durante o combate, mantem vidas, pontuacao e combo; somente a horda fica oculta.
            yield return FadeGrupo(_hudClassica, 0f, _alphaHudClassica, _duracaoFadeHudBoss);
        }
        finally
        {
            _apresentacaoEmAndamento = false;
            _apresentacaoEntrada = null;
        }

        if (_estado != EstadoProgressao.GameOver && _preparado)
            _boss.IniciarEncontro();
    }

    private IEnumerator FadeEntrada(float duracao, bool exibirBoss)
    {
        float tempo = 0f;
        while (tempo < duracao)
        {
            if (PodeAvancarApresentacao())
            {
                tempo += Time.unscaledDeltaTime;
                AplicarFadeEntrada(Mathf.Clamp01(tempo / duracao), exibirBoss);
            }
            yield return null;
        }
        AplicarFadeEntrada(1f, exibirBoss);
    }

    private void AplicarFadeEntrada(float progresso, bool exibirBoss)
    {
        if (_hudClassica != null)
            _hudClassica.alpha = _alphaHudClassica * (exibirBoss ? 0f : 1f - progresso);
        if (exibirBoss && _hudBoss != null)
            _hudBoss.alpha = _alphaHudBoss * progresso;
    }

    private bool PodeAvancarApresentacao()
    {
        return Time.timeScale > 0f
            && !BloqueioGameplay.EstaAtivo(MotivoBloqueioGameplay.Pause)
            && !BloqueioGameplay.EstaAtivo(MotivoBloqueioGameplay.Tutorial)
            && !BloqueioGameplay.EstaAtivo(MotivoBloqueioGameplay.GameOver)
            && !BloqueioGameplay.EstaAtivo(MotivoBloqueioGameplay.TransicaoCenario);
    }

    private void RestaurarApresentacaoEntrada()
    {
        _apresentacaoEmAndamento = false;
        if (_hudClassica != null) _hudClassica.alpha = _alphaHudClassica;
        if (_hudBoss != null) _hudBoss.alpha = _alphaHudBoss;
        if (_controleVisual != null) _controleVisual.ConcluirEntrada();
        if (_integradorHud != null) _integradorHud.DefinirHudVisivel(true);
        _hudClassica = null;
        _hudBoss = null;
    }

    private void CancelarApresentacaoEntrada()
    {
        if (_apresentacaoEntrada != null) StopCoroutine(_apresentacaoEntrada);
        _apresentacaoEntrada = null;
        RestaurarApresentacaoEntrada();
    }

    private void AoIniciarEncerramento(ResultadoEncontroBoss resultado)
    {
        if (!EncontroEmAndamento) return;
        int pontos = resultado == ResultadoEncontroBoss.Fuga ? _pontosFuga : _pontosDerrotaDefinitiva;
        // Registra a vitoria quando a vida do boss acaba, antes da animacao de encerramento.
        ScoreManager.instance?.RegistrarResultadoNecromante(resultado, pontos);
        if (_hudClassica != null) _hudClassica.alpha = 0f;
        _hordas.OcultarHudHorda();
        if (_fadeSaidaHudBoss != null) StopCoroutine(_fadeSaidaHudBoss);
        _fadeSaidaHudBoss = StartCoroutine(OcultarHudBossAoEncerrar());
    }

    private IEnumerator OcultarHudBossAoEncerrar()
    {
        yield return FadeGrupo(_hudBoss, _hudBoss != null ? _hudBoss.alpha : 1f, 0f, _duracaoFadeHud);
        _integradorHud?.DefinirHudVisivel(false);
        _fadeSaidaHudBoss = null;
    }

    private IEnumerator FadeGrupo(CanvasGroup grupo, float inicio, float fim, float duracao)
    {
        if (grupo == null) yield break;
        float tempo = 0f;
        while (tempo < duracao)
        {
            if (PodeAvancarApresentacao())
            {
                tempo += Time.unscaledDeltaTime;
                grupo.alpha = Mathf.Lerp(inicio, fim, Mathf.Clamp01(tempo / duracao));
            }
            yield return null;
        }
        grupo.alpha = fim;
    }

    private void AoConcluirEncontro(ResultadoEncontroBoss resultado)
    {
        if (!EncontroEmAndamento || _vidaJogador.vidasAtuais <= 0) return;
        if (_fadeSaidaHudBoss != null) StopCoroutine(_fadeSaidaHudBoss);
        _fadeSaidaHudBoss = null;
        _encontro.gameObject.SetActive(false);
        _apresentacaoSaida = StartCoroutine(RestaurarHudAposEncontro(resultado));
    }

    private IEnumerator RestaurarHudAposEncontro(ResultadoEncontroBoss resultado)
    {
        // A transicao salva alpha zero; nao pode restaurar a HUD antes de o fade do cenario acabar.
        int destino = resultado == ResultadoEncontroBoss.Fuga ? 1 : 0;
        if (resultado == ResultadoEncontroBoss.Fuga) _estado = EstadoProgressao.TransicaoFloresta;
        _transicao.SolicitarCenario(destino);
        while (_transicao.CenarioAtual != destino || _transicao.CenarioSolicitado != destino
            || _transicao.EmTransicao || !PodeAvancarApresentacao())
            yield return null;

        _hordas.RestaurarHudHorda();
        yield return FadeGrupo(_hudClassica, 0f, _alphaHudClassica, _duracaoFadeHudBoss);
        _apresentacaoSaida = null;
        if (_estado == EstadoProgressao.GameOver) yield break;
        if (_hudBoss != null) _hudBoss.alpha = _alphaHudBoss;
        _hudClassica = null;
        _hudBoss = null;
        _estado = resultado == ResultadoEncontroBoss.Fuga
            ? EstadoProgressao.HordasFloresta : EstadoProgressao.Concluido;
        if (resultado == ResultadoEncontroBoss.DerrotaDefinitiva)
            _proximaHordaTrocaCenario = _hordas.NumeroHordaAtual + _hordasPorCenario;
        _hordas.RetomarAposEncontro(resultado == ResultadoEncontroBoss.DerrotaDefinitiva
            ? _respiroAposDerrotaDefinitiva : _hordas.delayEntreHordas,
            resultado == ResultadoEncontroBoss.DerrotaDefinitiva);
    }

    private void AoGameOver()
    {
        if (_estado == EstadoProgressao.GameOver) return;
        _estado = EstadoProgressao.GameOver;
        _encontroTestePendente = false;
        CancelarTesteEncerramento();
        CancelarApresentacaoEntrada();
        CancelarApresentacaoSaida();
        _hordas.DefinirSuspensoPorBoss(true);
        _boss.EncerrarExecucao();
        _transicao.CancelarTransicao();
        _encontro.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        CancelarTesteEncerramento();
        CancelarApresentacaoEntrada();
        CancelarApresentacaoSaida();
        if (!_preparado) return;
        _hordas.HordaConcluida -= AoConcluirHorda;
        _encontro.EncerramentoIniciado -= AoIniciarEncerramento;
        _encontro.EncontroEncerrado -= AoConcluirEncontro;
        _vidaJogador.OnGameOver -= AoGameOver;
        _preparado = false;
    }

    private void CancelarApresentacaoSaida()
    {
        if (_fadeSaidaHudBoss != null) StopCoroutine(_fadeSaidaHudBoss);
        if (_apresentacaoSaida != null) StopCoroutine(_apresentacaoSaida);
        _fadeSaidaHudBoss = null;
        _apresentacaoSaida = null;
    }

    [ContextMenu("Teste/Iniciar primeiro encontro (Play)")]
    private void TestarPrimeiroEncontro()
    {
        if (Application.isPlaying) SolicitarEncontroTeste(TipoEncontroNecromante.PrimeiroEncontro);
    }

    [ContextMenu("Teste/Iniciar reencontro (Play)")]
    private void TestarEncontroFinal()
    {
        if (Application.isPlaying) SolicitarEncontroTeste(TipoEncontroNecromante.EncontroFinal);
    }

    [ContextMenu("Teste/Iniciar fase 3 (Play)")]
    private void TestarFase3()
    {
        if (Application.isPlaying) SolicitarTesteFase3();
    }

    [ContextMenu("Teste/Fuga e transicao para floresta (Play)")]
    private void TestarFuga()
    {
        if (Application.isPlaying) SolicitarTesteFuga();
    }

    [ContextMenu("Teste/Morte definitiva (Play)")]
    private void TestarMorte()
    {
        if (Application.isPlaying) SolicitarTesteMorte();
    }

    private void OnValidate()
    {
        _hordaPrimeiroEncontro = Mathf.Max(1, _hordaPrimeiroEncontro);
        _hordaEncontroFinal = Mathf.Max(_hordaPrimeiroEncontro + 1, _hordaEncontroFinal);
        _dificuldadeEncontroFinal = Mathf.Max(1f, _dificuldadeEncontroFinal);
        _pontosFuga = Mathf.Max(0, _pontosFuga);
        _pontosDerrotaDefinitiva = Mathf.Max(0, _pontosDerrotaDefinitiva);
        _duracaoFadeHud = Mathf.Max(0f, _duracaoFadeHud);
        _duracaoTeleporteEntrada = Mathf.Max(0.01f, _duracaoTeleporteEntrada);
        _duracaoFadeHudBoss = Mathf.Max(0f, _duracaoFadeHudBoss);
        _hordasPorCenario = Mathf.Max(1, _hordasPorCenario);
        _respiroAposDerrotaDefinitiva = Mathf.Max(0f, _respiroAposDerrotaDefinitiva);
    }
}
