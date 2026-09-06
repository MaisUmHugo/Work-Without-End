using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class MovimentoNecromante : MovimentoBossBase
{
    [SerializeField] private Rigidbody2D _corpo;
    [Tooltip("Velocidade media. O deslocamento acelera e desacelera suavemente.")]
    [SerializeField, Min(0.1f)] private float _velocidade = 12f;
    [SerializeField, Min(0.01f)] private float _toleranciaChegada = 0.05f;

    private Vector2 _origem;
    private Vector2 _destino;
    private float _tempo;
    private float _duracao;
    private bool _aguardandoUltimoPasso;

    private bool _emMovimento;
    public override bool EmMovimento => _emMovimento;

    public override bool Inicializar()
    {
        if (_corpo == null) _corpo = GetComponent<Rigidbody2D>();
        if (_corpo == null || _corpo.gameObject != gameObject
            || _corpo.bodyType != RigidbodyType2D.Kinematic || !_corpo.simulated)
        {
            Debug.LogError("Necromante: use um Rigidbody2D Kinematic simulado na raiz.", this);
            return false;
        }

        Cancelar();
        return true;
    }

    public override void MoverPara(Vector2 destino)
    {
        Cancelar();
        _origem = _corpo.position;
        _destino = destino;
        float distancia = Vector2.Distance(_origem, _destino);
        if (distancia <= Mathf.Max(0.01f, _toleranciaChegada)) return;

        _duracao = distancia / Mathf.Max(0.1f, _velocidade);
        _tempo = 0f;
        _emMovimento = true;
    }

    public override void Atualizar(float deltaTime)
    {
        if (!EmMovimento || deltaTime <= 0f) return;

        // MovePosition sera aplicado pela fisica depois do FixedUpdate.
        if (_aguardandoUltimoPasso)
        {
            if (Vector2.Distance(_corpo.position, _destino) <= Mathf.Max(0.01f, _toleranciaChegada))
                Cancelar();
            else
                _corpo.MovePosition(_destino);
            return;
        }

        _tempo += deltaTime;
        float progresso = Mathf.Clamp01(_tempo / _duracao);
        float suavizado = progresso * progresso * (3f - 2f * progresso);
        _corpo.MovePosition(Vector2.Lerp(_origem, _destino, suavizado));
        _aguardandoUltimoPasso = progresso >= 1f;
    }

    public override void Cancelar()
    {
        _emMovimento = false;
        _aguardandoUltimoPasso = false;
        Pausar();
    }

    public override void Pausar()
    {
        if (_corpo != null)
        {
            _corpo.linearVelocity = Vector2.zero;
            _corpo.angularVelocity = 0f;
        }
    }
}
