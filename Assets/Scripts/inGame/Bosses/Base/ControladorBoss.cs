using UnityEngine;

[DisallowMultipleComponent]
public class ControladorBoss : MonoBehaviour
{
    [Header("Componentes")]
    [SerializeField] private ComportamentoBossBase _comportamento;
    [SerializeField] private MovimentoBossBase _movimento;
    [Header("Inicio")]
    [SerializeField] private bool _iniciarAutomaticamente = true;

    private bool _inicializado;
    private bool _emExecucao;
    private bool _inicioSolicitado;

    public EstadoBoss EstadoAtual => _emExecucao ? _comportamento.EstadoAtual : EstadoBoss.Inativo;
    public bool PodeAtualizar => _emExecucao && isActiveAndEnabled
        && !BloqueioGameplay.Bloqueado && Time.timeScale > 0f;

    private void OnEnable()
    {
        _inicioSolicitado = _iniciarAutomaticamente;
    }

    public void IniciarEncontro()
    {
        if (!_emExecucao)
            _inicioSolicitado = true;
    }

    private bool Inicializar()
    {
        if (_inicializado) return true;

        if (_comportamento == null || _movimento == null
            || _comportamento.gameObject != gameObject || _movimento.gameObject != gameObject)
        {
            Debug.LogError("Boss: atribua o comportamento e o movimento da mesma raiz.", this);
            return false;
        }

        _inicializado = _movimento.Inicializar() && _comportamento.Inicializar(_movimento);
        return _inicializado;
    }

    private void FixedUpdate()
    {
        if (BloqueioGameplay.Bloqueado || Time.timeScale <= 0f)
        {
            if (_inicializado) _movimento.Pausar();
            return;
        }

        if (_inicioSolicitado)
        {
            _inicioSolicitado = false;
            if (!Inicializar())
            {
                enabled = false;
                return;
            }

            _emExecucao = true;
            _comportamento.Iniciar();
        }

        if (!_emExecucao) return;

        if (!_comportamento.isActiveAndEnabled || !_movimento.isActiveAndEnabled)
        {
            EncerrarExecucao();
            return;
        }

        _comportamento.Atualizar(Time.fixedDeltaTime);
        _movimento.Atualizar(Time.fixedDeltaTime);
    }

    public void EncerrarExecucao()
    {
        _inicioSolicitado = false;
        _emExecucao = false;
        if (_inicializado)
        {
            _comportamento.Cancelar();
            _movimento.Cancelar();
        }
    }

    private void OnDisable()
    {
        EncerrarExecucao();
        _inicializado = false;
    }
}
