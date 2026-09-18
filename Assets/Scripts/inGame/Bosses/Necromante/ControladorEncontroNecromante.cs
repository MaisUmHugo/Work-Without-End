using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class ControladorEncontroNecromante : MonoBehaviour, IControladorEncontroBoss
{
    [Header("Componentes")]
    [SerializeField] private ControladorBoss _controladorBoss;
    [SerializeField] private ControladorFasesNecromante _controladorFases;
    [SerializeField] private VidaBoss _vida;
    [SerializeField] private GerenciadorInvocadosNecromante _gerenciadorInvocados;
    [SerializeField] private FeedbackBoss _feedback;
    [SerializeField] private Collider2D _colisor;

    [Header("Encontro")]
    [SerializeField] private TipoEncontroNecromante _tipoEncontro = TipoEncontroNecromante.EncontroFinal;
    [SerializeField, Range(0f, 1f)] private float _inicioFase2PrimeiroEncontro = 0.5f;
    [SerializeField, Range(0f, 1f)] private float _inicioFase2EncontroFinal = 0.66f;
    [SerializeField, Range(0f, 1f)] private float _inicioFase3EncontroFinal = 0.33f;

    [Header("Encerramento provisório")]
    [SerializeField] private Vector2 _deslocamentoFuga = new Vector2(12f, 8f);
    [SerializeField, Min(0f)] private float _duracaoFuga = 1.5f;
    [SerializeField, Min(0f)] private float _duracaoDerrota = 1.2f;

    [Header("Eventos para integracao")]
    [SerializeField] private UnityEvent _aoIniciarFuga = new UnityEvent();
    [SerializeField] private UnityEvent _aoDerrotaDefinitiva = new UnityEvent();
    [SerializeField] private UnityEvent _aoConcluirEncontro = new UnityEvent();

    [SerializeField, Tooltip("Resultado exibido para depuracao durante o Play Mode.")]
    private ResultadoEncontroBoss _resultadoAtual;

    private Vector3 _posicaoInicial;
    private Coroutine _sequenciaEncerramento;
    private bool _encerramentoIniciado;
    private bool _encontroConcluido;

    public TipoEncontroNecromante TipoEncontro => _tipoEncontro;
    public ResultadoEncontroBoss ResultadoAtual => _resultadoAtual;
    public bool Encerrado => _encerramentoIniciado;
    public bool Concluido => _encontroConcluido;

    public event Action<ResultadoEncontroBoss> EncerramentoIniciado;
    public event Action<ResultadoEncontroBoss> EncontroEncerrado;

    private void Awake()
    {
        _posicaoInicial = transform.position;

        if (!ReferenciasValidas())
        {
            Debug.LogError("Necromante: configure os componentes do controlador de encontro.", this);
            enabled = false;
            return;
        }

        AplicarTipoEncontro();
    }

    private void OnEnable()
    {
        if (_vida == null) return;

        _vida.VidaAlterada += AoAlterarVida;
        _vida.VidaEsgotada += AoEsgotarVida;
    }

    private void OnDisable()
    {
        if (_vida != null)
        {
            _vida.VidaAlterada -= AoAlterarVida;
            _vida.VidaEsgotada -= AoEsgotarVida;
        }

        if (_sequenciaEncerramento != null)
            StopCoroutine(_sequenciaEncerramento);
        _sequenciaEncerramento = null;
    }

    public void ConfigurarEncontro(TipoEncontroNecromante tipoEncontro)
    {
        _tipoEncontro = tipoEncontro;
        if (!_encerramentoIniciado)
            AplicarTipoEncontro();
    }

    private void AplicarTipoEncontro()
    {
        if (_controladorFases == null) return;

        if (_tipoEncontro == TipoEncontroNecromante.PrimeiroEncontro)
        {
            _controladorFases.DefinirFaseMaxima(FaseBoss.Fase2);
            _controladorFases.DefinirLimites(_inicioFase2PrimeiroEncontro, 0f);
            return;
        }

        _controladorFases.DefinirFaseMaxima(FaseBoss.Fase3);
        _controladorFases.DefinirLimites(_inicioFase2EncontroFinal, _inicioFase3EncontroFinal);
    }

    private void AoAlterarVida(int vidaAtual, int vidaMaxima)
    {
        if (_encerramentoIniciado && vidaMaxima > 0 && vidaAtual >= vidaMaxima)
            ReiniciarEstadoEncontro();
    }

    private void AoEsgotarVida()
    {
        if (_encerramentoIniciado) return;

        _encerramentoIniciado = true;
        _resultadoAtual = _tipoEncontro == TipoEncontroNecromante.PrimeiroEncontro
            ? ResultadoEncontroBoss.Fuga
            : ResultadoEncontroBoss.DerrotaDefinitiva;

        _controladorBoss.EncerrarExecucao();
        _gerenciadorInvocados.RemoverTodos();
        _colisor.enabled = false;
        _feedback.DefinirExibirEsgotado(_resultadoAtual == ResultadoEncontroBoss.DerrotaDefinitiva);

        EncerramentoIniciado?.Invoke(_resultadoAtual);

        if (_resultadoAtual == ResultadoEncontroBoss.Fuga)
        {
            _aoIniciarFuga?.Invoke();
            _sequenciaEncerramento = StartCoroutine(ExecutarFuga());
        }
        else
        {
            _aoDerrotaDefinitiva?.Invoke();
            _sequenciaEncerramento = StartCoroutine(AguardarDerrota());
        }
    }

    private IEnumerator ExecutarFuga()
    {
        Vector3 origem = transform.position;
        Vector3 destino = origem + (Vector3)_deslocamentoFuga;
        float duracao = Mathf.Max(0f, _duracaoFuga);

        if (duracao <= 0f)
        {
            transform.position = destino;
            ConcluirEncontro();
            yield break;
        }

        float tempo = 0f;
        while (tempo < duracao)
        {
            if (!BloqueioGameplay.Bloqueado && Time.timeScale > 0f)
            {
                tempo += Time.deltaTime;
                float progresso = Mathf.Clamp01(tempo / duracao);
                transform.position = Vector3.Lerp(origem, destino, progresso);
            }

            yield return null;
        }

        transform.position = destino;
        ConcluirEncontro();
    }

    private IEnumerator AguardarDerrota()
    {
        float duracao = Mathf.Max(0f, _duracaoDerrota);
        float tempo = 0f;

        while (tempo < duracao)
        {
            if (!BloqueioGameplay.Bloqueado && Time.timeScale > 0f)
                tempo += Time.deltaTime;

            yield return null;
        }

        ConcluirEncontro();
    }

    private void ConcluirEncontro()
    {
        _sequenciaEncerramento = null;
        _encontroConcluido = true;
        EncontroEncerrado?.Invoke(_resultadoAtual);
        _aoConcluirEncontro?.Invoke();
    }

    private void ReiniciarEstadoEncontro()
    {
        if (_sequenciaEncerramento != null)
            StopCoroutine(_sequenciaEncerramento);

        _sequenciaEncerramento = null;
        _encerramentoIniciado = false;
        _encontroConcluido = false;
        _resultadoAtual = ResultadoEncontroBoss.Nenhum;
        transform.position = _posicaoInicial;
        _colisor.enabled = true;
        _feedback.DefinirExibirEsgotado(true);
        AplicarTipoEncontro();
    }

    private bool ReferenciasValidas()
    {
        return _controladorBoss != null && _controladorBoss.gameObject == gameObject
            && _controladorFases != null && _controladorFases.gameObject == gameObject
            && _vida != null && _vida.gameObject == gameObject
            && _gerenciadorInvocados != null && _gerenciadorInvocados.gameObject == gameObject
            && _feedback != null && _feedback.transform.IsChildOf(transform)
            && _colisor != null && _colisor.gameObject == gameObject;
    }

    private void OnValidate()
    {
        _inicioFase2PrimeiroEncontro = Mathf.Clamp01(_inicioFase2PrimeiroEncontro);
        _inicioFase2EncontroFinal = Mathf.Clamp01(_inicioFase2EncontroFinal);
        _inicioFase3EncontroFinal = Mathf.Clamp(
            _inicioFase3EncontroFinal, 0f, _inicioFase2EncontroFinal);
        _duracaoFuga = Mathf.Max(0f, _duracaoFuga);
        _duracaoDerrota = Mathf.Max(0f, _duracaoDerrota);

        if (_controladorBoss == null)
            _controladorBoss = GetComponent<ControladorBoss>();
        if (_controladorFases == null)
            _controladorFases = GetComponent<ControladorFasesNecromante>();
        if (_vida == null)
            _vida = GetComponent<VidaBoss>();
        if (_gerenciadorInvocados == null)
            _gerenciadorInvocados = GetComponent<GerenciadorInvocadosNecromante>();
        if (_feedback == null)
            _feedback = GetComponentInChildren<FeedbackBoss>();
        if (_colisor == null)
            _colisor = GetComponent<Collider2D>();
    }
}
