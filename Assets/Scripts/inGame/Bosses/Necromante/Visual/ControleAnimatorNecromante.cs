using UnityEngine;

[DisallowMultipleComponent]
public class ControleAnimatorNecromante : MonoBehaviour
{
    private static readonly int EstaMovendo = Animator.StringToHash("EstaMovendo");
    private static readonly int EstaAtacando = Animator.StringToHash("EstaAtacando");
    private static readonly int EstaVulneravel = Animator.StringToHash("EstaVulneravel");
    private static readonly int EstaMorto = Animator.StringToHash("EstaMorto");
    private static readonly int TipoAtaque = Animator.StringToHash("TipoAtaque");

    [Header("Referencias")]
    [SerializeField] private Animator _animator;
    [SerializeField] private ComportamentoBossNecromante _comportamento;
    [SerializeField] private MovimentoBossBase _movimento;
    [SerializeField] private VidaBoss _vida;
    [SerializeField] private ControladorEncontroNecromante _encontro;

    public TipoAtaqueNecromante TipoAtaqueAtual { get; private set; }

    private void Awake()
    {
        if (!ReferenciasValidas())
        {
            Debug.LogError("Necromante: configure Animator, comportamento, movimento e vida no controle visual.", this);
            enabled = false;
            return;
        }

        AtualizarParametros();
    }

    private void Update()
    {
        AtualizarParametros();
    }

    private void AtualizarParametros()
    {
        if (_animator == null || _comportamento == null || _movimento == null
            || _vida == null || _encontro == null)
            return;

        bool fugindo = _encontro.ResultadoAtual == ResultadoEncontroBoss.Fuga;
        bool morto = _vida.Esgotada && !fugindo;
        bool vulneravel = !morto && _vida.Vulneravel;
        bool atacando = !morto && !vulneravel && AtaqueEmExecucao();
        bool movendo = !morto && !vulneravel && !atacando && _movimento.EmMovimento;

        TipoAtaqueAtual = atacando
            ? IdentificarTipoAtaque(_comportamento.AtaqueAtual)
            : TipoAtaqueNecromante.Nenhum;

        _animator.SetBool(EstaMorto, morto);
        _animator.SetBool(EstaVulneravel, vulneravel);
        _animator.SetBool(EstaAtacando, atacando);
        _animator.SetBool(EstaMovendo, movendo);
        _animator.SetInteger(TipoAtaque, (int)TipoAtaqueAtual);
    }

    private bool AtaqueEmExecucao()
    {
        EstadoBoss estado = _comportamento.EstadoAtual;
        return _comportamento.AtaqueAtual != null
            && _comportamento.AtaqueAtual.EmExecucao
            && (estado == EstadoBoss.PreparandoAtaque || estado == EstadoBoss.Atacando);
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
            && _encontro != null && _encontro.gameObject == gameObject;
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
    }
}
