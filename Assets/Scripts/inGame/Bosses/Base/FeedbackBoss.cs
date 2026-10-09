using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class FeedbackBoss : MonoBehaviour
{
    [SerializeField] private VidaBoss _vida;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [Header("Cores provisorias")]
    [SerializeField] private Color _corVulneravel = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color _corDano = Color.white;
    [SerializeField] private Color _corBloqueio = new Color(0.35f, 0.55f, 1f, 1f);
    [SerializeField] private Color _corEsgotado = new Color(0.25f, 0.25f, 0.25f, 1f);
    [SerializeField, Min(0.01f)] private float _duracaoFeedback = 0.12f;
    [Header("Piscar durante a vulnerabilidade")]
    [SerializeField] private bool _piscarQuandoVulneravel = true;
    [SerializeField, Min(0.01f)] private float _intervaloPiscarVulneravel = 0.18f;
    [SerializeField, Range(0f, 1f)] private float _alphaPiscarVulneravel = 0.45f;

    private Color _corOriginal;
    private Coroutine _feedbackAtual;
    private bool _exibirEsgotado = true;
    private float _tempoPiscarVulneravel;
    private bool _recuperandoVida;

    private void Awake()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_spriteRenderer != null)
            _corOriginal = _spriteRenderer.color;
    }

    private void OnEnable()
    {
        _tempoPiscarVulneravel = 0f;
        if (_vida == null) return;

        _vida.VidaAlterada += AoAlterarVida;
        _vida.VulnerabilidadeAlterada += AoAlterarVulnerabilidade;
        _vida.DanoRecebido += AoReceberDano;
        _vida.DanoBloqueado += AoBloquearDano;
        _vida.VidaEsgotada += AoEsgotarVida;
        AtualizarCorBase();
    }

    private void LateUpdate()
    {
        if (BloqueioGameplay.Bloqueado || _feedbackAtual != null
            || _spriteRenderer == null || _vida == null) return;
        if (!_recuperandoVida && (!_piscarQuandoVulneravel || !_vida.Vulneravel || _vida.Esgotada)) return;

        _tempoPiscarVulneravel += TempoGameplay.DeltaTime;
        Color cor = _recuperandoVida ? _corOriginal : _corVulneravel;
        if (Mathf.FloorToInt(_tempoPiscarVulneravel / _intervaloPiscarVulneravel) % 2 == 1)
            cor.a *= _alphaPiscarVulneravel;
        _spriteRenderer.color = cor;
    }

    private void OnDisable()
    {
        _recuperandoVida = false;
        if (_vida != null)
        {
            _vida.VidaAlterada -= AoAlterarVida;
            _vida.VulnerabilidadeAlterada -= AoAlterarVulnerabilidade;
            _vida.DanoRecebido -= AoReceberDano;
            _vida.DanoBloqueado -= AoBloquearDano;
            _vida.VidaEsgotada -= AoEsgotarVida;
        }

        if (_feedbackAtual != null)
            StopCoroutine(_feedbackAtual);

        _feedbackAtual = null;
        if (_spriteRenderer != null)
            _spriteRenderer.color = _corOriginal;
    }

    private void AoAlterarVida(int vidaAtual, int vidaMaxima)
    {
        if (_feedbackAtual == null)
            AtualizarCorBase();
    }

    private void AoAlterarVulnerabilidade(bool vulneravel)
    {
        _tempoPiscarVulneravel = 0f;
        if (_feedbackAtual == null)
            AtualizarCorBase();
    }

    private void AoReceberDano(int dano)
    {
        IniciarFeedback(_corDano);
    }

    private void AoBloquearDano(int dano)
    {
        IniciarFeedback(_corBloqueio);
    }

    private void AoEsgotarVida()
    {
        if (_feedbackAtual != null)
            StopCoroutine(_feedbackAtual);

        _feedbackAtual = null;
        AtualizarCorBase();
    }

    public void DefinirRecuperandoVida(bool recuperandoVida)
    {
        _recuperandoVida = recuperandoVida;
        _tempoPiscarVulneravel = 0f;
        if (_feedbackAtual != null) StopCoroutine(_feedbackAtual);
        _feedbackAtual = null;
        AtualizarCorBase();
    }

    public void DefinirExibirEsgotado(bool exibirEsgotado)
    {
        _exibirEsgotado = exibirEsgotado;
        if (_feedbackAtual == null)
            AtualizarCorBase();
    }

    private void IniciarFeedback(Color cor)
    {
        if (_recuperandoVida || _spriteRenderer == null || _vida == null || _vida.Esgotada) return;

        if (_feedbackAtual != null)
            StopCoroutine(_feedbackAtual);

        _feedbackAtual = StartCoroutine(ExibirFeedback(cor));
    }

    private IEnumerator ExibirFeedback(Color cor)
    {
        _spriteRenderer.color = cor;
        yield return new EsperaGameplay(_duracaoFeedback);
        _feedbackAtual = null;
        AtualizarCorBase();
    }

    private void AtualizarCorBase()
    {
        if (_spriteRenderer == null || _vida == null) return;

        _spriteRenderer.color = _recuperandoVida ? _corOriginal : _vida.Esgotada && _exibirEsgotado
            ? _corEsgotado
            : (_vida.Vulneravel ? _corVulneravel : _corOriginal);
    }

    private void OnValidate()
    {
        _duracaoFeedback = Mathf.Max(0.01f, _duracaoFeedback);
        _intervaloPiscarVulneravel = Mathf.Max(0.01f, _intervaloPiscarVulneravel);
        _alphaPiscarVulneravel = Mathf.Clamp01(_alphaPiscarVulneravel);
    }
}
