using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Mira : MonoBehaviour
{
    public const string ChaveSensibilidadeControle = "SensibilidadeControle";
    public const float SensibilidadeControlePadrao = 5f;
    public const string ChaveMiraOitoDirecoes = "MiraOitoDirecoes";
    private Camera cam;
    private INPUTS inputs;
    private SpriteRenderer sr;
    private Color corNormal;

    [Header("Mira no controle")]
    [SerializeField] private Transform _origemLancamento;
    [SerializeField, Min(0.1f)] private float _distanciaMiraControle = 12f;
    [SerializeField, Range(0f, 0.95f)] private float _zonaMortaControle = 0.3f;
    [SerializeField, Range(0f, 10f)] private float _sensibilidadeControle = SensibilidadeControlePadrao;
    [SerializeField, Range(0f, 10f), Tooltip("Ignora pequenas variacoes de direcao do analogico, em graus.")]
    private float _limiarMudancaDirecao = 3f;
    [SerializeField, Min(1f), Tooltip("Velocidade de ajuste da direcao, em graus por segundo.")]
    private float _velocidadeRotacaoControle = 720f;
    [SerializeField] private bool _miraOitoDirecoes = true;

    [Header("Assistencia de mira no controle")]
    [SerializeField] private bool _ativarAssistencia = true;
    [SerializeField, Range(0f, 1f)] private float _intensidadeAssistencia = 0.35f;
    [SerializeField, Range(1f, 20f)] private float _anguloAssistencia = 8f;
    [SerializeField, Range(0f, 5f)] private float _correcaoMaximaAssistencia = 2f;
    [SerializeField, Min(1f)] private float _alcanceAssistencia = 55f;
    private readonly Collider2D[] _alvosAssistencia = new Collider2D[128];

    private InputAction _mirarControle;
    private bool _usandoControle;
    private Vector2 _direcaoControle = Vector2.right;
    private Vector2 _direcaoDesejada = Vector2.right;
    public float SensibilidadeControle => _sensibilidadeControle;
    public bool MiraOitoDirecoes => _miraOitoDirecoes;

    [Header("Cooldown Visual")]
    public Image cooldownUI;
    [HideInInspector] public float cooldownProgresso;
    [HideInInspector] public bool emCooldown;

    private void Awake()
    {
        cam = Camera.main;
        inputs = new INPUTS();
        DefinirSensibilidadeControle(PlayerPrefs.GetFloat(ChaveSensibilidadeControle, SensibilidadeControlePadrao));
        DefinirMiraOitoDirecoes(PlayerPrefs.GetInt(ChaveMiraOitoDirecoes, 1) == 1);
        _mirarControle = inputs.asset.FindAction("Gameplay/MirarControle", true);
        sr = GetComponent<SpriteRenderer>();

        if (sr != null)
            corNormal = sr.color;
    }

    private void OnEnable()
    {
        _mirarControle.performed += AoMirarControle;
        inputs.Gameplay.Aim.performed += AoMirarMouse;
        inputs.Gameplay.Shoot.performed += AoUsarDispositivo;
        inputs.Gameplay.Move.performed += AoUsarDispositivo;
        inputs.Gameplay.Enable();
    }

    private void OnDisable()
    {
        _mirarControle.performed -= AoMirarControle;
        inputs.Gameplay.Aim.performed -= AoMirarMouse;
        inputs.Gameplay.Shoot.performed -= AoUsarDispositivo;
        inputs.Gameplay.Move.performed -= AoUsarDispositivo;
        inputs.Gameplay.Disable();
    }

    private void OnDestroy()
    {
        inputs?.Dispose();
    }

    public void DefinirOrigemLancamento(Transform origem)
    {
        _origemLancamento = origem;
    }

    public void DefinirSensibilidadeControle(float valor)
    {
        _sensibilidadeControle = Mathf.Clamp(valor, 0f, 10f);
    }

    public void DefinirMiraOitoDirecoes(bool ativada)
    {
        _miraOitoDirecoes = ativada;
        if (ativada)
            _direcaoControle = _direcaoDesejada = AjustarOitoDirecoes(_direcaoDesejada);
    }

    private Vector2 AjustarOitoDirecoes(Vector2 direcao)
    {
        float angulo = Mathf.Round(Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg / 45f) * 45f * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));
    }

    public Vector3 ObterPosicaoAlvo(Transform origem)
    {
        if (_usandoControle && origem != null)
            return origem.position + (Vector3)(ObterDirecaoAssistida(origem.position) * _distanciaMiraControle);

        if (cam == null) cam = Camera.main;
        if (cam == null) return transform.position;
        Vector3 posicao = cam.ScreenToWorldPoint(inputs.Gameplay.Aim.ReadValue<Vector2>());
        posicao.z = -5f;
        return posicao;
    }

    private void AoMirarControle(InputAction.CallbackContext contexto)
    {
        if (BloqueioGameplay.Bloqueado) return;
        Vector2 direcao = contexto.ReadValue<Vector2>();
        if (direcao.sqrMagnitude <= _zonaMortaControle * _zonaMortaControle) return;
        AtualizarDirecaoDesejada(direcao);
        _usandoControle = true;
    }

    private void AtualizarDirecaoDesejada(Vector2 direcao)
    {
        if (_miraOitoDirecoes)
        {
            _direcaoControle = _direcaoDesejada = AjustarOitoDirecoes(direcao);
            return;
        }
        if (Vector2.Angle(_direcaoDesejada, direcao) >= _limiarMudancaDirecao)
            _direcaoDesejada = direcao.normalized;
    }

    private Vector2 ObterDirecaoAssistida(Vector2 origem)
    {
        if (_miraOitoDirecoes || !_ativarAssistencia || _intensidadeAssistencia <= 0f || BloqueioGameplay.Bloqueado)
            return _direcaoControle;
        if (cam == null) cam = Camera.main;
        if (cam == null) return _direcaoControle;

        ContactFilter2D filtro = new ContactFilter2D { useTriggers = true };
        int quantidade = Physics2D.OverlapCircle(origem, _alcanceAssistencia, filtro, _alvosAssistencia);
        float menorAngulo = _anguloAssistencia;
        Vector2 direcaoAlvo = _direcaoControle;
        for (int i = 0; i < quantidade; i++)
        {
            Collider2D colisor = _alvosAssistencia[i];
            Entregavel entregavel = colisor.GetComponentInParent<Entregavel>();
            if (entregavel != null)
            {
                if (!entregavel.PodeReceberEntrega) continue;
            }
            else
            {
                ReceptorEntregaBoss boss = colisor.GetComponentInParent<ReceptorEntregaBoss>();
                if (boss == null || !boss.PodeReceberEntrega) continue;
            }

            Vector3 centro = colisor.bounds.center;
            Vector3 viewport = cam.WorldToViewportPoint(centro);
            if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
                continue;
            Vector2 deslocamento = (Vector2)centro - origem;
            if (deslocamento.sqrMagnitude < 0.01f || deslocamento.sqrMagnitude > _alcanceAssistencia * _alcanceAssistencia)
                continue;
            float angulo = Vector2.Angle(_direcaoControle, deslocamento);
            if (angulo >= menorAngulo) continue;
            menorAngulo = angulo;
            direcaoAlvo = deslocamento.normalized;
        }

        float atual = Mathf.Atan2(_direcaoControle.y, _direcaoControle.x) * Mathf.Rad2Deg;
        float alvo = Mathf.Atan2(direcaoAlvo.y, direcaoAlvo.x) * Mathf.Rad2Deg;
        float correcao = Mathf.Abs(Mathf.DeltaAngle(atual, alvo)) * _intensidadeAssistencia
            * (1f - menorAngulo / _anguloAssistencia);
        float anguloFinal = Mathf.MoveTowardsAngle(atual, alvo, Mathf.Min(correcao, _correcaoMaximaAssistencia)) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(anguloFinal), Mathf.Sin(anguloFinal));
    }

    private void AoMirarMouse(InputAction.CallbackContext contexto)
    {
        if (!BloqueioGameplay.Bloqueado) _usandoControle = false;
    }

    private void AoUsarDispositivo(InputAction.CallbackContext contexto)
    {
        if (BloqueioGameplay.Bloqueado) return;
        _usandoControle = contexto.control.device is Gamepad;
    }

    private void Update()
    {
        if (!BloqueioGameplay.Bloqueado)
        {
            if (_usandoControle)
            {
                Vector2 direcao = _mirarControle.ReadValue<Vector2>();
                if (direcao.sqrMagnitude > _zonaMortaControle * _zonaMortaControle)
                    AtualizarDirecaoDesejada(direcao);

                float anguloAtual = Mathf.Atan2(_direcaoControle.y, _direcaoControle.x) * Mathf.Rad2Deg;
                float anguloDesejado = Mathf.Atan2(_direcaoDesejada.y, _direcaoDesejada.x) * Mathf.Rad2Deg;
                float angulo = Mathf.MoveTowardsAngle(anguloAtual, anguloDesejado,
                    _velocidadeRotacaoControle * Mathf.Lerp(1f / 6f, 11f / 6f, _sensibilidadeControle / 10f)
                    * Time.deltaTime) * Mathf.Deg2Rad;
                _direcaoControle = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));
            }

            Vector3 posicao = ObterPosicaoAlvo(_origemLancamento);
            posicao.z = -5f;
            transform.position = posicao;
        }

        if (cooldownUI != null)
        {
            cooldownUI.fillAmount = 1f - cooldownProgresso;
            cooldownUI.enabled = emCooldown;
        }

        if (sr == null) return;

        Color corCooldown = new Color(0.55f, 0.55f, 0.55f, corNormal.a);
        sr.color = emCooldown
            ? Color.Lerp(corCooldown, corNormal, cooldownProgresso)
            : corNormal;
    }

    private void OnValidate()
    {
        _distanciaMiraControle = Mathf.Max(0.1f, _distanciaMiraControle);
        _zonaMortaControle = Mathf.Clamp(_zonaMortaControle, 0f, 0.95f);
        _sensibilidadeControle = Mathf.Clamp(_sensibilidadeControle, 0f, 10f);
        _limiarMudancaDirecao = Mathf.Clamp(_limiarMudancaDirecao, 0f, 10f);
        _velocidadeRotacaoControle = Mathf.Max(1f, _velocidadeRotacaoControle);
        _intensidadeAssistencia = Mathf.Clamp01(_intensidadeAssistencia);
        _anguloAssistencia = Mathf.Clamp(_anguloAssistencia, 1f, 20f);
        _correcaoMaximaAssistencia = Mathf.Clamp(_correcaoMaximaAssistencia, 0f, 5f);
        _alcanceAssistencia = Mathf.Max(1f, _alcanceAssistencia);
    }
}
