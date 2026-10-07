using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class CheatAtaquesNecromante : MonoBehaviour
{
    [Header("Atalhos de teste")]
    [Tooltip("Disponivel somente no Editor e em Development Build.")]
    [SerializeField] private bool _ativarCheats = true;
    [SerializeField] private bool _exigirShift = true;
    [SerializeField] private Key _teclaProjetil = Key.Digit1;
    [SerializeField] private Key _teclaTiroCarregado = Key.Digit2;
    [SerializeField] private Key _teclaSequenciaRapida = Key.Digit3;
    [FormerlySerializedAs("_teclaZumbiSombrio")]
    [SerializeField] private Key _teclaInvocacao = Key.Digit4;
    [Tooltip("Suspende a luta e os spawns para testar apenas um Sombrio na lane do jogador.")]
    [SerializeField] private Key _teclaSombrioIsolado = Key.Digit5;
    [SerializeField] private Key _teclaCidade = Key.Digit6;
    [SerializeField] private Key _teclaFloresta = Key.Digit7;
    [Header("HUD da cena de teste")]
    [SerializeField] private bool _ocultarProgressoEntregasNoTeste;
    [SerializeField] private bool _ocultarTextoVidaNoTeste;
    [Header("Referencias")]
    [SerializeField] private ComportamentoBossNecromante _comportamento;
    [SerializeField] private AtaqueProjetilNecromante _ataqueProjetil;
    [SerializeField] private AtaqueTiroCarregadoNecromante _tiroCarregado;
    [SerializeField] private AtaqueSequenciaRapidaNecromante _sequenciaRapida;
    [SerializeField] private AtaqueInvocacaoNecromante _ataqueInvocacao;

    private ControladorBoss _controladorBoss;
    private GerenciadorInvocadosNecromante _gerenciadorInvocados;
    private Coroutine _inicioPendente;
    private PressaoAmbienteNecromante _pressaoAmbiente;
    private SpawnerManager _spawnerTeste;
    private bool _testeSombrioIsolado;
    private bool _pressaoEstavaAtiva;
    private bool _hudTesteConfigurada;
    private ControladorTransicaoCenario _transicaoCenario;

    private void Awake()
    {
        _controladorBoss = GetComponent<ControladorBoss>();
        _gerenciadorInvocados = GetComponent<GerenciadorInvocadosNecromante>();
        _pressaoAmbiente = GetComponent<PressaoAmbienteNecromante>();
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Keyboard teclado = Keyboard.current;
        if (!_ativarCheats || teclado == null || BloqueioGameplay.BloqueadoSemTransicao)
            return;

        bool shiftPressionado = teclado.leftShiftKey.isPressed || teclado.rightShiftKey.isPressed;
        if (_exigirShift && !shiftPressionado)
            return;

        if (teclado[_teclaCidade].wasPressedThisFrame)
        {
            SolicitarCenarioTeste(0);
            return;
        }
        if (teclado[_teclaFloresta].wasPressedThisFrame)
        {
            SolicitarCenarioTeste(1);
            return;
        }
        if (BloqueioGameplay.Bloqueado) return;

        if (teclado[_teclaProjetil].wasPressedThisFrame)
            SolicitarAtaqueTeste(_ataqueProjetil, "projetil comum");
        else if (teclado[_teclaTiroCarregado].wasPressedThisFrame)
            SolicitarAtaqueTeste(_tiroCarregado, "tiro carregado");
        else if (teclado[_teclaSequenciaRapida].wasPressedThisFrame)
            SolicitarAtaqueTeste(_sequenciaRapida, "sequencia rapida");
        else if (teclado[_teclaInvocacao].wasPressedThisFrame)
            SolicitarAtaqueTeste(_ataqueInvocacao, "invocacao");
        else if (teclado[_teclaSombrioIsolado].wasPressedThisFrame)
            InvocarSombrioIsolado();
#endif
    }

    private void SolicitarCenarioTeste(int indice)
    {
        // Na cena integrada, o atalho vive na progressao, que permanece ativa sem o boss.
        if (FindFirstObjectByType<ProgressaoNecromante>() != null) return;
        if (_transicaoCenario == null)
            _transicaoCenario = FindFirstObjectByType<ControladorTransicaoCenario>();
        if (_transicaoCenario == null)
        {
            Debug.LogWarning("[CHEAT] Controlador de transicao ausente nesta cena.", this);
            return;
        }
        _transicaoCenario.SolicitarCenario(indice);
    }

    private void LateUpdate()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_hudTesteConfigurada || (!_ocultarProgressoEntregasNoTeste && !_ocultarTextoVidaNoTeste))
            return;

        HordaManager horda = HordaManager.instance;
        IntegradorHUDBoss integrador = GetComponent<IntegradorHUDBoss>();
        if ((_ocultarProgressoEntregasNoTeste && horda == null)
            || (_ocultarTextoVidaNoTeste && (integrador == null || integrador.HudAtual == null)))
            return;

        if (_ocultarProgressoEntregasNoTeste)
        {
            if (horda.barraProgresso != null)
                horda.barraProgresso.gameObject.SetActive(false);
            if (horda.TextoEntrega != null)
                horda.TextoEntrega.gameObject.SetActive(false);
            if (horda.TextoHorda != null)
                horda.TextoHorda.gameObject.SetActive(false);
        }

        if (_ocultarTextoVidaNoTeste)
            integrador.HudAtual.DefinirTextoVidaVisivel(false);
        _hudTesteConfigurada = true;
