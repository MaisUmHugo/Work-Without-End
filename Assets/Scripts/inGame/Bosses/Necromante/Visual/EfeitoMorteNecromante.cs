using UnityEngine;

[DisallowMultipleComponent]
public class EfeitoMorteNecromante : MonoBehaviour
{
    [SerializeField] private ControladorEncontroNecromante _encontro;
    [SerializeField] private ControleAnimatorNecromante _controleVisual;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private GameObject _prefabAlmas;
    [SerializeField] private Material _materialBrilho;
    [SerializeField, Min(0f)] private float _duracaoDesaparecimento = 3f;
    [SerializeField, Min(1f)] private float _intensidadeBrilho = 12f;
    [SerializeField, Min(0.1f)] private float _escalaAlmas = 2f;
    [SerializeField, Min(1f)] private float _intensidadeAlmas = 4f;

    private Material _materialOriginal;
    private Color _corOriginal;
    private MaterialPropertyBlock _propriedades;
    private MaterialPropertyBlock _propriedadesOriginais;
    private GameObject _almas;
    private float _tempo;
    private bool _emExecucao;
    private static readonly int CorMaterial = Shader.PropertyToID("_Color");

    public float DuracaoDesaparecimento => _duracaoDesaparecimento;

    private void Awake()
    {
        _propriedades = new MaterialPropertyBlock();
        _propriedadesOriginais = new MaterialPropertyBlock();
        if (_spriteRenderer != null) _corOriginal = _spriteRenderer.color;
    }

    private void OnEnable()
    {
        if (_encontro != null) _encontro.EncerramentoIniciado += AoEncerrar;
    }

    private void AoEncerrar(ResultadoEncontroBoss resultado)
    {
        if (resultado != ResultadoEncontroBoss.DerrotaDefinitiva || _spriteRenderer == null) return;
        _materialOriginal = _spriteRenderer.sharedMaterial;
        _spriteRenderer.GetPropertyBlock(_propriedadesOriginais);
        if (_materialBrilho != null) _spriteRenderer.sharedMaterial = _materialBrilho;
        _tempo = 0f;
        _emExecucao = true;
        if (_prefabAlmas != null)
        {
            _almas = Instantiate(_prefabAlmas, _spriteRenderer.bounds.center,
                _prefabAlmas.transform.rotation);
            _almas.transform.localScale *= _escalaAlmas;
            foreach (ParticleSystemRenderer render in _almas.GetComponentsInChildren<ParticleSystemRenderer>())
            {
                render.sortingLayerID = _spriteRenderer.sortingLayerID;
                render.sortingOrder = _spriteRenderer.sortingOrder + 1;
                var propriedadesAlmas = new MaterialPropertyBlock();
                render.GetPropertyBlock(propriedadesAlmas);
                propriedadesAlmas.SetFloat("_HdrMultiply", _intensidadeAlmas);
                render.SetPropertyBlock(propriedadesAlmas);
            }
            PausaGameplay.Registrar(_almas);
        }
    }

    private void LateUpdate()
    {
        if (!_emExecucao || _spriteRenderer == null || BloqueioGameplay.Bloqueado) return;
        _tempo += TempoGameplay.DeltaTime;
        float animacao = _controleVisual != null ? _controleVisual.DuracaoAnimacaoMorte : 0f;
        float progresso = _duracaoDesaparecimento > 0f
            ? Mathf.Clamp01((_tempo - animacao) / _duracaoDesaparecimento)
            : (_tempo >= animacao ? 1f : 0f);
        float alpha = 1f - Mathf.SmoothStep(0f, 1f, progresso);
        // O material unlit recebe HDR sem depender da cor escura do feedback de dano.
        Color corMaterial = Color.white * _intensidadeBrilho;
        corMaterial.a = 1f;
        _propriedades.SetColor(CorMaterial, corMaterial);
        _spriteRenderer.SetPropertyBlock(_propriedades);
        _spriteRenderer.color = new Color(1f, 1f, 1f, alpha);
    }

    private void OnDisable()
    {
        if (_encontro != null) _encontro.EncerramentoIniciado -= AoEncerrar;
        if (_emExecucao && _spriteRenderer != null)
        {
            _spriteRenderer.sharedMaterial = _materialOriginal;
            _spriteRenderer.color = _corOriginal;
            _spriteRenderer.SetPropertyBlock(_propriedadesOriginais);
        }
        _emExecucao = false;
        if (_almas != null) Destroy(_almas);
        _almas = null;
    }

    private void OnValidate()
    {
        if (_encontro == null) _encontro = GetComponent<ControladorEncontroNecromante>();
        if (_controleVisual == null) _controleVisual = GetComponent<ControleAnimatorNecromante>();
        if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _duracaoDesaparecimento = Mathf.Max(0f, _duracaoDesaparecimento);
        _intensidadeBrilho = Mathf.Max(1f, _intensidadeBrilho);
    }
}
