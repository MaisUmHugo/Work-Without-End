using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class ProjetilNecromante : MonoBehaviour
{
    [SerializeField] private Rigidbody2D _corpo;
    [Tooltip("Protecao para projeteis que nunca alcancem uma camera valida.")]
    [SerializeField, Min(1f)] private float _tempoMaximo = 20f;
    [SerializeField, Min(0f)] private float _margemSaidaCamera = 0.1f;
    [SerializeField] private bool _rotacionarComDirecao = true;

    private Vector2 _direcao;
    private float _velocidade;
    private int _dano = 1;
    private float _tempoAtivo;
    private bool _configurado;
    private bool _impactou;
    private Camera _camera;
    private bool _entrouNaCamera;

    private void Awake()
    {
        if (_corpo == null)
            _corpo = GetComponent<Rigidbody2D>();
    }

    public void Configurar(Vector2 direcao, float velocidade)
    {
        Configurar(direcao, velocidade, 1);
    }

    public void Configurar(Vector2 direcao, float velocidade, int dano)
    {
        _direcao = direcao.sqrMagnitude > 0f ? direcao.normalized : Vector2.left;
        _velocidade = Mathf.Max(0.1f, velocidade);
        _dano = Mathf.Max(1, dano);
        _tempoAtivo = 0f;
        _impactou = false;
        _configurado = true;
        _camera = Camera.main;
        _entrouNaCamera = false;

        if (_rotacionarComDirecao)
            transform.right = _direcao;

        AtualizarVisibilidade(transform.position);
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
        if (_tempoAtivo >= Mathf.Max(1f, _tempoMaximo) && (!_entrouNaCamera || _camera == null))
        {
            Destruir();
            return;
        }

        Vector2 proximaPosicao = _corpo.position + _direcao * (_velocidade * Time.fixedDeltaTime);
        _corpo.MovePosition(proximaPosicao);
        AtualizarVisibilidade(proximaPosicao);
    }

    private void AtualizarVisibilidade(Vector3 posicao)
    {
        if (_camera == null)
            _camera = Camera.main;
        if (_camera == null) return;

        Vector3 viewport = _camera.WorldToViewportPoint(posicao);
        bool dentro = viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f
            && viewport.y >= 0f && viewport.y <= 1f;
        if (dentro)
        {
            _entrouNaCamera = true;
            return;
        }

        float margem = Mathf.Max(0f, _margemSaidaCamera);
        bool foraComMargem = viewport.z <= 0f || viewport.x < -margem || viewport.x > 1f + margem
            || viewport.y < -margem || viewport.y > 1f + margem;
        if (_entrouNaCamera && foraComMargem)
            Destruir();
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
        VidaManager.instance?.PerderVidas(_dano);
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
        _tempoMaximo = Mathf.Max(1f, _tempoMaximo);
        _margemSaidaCamera = Mathf.Max(0f, _margemSaidaCamera);
    }
}