#endif
    }

    [ContextMenu("Teste/Invocar somente Zumbi Sombrio (Play)")]
    private void InvocarSombrioIsolado()
    {
        if (!Application.isPlaying || BloqueioGameplay.Bloqueado)
        {
            Debug.LogWarning("[CHEAT] Entre em Play e feche o tutorial ou a pausa antes de testar o Sombrio.", this);
            return;
        }
        if (_gerenciadorInvocados == null)
        {
            Debug.LogWarning("[CHEAT] Gerenciador de invocados ausente para testar o Sombrio.", this);
            return;
        }

        if (!_testeSombrioIsolado)
        {
            if (_inicioPendente != null)
                StopCoroutine(_inicioPendente);
            _inicioPendente = null;
            _controladorBoss?.EncerrarExecucao();
            _pressaoEstavaAtiva = _pressaoAmbiente != null && _pressaoAmbiente.enabled;
            if (_pressaoAmbiente != null)
                _pressaoAmbiente.enabled = false;
            _spawnerTeste = SpawnerManager.instance;
            _spawnerTeste?.DefinirSuspensoPorEncontroBoss(true);
            _testeSombrioIsolado = true;
        }

        _gerenciadorInvocados.RemoverTodos();
        foreach (Entregavel entregavel in FindObjectsByType<Entregavel>(FindObjectsSortMode.None))
            Destroy(entregavel.gameObject);
        foreach (ProjetilNecromante projetil in FindObjectsByType<ProjetilNecromante>(FindObjectsSortMode.None))
            Destroy(projetil.gameObject);
        if (_gerenciadorInvocados.TentarInvocarZumbiSombrioNaLaneDoJogador())
        {
            Debug.Log("[CHEAT] Zumbi Sombrio invocado na lane do jogador para testar duas entregas.", this);
            return;
        }

        Debug.LogWarning(
            "[CHEAT] Nao foi possivel invocar o Sombrio. Confira o prefab, o jogador, "
            + "a Main Camera e o LanesController da cena.", this);
    }

    private void SolicitarAtaqueTeste(AtaqueBossBase ataque, string nomeAtaque)
    {
        if (_comportamento == null || ataque == null)
        {
            Debug.LogWarning($"[CHEAT] Referencias ausentes para testar {nomeAtaque}.", this);
            return;
        }

        EncerrarTesteSombrio();
        if (_comportamento.EstadoAtual != EstadoBoss.Inativo)
        {
            TentarIniciar(ataque, nomeAtaque);
            return;
        }

        if (_controladorBoss == null)
        {
            Debug.LogWarning("[CHEAT] ControladorBoss nao encontrado para iniciar o encontro.", this);
            return;
        }

        if (_inicioPendente != null)
            StopCoroutine(_inicioPendente);

        _controladorBoss.IniciarEncontro();
        _inicioPendente = StartCoroutine(AguardarInicioEAtacar(ataque, nomeAtaque));
    }

    private IEnumerator AguardarInicioEAtacar(AtaqueBossBase ataque, string nomeAtaque)
    {
        const int tentativas = 5;
        for (int i = 0; i < tentativas; i++)
        {
            while (BloqueioGameplay.Bloqueado)
                yield return null;
            yield return new WaitForFixedUpdate();
            if (BloqueioGameplay.Bloqueado)
            {
                i--;
                continue;
            }

            if (_comportamento != null && _comportamento.EstadoAtual != EstadoBoss.Inativo)
            {
                _inicioPendente = null;
                TentarIniciar(ataque, nomeAtaque);
                yield break;
            }
        }

        _inicioPendente = null;
        Debug.LogWarning(
            $"[CHEAT] O encontro nao iniciou; nao foi possivel testar {nomeAtaque}. "
            + "Confira os erros anteriores do ControladorBoss no Console.",
            this);
    }

    private void TentarIniciar(AtaqueBossBase ataque, string nomeAtaque)
    {
        if (ataque == _ataqueInvocacao
            && _gerenciadorInvocados != null && _gerenciadorInvocados.QuantidadeAtiva > 0)
        {
            _gerenciadorInvocados.RemoverTodos();
            Debug.Log("[CHEAT] Invocados anteriores removidos para testar uma nova invocacao.", this);
        }

        if (ataque == _ataqueInvocacao && _ataqueInvocacao != null)
            _ataqueInvocacao.GarantirSombrioNaProximaInvocacaoTeste();

        if (_comportamento != null && _comportamento.TentarIniciarAtaqueTeste(ataque))
        {
            Debug.Log($"[CHEAT] Necromante iniciou {nomeAtaque}.", this);
            return;
        }

        Debug.LogWarning($"[CHEAT] Nao foi possivel iniciar {nomeAtaque}.", this);
    }

    private void OnDisable()
    {
        if (_inicioPendente != null)
            StopCoroutine(_inicioPendente);
        _inicioPendente = null;
        EncerrarTesteSombrio();
    }

    private void EncerrarTesteSombrio()
    {
        if (!_testeSombrioIsolado) return;

        _gerenciadorInvocados?.RemoverTodos();
        _spawnerTeste?.DefinirSuspensoPorEncontroBoss(false);
        if (_pressaoAmbiente != null)
            _pressaoAmbiente.enabled = _pressaoEstavaAtiva;
        _testeSombrioIsolado = false;
        _spawnerTeste = null;
    }

}
