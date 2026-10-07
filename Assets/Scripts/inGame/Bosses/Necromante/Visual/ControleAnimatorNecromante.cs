using UnityEngine;

[DisallowMultipleComponent]
public class ControleAnimatorNecromante : MonoBehaviour
{
    private enum EstadoVisual
    {
        Nenhum,
        Idle,
        Movimentacao,
        AtaqueProjetil,
        TiroCarregado,
        SequenciaRapida,
        InvocacaoInicio,
        InvocacaoCarregando,
        InvocacaoAbaixar,
        InvocacaoSustentar,
        TeleporteFuga,
        Vulneravel,
        Morte
    }

    private static readonly int Idle = Animator.StringToHash("Base Layer.Idle");
    private static readonly int Movimentacao = Animator.StringToHash("Base Layer.Movimentacao");
    private static readonly int AtaqueProjetil = Animator.StringToHash("Base Layer.AtaqueProjetil");
    private static readonly int TiroCarregado = Animator.StringToHash("Base Layer.TiroCarregado");
    private static readonly int SequenciaRapida = Animator.StringToHash("Base Layer.SequenciaRapida");
    private static readonly int InvocacaoInicio = Animator.StringToHash("Base Layer.Invocacao_Inicio");
    private static readonly int InvocacaoCarregando = Animator.StringToHash("Base Layer.Invocacao_Carregando");
    private static readonly int InvocacaoAbaixar = Animator.StringToHash("Base Layer.Invocacao_Abaixar");
    private static readonly int InvocacaoSustentar = Animator.StringToHash("Base Layer.Invocacao_Sustentar");
    private static readonly int TeleporteFuga = Animator.StringToHash("Base Layer.Teleporte_Fuga");
    private static readonly int Vulneravel = Animator.StringToHash("Base Layer.Vulneravel");
    private static readonly int Morte = Animator.StringToHash("Base Layer.Morte");
    private static readonly int InicioCurto = Animator.StringToHash("Invocacao_Inicio");
    private static readonly int AbaixarCurto = Animator.StringToHash("Invocacao_Abaixar");

    [Header("Referencias")]
    [SerializeField] private Animator _animator;
    [SerializeField] private ComportamentoBossNecromante _comportamento;
    [SerializeField] private MovimentoBossBase _movimento;
    [SerializeField] private VidaBoss _vida;
    [SerializeField] private ControladorEncontroNecromante _encontro;
    [SerializeField] private AtaqueInvocacaoNecromante _ataqueInvocacao;
    [SerializeField, Min(0.01f)] private float _velocidadeAnimacaoAbaixarCajado = 0.8f;

    private EstadoVisual _estadoAtual;
    private bool _apresentandoEntrada;

    public TipoAtaqueNecromante TipoAtaqueAtual { get; private set; }
    public float DuracaoAnimacaoMorte => ObterDuracaoClip("Placeholder_Morte");

    private void Awake()
    {
        if (ReferenciasValidas()) return;

        Debug.LogError("Necromante: configure Animator, comportamento, movimento, vida, encontro e invocacao no controle visual.", this);
        enabled = false;
    }

    private void OnEnable()
    {
        _estadoAtual = EstadoVisual.Nenhum;
    }

    private void OnDisable()
    {
        _apresentandoEntrada = false;
        if (_animator != null)
            _animator.speed = 1f;
        _estadoAtual = EstadoVisual.Nenhum;
    }

    private void Update()
    {
        if (_apresentandoEntrada || BloqueioGameplay.Bloqueado) return;
        TipoAtaqueAtual = TipoAtaqueNecromante.Nenhum;

        if (_animator == null || _comportamento == null || _movimento == null
            || _vida == null || _encontro == null || _ataqueInvocacao == null)
            return;

        if (_encontro.ResultadoAtual == ResultadoEncontroBoss.Fuga)
        {
            float duracao = Mathf.Max(0.01f, _encontro.DuracaoFuga);
            float velocidade = ObterDuracaoClip("Placeholder_Teleporte_Fuga") / duracao;
            Tocar(EstadoVisual.TeleporteFuga, Mathf.Max(0.01f, velocidade));
            return;
        }

        if (_vida.Esgotada)
        {
            Tocar(EstadoVisual.Morte);
            return;
        }

        if (_vida.Vulneravel)
        {
            Tocar(EstadoVisual.Vulneravel);
            return;
        }

        AtaqueBossBase ataque = _comportamento.AtaqueAtual;
        if (ataque != null && ataque.EmExecucao && AtaqueEmExecucao())
        {
            TipoAtaqueAtual = IdentificarTipoAtaque(ataque);
            if (TipoAtaqueAtual == TipoAtaqueNecromante.Invocacao)
                AtualizarInvocacao();
            else
                Tocar(ObterEstadoAtaque(TipoAtaqueAtual));
            return;
        }

        Tocar(_movimento.EmMovimento ? EstadoVisual.Movimentacao : EstadoVisual.Idle);
    }

    public void ApresentarEntrada(float progresso)
    {
        if (_animator == null || !_animator.isActiveAndEnabled
            || _animator.runtimeAnimatorController == null
            || !_animator.HasState(0, TeleporteFuga)) return;

        _apresentandoEntrada = true;
        TipoAtaqueAtual = TipoAtaqueNecromante.Nenhum;
        // Percorre o teleporte de fuga ao contrario, sem velocidade negativa no Animator.
        _animator.speed = 0f;
        _animator.Play(TeleporteFuga, 0, Mathf.Lerp(0.999f, 0f, Mathf.Clamp01(progresso)));
        _animator.Update(0f);
    }

