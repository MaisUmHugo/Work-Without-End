using UnityEngine;

[DisallowMultipleComponent]
public class ComportamentoBossNecromante : ComportamentoBossBase
{
    [Header("Combate")]
    [SerializeField] private VidaBoss _vida;
    [SerializeField] private SeletorAtaquesBoss _seletorAtaques;
    [Header("Destinos fixos na cena, fora da hierarquia do boss")]
    [SerializeField] private Transform _direitaSuperior;
    [SerializeField] private Transform _direitaInferior;
    [SerializeField] private Transform _centroSuperior;
    [Header("Tempos em segundos")]
    [SerializeField] private Vector2 _esperaPrincipal = new Vector2(3f, 5f);
    [SerializeField] private Vector2 _janelaVulnerabilidade = new Vector2(1.5f, 3f);
    [Header("Area de combate")]
    [Tooltip("Altura minima do centro da raiz. Considere tamanho do sprite e flutuacao.")]
    [SerializeField] private float _alturaMinima = 11.2f;
    [SerializeField] private Vector2 _limitesX = new Vector2(8f, 44.8f);

    private MovimentoBossBase _movimento;
    private float _tempoEspera;
    private bool _naPrincipal;
    private bool _proximaVisitaInferior;
    private EstadoBoss _estado = EstadoBoss.Inativo;
    private AtaqueBossBase _ataqueAtual;

    public override EstadoBoss EstadoAtual => _estado;

    public override bool Inicializar(MovimentoBossBase movimento)
    {
        if (movimento == null || _vida == null || _seletorAtaques == null
            || _vida.gameObject != gameObject || _seletorAtaques.gameObject != gameObject
            || !PontoValido(_direitaSuperior)
            || !PontoValido(_direitaInferior) || !PontoValido(_centroSuperior))
        {
            Debug.LogError("Necromante: configure vida, ataque e os tres destinos do boss.", this);
            return false;
        }

        _movimento = movimento;
        return _seletorAtaques.Inicializar();
    }

    private bool PontoValido(Transform ponto)
    {
        return ponto != null && ponto != transform && !ponto.IsChildOf(transform);
    }

    public override void Iniciar()
    {
        _seletorAtaques.Reiniciar();
        _vida.DefinirVulneravel(false);
        _proximaVisitaInferior = true;
        _naPrincipal = true;
        _estado = EstadoBoss.Entrando;
        _movimento.MoverPara(LimitarDestino(_direitaSuperior.position));
    }

    public override void Atualizar(float deltaTime)
    {
        if (_estado == EstadoBoss.Inativo || deltaTime <= 0f) return;

        _seletorAtaques.AtualizarCooldowns(deltaTime);

        if (_estado == EstadoBoss.Entrando || _estado == EstadoBoss.Reposicionando)
        {
            if (_movimento.EmMovimento) return;

            _estado = _naPrincipal ? EstadoBoss.Aguardando : EstadoBoss.Vulneravel;
            _tempoEspera = SortearEspera(_naPrincipal ? _esperaPrincipal : _janelaVulnerabilidade);
            _vida.DefinirVulneravel(!_naPrincipal);
            return;
        }

        if (_estado == EstadoBoss.PreparandoAtaque || _estado == EstadoBoss.Atacando)
        {
            _ataqueAtual.Atualizar(deltaTime);
            if (_ataqueAtual.EmExecucao)
            {
                _estado = _ataqueAtual.EmPreparacao
                    ? EstadoBoss.PreparandoAtaque
                    : EstadoBoss.Atacando;
                return;
            }

            _ataqueAtual = null;
            MoverParaPontoSecundario();
            return;
        }

        _tempoEspera -= deltaTime;
        if (_tempoEspera > 0f) return;

        if (_naPrincipal)
        {
            if (!_seletorAtaques.TentarIniciar(null, out _ataqueAtual))
            {
                Cancelar();
                return;
            }

            _estado = _ataqueAtual.EmPreparacao
                ? EstadoBoss.PreparandoAtaque
                : EstadoBoss.Atacando;
            return;
        }

        IniciarMovimento(_direitaSuperior, true);
    }

    private void MoverParaPontoSecundario()
    {
        Transform destino = _proximaVisitaInferior ? _direitaInferior : _centroSuperior;
        _proximaVisitaInferior = !_proximaVisitaInferior;
        IniciarMovimento(destino, false);
    }

    private void IniciarMovimento(Transform destino, bool paraPrincipal)
    {
        if (destino == null)
        {
            Cancelar();
            return;
        }

        _naPrincipal = paraPrincipal;
        _vida.DefinirVulneravel(false);
        _estado = EstadoBoss.Reposicionando;
        _movimento.MoverPara(LimitarDestino(destino.position));
    }

    private Vector2 LimitarDestino(Vector2 destino)
    {
        destino.x = Mathf.Clamp(destino.x, Mathf.Min(_limitesX.x, _limitesX.y), Mathf.Max(_limitesX.x, _limitesX.y));
        destino.y = Mathf.Max(_alturaMinima, destino.y);
        return destino;
    }

    private static float SortearEspera(Vector2 intervalo)
    {
        float minimo = Mathf.Max(0f, Mathf.Min(intervalo.x, intervalo.y));
        float maximo = Mathf.Max(minimo, Mathf.Max(intervalo.x, intervalo.y));
        return Random.Range(minimo, maximo);
    }

    public override void Cancelar()
    {
        _seletorAtaques?.CancelarTodos();
        _ataqueAtual = null;
        _vida?.DefinirVulneravel(false);
        _movimento?.Cancelar();
        _estado = EstadoBoss.Inativo;
        _tempoEspera = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        DesenharDestino(_direitaSuperior, Color.cyan);
        DesenharDestino(_direitaInferior, Color.yellow);
        DesenharDestino(_centroSuperior, Color.green);
    }

    private void DesenharDestino(Transform ponto, Color cor)
    {
        if (ponto == null) return;
        Vector2 destino = LimitarDestino(ponto.position);
        Gizmos.color = cor;
        Gizmos.DrawWireSphere(new Vector3(destino.x, destino.y, transform.position.z), 0.6f);
        Gizmos.DrawLine(transform.position, new Vector3(destino.x, destino.y, transform.position.z));
    }
}
