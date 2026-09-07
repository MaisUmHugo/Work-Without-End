using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class ProjetilNecromante : MonoBehaviour
{
    [SerializeField] private Rigidbody2D _corpo;
    [SerializeField, Min(0.1f)] private float _tempoMaximo = 6f;
    [SerializeField] private bool _rotacionarComDirecao = true;

    private Vector2 _direcao;
    private float _velocidade;
    private float _tempoAtivo;
    private bool _configurado;
    private bool _impactou;

    private void Awake()
    {
        if (_corpo == null)
            _corpo = GetComponent<Rigidbody2D>();
    }

    public void Configurar(Vector2 direcao, float velocidade)
    {
        _direcao = direcao.sqrMagnitude > 0f ? direcao.normalized : Vector2.left;
        _velocidade = Mathf.Max(0.1f, velocidade);
        _tempoAtivo = 0f;
        _impactou = false;
        _configurado = true;

        if (_rotacionarComDirecao)
            transform.right = _direcao;
    }

    private void FixedUpdate()
    {
        if (!_configurado || _impactou || _corpo == null) return;

        if (BloqueioGameplay.Bloqueado || Time.timeScale <= 0f)
        {
            _corpo.linearVelocity = Vector2.zero;
            return;
        }

        _tempoAtivo += Time.fixedDeltaTime;
        if (_tempoAtivo >= Mathf.Max(0.1f, _tempoMaximo))
        {
            Destruir();
            return;
        }

        _corpo.MovePosition(_corpo.position + _direcao * (_velocidade * Time.fixedDeltaTime));
    }

    private void OnTriggerEnter2D(Collider2D colisao)
    {
        TentarAtingirJogador(colisao);
    }

    private void OnTriggerStay2D(Collider2D colisao)
    {
        TentarAtingirJogador(colisao);
    }

    private void TentarAtingirJogador(Collider2D colisao)
    {
        if (_impactou || BloqueioGameplay.Bloqueado || !colisao.CompareTag("Player")) return;

        _impactou = true;
        VidaManager.instance?.PerderVida();
        Destruir();
    }

    private void Destruir()
    {
        _impactou = true;
        if (_corpo != null)
            _corpo.linearVelocity = Vector2.zero;
        Destroy(gameObject);
    }

    private void OnValidate()
    {
        _tempoMaximo = Mathf.Max(0.1f, _tempoMaximo);
    }
}