    public void ConcluirEntrada()
    {
        _apresentandoEntrada = false;
        _estadoAtual = EstadoVisual.Nenhum;
        if (_animator == null || !_animator.isActiveAndEnabled) return;
        Tocar(EstadoVisual.Idle);
        _animator.Update(0f);
        if (BloqueioGameplay.Bloqueado) _animator.speed = 0f;
    }

    private void AtualizarInvocacao()
    {
        if (_ataqueInvocacao.EmPreparacao)
        {
            if (_estadoAtual != EstadoVisual.InvocacaoInicio
                && _estadoAtual != EstadoVisual.InvocacaoCarregando)
            {
                float duracaoInicio = Mathf.Max(0.01f, _ataqueInvocacao.TempoPreparacaoEfetivo * 0.7f);
                float velocidade = Mathf.Max(0.01f,
                    ObterDuracaoClip("Placeholder_Invocacao_Inicio") / duracaoInicio);
                Tocar(EstadoVisual.InvocacaoInicio, velocidade);
            }
            else if (_estadoAtual == EstadoVisual.InvocacaoInicio && ClipConcluido(InicioCurto))
            {
                Tocar(EstadoVisual.InvocacaoCarregando);
            }
            return;
        }

        if (_estadoAtual != EstadoVisual.InvocacaoAbaixar
            && _estadoAtual != EstadoVisual.InvocacaoSustentar)
            Tocar(EstadoVisual.InvocacaoAbaixar, _velocidadeAnimacaoAbaixarCajado);
        else if (_estadoAtual == EstadoVisual.InvocacaoAbaixar && ClipConcluido(AbaixarCurto))
            Tocar(EstadoVisual.InvocacaoSustentar);
    }

    private bool ClipConcluido(int nomeCurto)
    {
        AnimatorStateInfo estado = _animator.GetCurrentAnimatorStateInfo(0);
        return estado.shortNameHash == nomeCurto && estado.normalizedTime >= 1f;
    }

    private float ObterDuracaoClip(string nomePlaceholder)
    {
        AnimatorOverrideController controladorArte = _animator.runtimeAnimatorController
            as AnimatorOverrideController;
        AnimationClip clip = controladorArte != null ? controladorArte[nomePlaceholder] : null;
        return clip != null ? Mathf.Max(0.01f, clip.length) : 1f;
    }

    private void Tocar(EstadoVisual estado, float velocidade = 1f)
    {
        if (_estadoAtual == estado) return;

        _estadoAtual = estado;
        _animator.speed = velocidade;
        _animator.Play(ObterHash(estado), 0, 0f);
    }

    private static int ObterHash(EstadoVisual estado)
    {
        switch (estado)
        {
            case EstadoVisual.Movimentacao: return Movimentacao;
            case EstadoVisual.AtaqueProjetil: return AtaqueProjetil;
            case EstadoVisual.TiroCarregado: return TiroCarregado;
            case EstadoVisual.SequenciaRapida: return SequenciaRapida;
            case EstadoVisual.InvocacaoInicio: return InvocacaoInicio;
            case EstadoVisual.InvocacaoCarregando: return InvocacaoCarregando;
            case EstadoVisual.InvocacaoAbaixar: return InvocacaoAbaixar;
            case EstadoVisual.InvocacaoSustentar: return InvocacaoSustentar;
            case EstadoVisual.TeleporteFuga: return TeleporteFuga;
            case EstadoVisual.Vulneravel: return Vulneravel;
            case EstadoVisual.Morte: return Morte;
            default: return Idle;
        }
    }

    private bool AtaqueEmExecucao()
    {
        EstadoBoss estado = _comportamento.EstadoAtual;
        return estado == EstadoBoss.PreparandoAtaque || estado == EstadoBoss.Atacando;
    }

    private static EstadoVisual ObterEstadoAtaque(TipoAtaqueNecromante tipo)
    {
        switch (tipo)
        {
            case TipoAtaqueNecromante.Projetil: return EstadoVisual.AtaqueProjetil;
            case TipoAtaqueNecromante.TiroCarregado: return EstadoVisual.TiroCarregado;
            case TipoAtaqueNecromante.SequenciaRapida: return EstadoVisual.SequenciaRapida;
            default: return EstadoVisual.Idle;
        }
    }

    private static TipoAtaqueNecromante IdentificarTipoAtaque(AtaqueBossBase ataque)
    {
        return ataque is IAtaqueVisualNecromante ataqueAnimavel
            ? ataqueAnimavel.TipoAnimacao
            : TipoAtaqueNecromante.Nenhum;
    }

    private bool ReferenciasValidas()
    {
        return _animator != null && _animator.transform.IsChildOf(transform)
            && _comportamento != null && _comportamento.gameObject == gameObject
            && _movimento != null && _movimento.gameObject == gameObject
            && _vida != null && _vida.gameObject == gameObject
            && _encontro != null && _encontro.gameObject == gameObject
            && _ataqueInvocacao != null && _ataqueInvocacao.gameObject == gameObject;
    }

    private void OnValidate()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
        if (_comportamento == null)
            _comportamento = GetComponent<ComportamentoBossNecromante>();
        if (_movimento == null)
            _movimento = GetComponent<MovimentoBossBase>();
        if (_vida == null)
            _vida = GetComponent<VidaBoss>();
        if (_encontro == null)
            _encontro = GetComponent<ControladorEncontroNecromante>();
        if (_ataqueInvocacao == null)
            _ataqueInvocacao = GetComponent<AtaqueInvocacaoNecromante>();
    }
}
