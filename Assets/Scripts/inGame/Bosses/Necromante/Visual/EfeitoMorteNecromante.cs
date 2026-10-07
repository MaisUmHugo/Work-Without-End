using UnityEngine;

[DisallowMultipleComponent]
public class EfeitoMorteNecromante : MonoBehaviour
{
    [SerializeField] private ControladorEncontroNecromante _encontro;
    [SerializeField] private ControleAnimatorNecromante _controleVisual;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Material _materialBrilho;
    [SerializeField, Min(1f)] private float _intensidadeBrilho = 12f;
    [Header("Fumaca pixel no desmanche final")]
    [SerializeField] private Sprite[] _quadrosFumaca;
    [SerializeField, Min(0.1f)] private float _duracaoFumaca = 1.2f;
    [SerializeField, Min(0.1f)] private float _coberturaFumaca = 1.1f;
    [SerializeField, Range(0f, 1f)] private float _alturaFumacaNoCorpo = 0.2f;
    [SerializeField] private Color _corFumaca = new Color(0.85f, 0.8f, 1f, 1f);

    private Material _materialOriginal;
    private Color _corOriginal;
    private bool _spriteEstavaVisivel;
    private MaterialPropertyBlock _propriedades;
    private MaterialPropertyBlock _propriedadesOriginais;
    private SpriteRenderer _fumaca;
    private float _tempoFumaca;
    private bool _emExecucao;
    private bool _fumacaIniciada;
    private static readonly int CorMaterial = Shader.PropertyToID("_Color");

    public bool Concluido { get; private set; }
    public float DuracaoSequencia => _controleVisual == null ? _duracaoFumaca
        : Mathf.Max(_controleVisual.DuracaoAnimacaoMorte,
            _controleVisual.TempoInicioDesmancheMorte + _duracaoFumaca);

    private void Awake()
    {
        _propriedades = new MaterialPropertyBlock();
        _propriedadesOriginais = new MaterialPropertyBlock();
        if (_spriteRenderer != null) _corOriginal = _spriteRenderer.color;
    }

    private void OnEnable()
    {
        Concluido = false;
        if (_encontro != null) _encontro.EncerramentoIniciado += AoEncerrar;
    }

    private void AoEncerrar(ResultadoEncontroBoss resultado)
    {
        if (resultado != ResultadoEncontroBoss.DerrotaDefinitiva || _spriteRenderer == null) return;
        _materialOriginal = _spriteRenderer.sharedMaterial;
        _spriteEstavaVisivel = _spriteRenderer.enabled;
        _spriteRenderer.GetPropertyBlock(_propriedadesOriginais);
        if (_materialBrilho != null) _spriteRenderer.sharedMaterial = _materialBrilho;
        _emExecucao = true;
        _fumacaIniciada = false;
        _tempoFumaca = 0f;
        Concluido = false;
    }

    private void IniciarFumaca()
    {
        _fumacaIniciada = true;
        if (_quadrosFumaca == null || _quadrosFumaca.Length == 0 || _quadrosFumaca[0] == null) return;
        var objeto = new GameObject("FumacaMorteNecromante");
        objeto.transform.position = PosicaoFumaca();
        _fumaca = objeto.AddComponent<SpriteRenderer>();
        _fumaca.sprite = _quadrosFumaca[0];
        _fumaca.sharedMaterial = _materialBrilho != null ? _materialBrilho : _materialOriginal;
        _fumaca.sortingLayerID = _spriteRenderer.sortingLayerID;
        _fumaca.sortingOrder = _spriteRenderer.sortingOrder + 1;
        _fumaca.color = _corFumaca;
        Vector3 tamanho = _spriteRenderer.bounds.size;
        float escala = Mathf.Max(tamanho.x, tamanho.y) * _coberturaFumaca
            / Mathf.Max(0.01f, _quadrosFumaca[0].bounds.size.y);
        objeto.transform.localScale = Vector3.one * escala;
    }

    private void LateUpdate()
    {
        if (!_emExecucao || _spriteRenderer == null || _controleVisual == null || BloqueioGameplay.Bloqueado) return;
        float progresso = _controleVisual.ProgressoAnimacaoMorte;
        float inicio = _controleVisual.InicioDesmancheMorte;
        if (progresso >= inicio)
        {
            if (!_fumacaIniciada) IniciarFumaca();
            _tempoFumaca += TempoGameplay.DeltaTime;
        }
        float desmanche = Mathf.InverseLerp(inicio, 0.98f, progresso);
        // O material unlit recebe HDR sem depender da cor escura do feedback de dano.
        Color corMaterial = Color.white * _intensidadeBrilho;
        corMaterial.a = 1f;
        _propriedades.SetColor(CorMaterial, corMaterial);
        _spriteRenderer.SetPropertyBlock(_propriedades);
        _spriteRenderer.color = new Color(1f, 1f, 1f, 1f - Mathf.SmoothStep(0f, 1f, desmanche));
        if (desmanche >= 1f) _spriteRenderer.enabled = false;

        if (_fumaca != null)
        {
            _fumaca.transform.position = PosicaoFumaca();
            float tempoNormalizado = Mathf.Clamp01(_tempoFumaca / _duracaoFumaca);
            int indice = Mathf.Min(_quadrosFumaca.Length - 1, Mathf.FloorToInt(tempoNormalizado * _quadrosFumaca.Length));
            _fumaca.sprite = _quadrosFumaca[indice];
            Color cor = _corFumaca;
            cor.a *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 1f, tempoNormalizado));
            _fumaca.color = cor;
            if (tempoNormalizado >= 1f) LimparFumaca();
        }
        Concluido = progresso >= 1f && _fumacaIniciada
            && (_fumaca == null || _tempoFumaca >= _duracaoFumaca);
    }

    private Vector3 PosicaoFumaca()
    {
        Bounds corpo = _spriteRenderer.bounds;
        return new Vector3(corpo.center.x, corpo.min.y + corpo.size.y * _alturaFumacaNoCorpo, corpo.center.z);
    }

    private void LimparFumaca()
    {
        if (_fumaca != null) Destroy(_fumaca.gameObject);
        _fumaca = null;
    }

    private void OnDisable()
    {
        if (_encontro != null) _encontro.EncerramentoIniciado -= AoEncerrar;
        if (_emExecucao && _spriteRenderer != null)
        {
            _spriteRenderer.sharedMaterial = _materialOriginal;
            _spriteRenderer.color = _corOriginal;
            _spriteRenderer.enabled = _spriteEstavaVisivel;
            _spriteRenderer.SetPropertyBlock(_propriedadesOriginais);
        }
        _emExecucao = false;
        LimparFumaca();
    }

    private void OnValidate()
    {
        if (_encontro == null) _encontro = GetComponent<ControladorEncontroNecromante>();
        if (_controleVisual == null) _controleVisual = GetComponent<ControleAnimatorNecromante>();
        if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _duracaoFumaca = Mathf.Max(0.1f, _duracaoFumaca);
        _intensidadeBrilho = Mathf.Max(1f, _intensidadeBrilho);
    }
}
