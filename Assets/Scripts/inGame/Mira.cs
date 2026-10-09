using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum ModoMiraControle
{
    DirecoesFixas,
    Livre,
    Cursor
}

public class Mira : MonoBehaviour
{
    public const string ChaveSensibilidadeControle = "SensibilidadeControle";
    public const float SensibilidadeControlePadrao = 5f;
    public const string ChaveMiraOitoDirecoes = "MiraOitoDirecoes";
    public const string ChaveModoMiraControle = "ModoMiraControle";
    public const string ChaveAssistenciaControle = "AssistenciaMiraControle";
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
    [SerializeField, HideInInspector] private bool _miraOitoDirecoes;
    [SerializeField] private ModoMiraControle _modoMiraControle = ModoMiraControle.Cursor;
    [SerializeField, Tooltip("Graus em relacao a direita. Inclui mais diagonais para frente.")]
    private float[] _angulosDirecoesFixas = { -180f, -135f, -90f, -75f, -60f, -45f, -30f, -15f, 0f, 15f, 30f, 45f, 60f, 75f, 90f, 135f };
    [SerializeField, Min(1f)] private float _velocidadeCursorControle = 900f;
    private Vector2 _posicaoCursorControle;
    private bool _cursorControlePreparado;

    [Header("Assistencia de mira no controle")]
    [SerializeField] private bool _ativarAssistencia = true;
    [SerializeField, Range(0f, 1f)] private float _intensidadeAssistencia = 0.7f;
    [SerializeField, Range(1f, 20f)] private float _anguloAssistencia = 12f;
    [SerializeField, Range(0f, 12f)] private float _correcaoMaximaAssistencia = 6f;
    [SerializeField, Min(1f)] private float _alcanceAssistencia = 55f;
    private readonly Collider2D[] _alvosAssistencia = new Collider2D[128];
    private Collider2D _alvoAssistencia;

    private InputAction _mirarControle;
    private bool _usandoControle;
    private Vector2 _direcaoControle = Vector2.right;
    private Vector2 _direcaoDesejada = Vector2.right;
    public float SensibilidadeControle => _sensibilidadeControle;
    public bool MiraOitoDirecoes => _miraOitoDirecoes;
    public ModoMiraControle ModoControle => _modoMiraControle;
    public bool AssistenciaAtivada => _ativarAssistencia;

    public static ModoMiraControle ObterModoSalvo()
    {
        int anterior = PlayerPrefs.HasKey(ChaveMiraOitoDirecoes)
            ? (PlayerPrefs.GetInt(ChaveMiraOitoDirecoes) == 1 ? 0 : 1)
            : (int)ModoMiraControle.Cursor;
        return (ModoMiraControle)Mathf.Clamp(PlayerPrefs.GetInt(ChaveModoMiraControle, anterior), 0, 2);
    }

    [Header("Cooldown Visual")]
    public Image cooldownUI;
    [HideInInspector] public float cooldownProgresso;
    [HideInInspector] public bool emCooldown;

    private void Awake()
    {
        cam = Camera.main;
        inputs = new INPUTS();
        DefinirSensibilidadeControle(PlayerPrefs.GetFloat(ChaveSensibilidadeControle, SensibilidadeControlePadrao));
        DefinirModoControle(ObterModoSalvo());
        DefinirAssistencia(PlayerPrefs.GetInt(ChaveAssistenciaControle, 1) == 1);
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
        if (_modoMiraControle == ModoMiraControle.Cursor && !_cursorControlePreparado)
            PrepararCursorControle(origem);
    }

    public void DefinirSensibilidadeControle(float valor)
    {
        _sensibilidadeControle = Mathf.Clamp(valor, 0f, 10f);
    }

    public void DefinirMiraOitoDirecoes(bool ativada)
    {
        DefinirModoControle(ativada ? ModoMiraControle.DirecoesFixas : ModoMiraControle.Livre);
    }

    public void DefinirModoControle(ModoMiraControle modo)
    {
        _modoMiraControle = (ModoMiraControle)Mathf.Clamp((int)modo, 0, 2);
        _miraOitoDirecoes = _modoMiraControle == ModoMiraControle.DirecoesFixas;
        _alvoAssistencia = null;
        if (_miraOitoDirecoes)
            _direcaoControle = _direcaoDesejada = AjustarDirecoesFixas(_direcaoDesejada);
        _cursorControlePreparado = false;
    }

    public void DefinirAssistencia(bool ativada)
    {
        _ativarAssistencia = ativada;
        _alvoAssistencia = null;
    }

    private Vector2 AjustarDirecoesFixas(Vector2 direcao)
    {
        float desejado = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
        float escolhido = Mathf.Round(desejado / 45f) * 45f;
        float menorDiferenca = float.MaxValue;
        if (_angulosDirecoesFixas != null)
        {
            foreach (float candidato in _angulosDirecoesFixas)
            {
                float diferenca = Mathf.Abs(Mathf.DeltaAngle(desejado, candidato));
                if (diferenca >= menorDiferenca) continue;
                escolhido = candidato;
                menorDiferenca = diferenca;
            }
        }
        float angulo = escolhido * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));
    }

    private void PrepararCursorControle(Transform origem)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || origem == null) return;
        Vector3 alvo = _usandoControle ? transform.position : origem.position + (Vector3)(_direcaoControle * _distanciaMiraControle);
        _posicaoCursorControle = cam.WorldToScreenPoint(alvo);
        LimitarCursorNaTela();
        _cursorControlePreparado = true;
    }

    private void LimitarCursorNaTela()
    {
        _posicaoCursorControle.x = Mathf.Clamp(_posicaoCursorControle.x, 0f, Mathf.Max(0f, Screen.width - 1f));
        _posicaoCursorControle.y = Mathf.Clamp(_posicaoCursorControle.y, 0f, Mathf.Max(0f, Screen.height - 1f));
    }

    public Vector3 ObterPosicaoAlvo(Transform origem)
    {
        if (_usandoControle && origem != null)
        {
            if (_modoMiraControle == ModoMiraControle.Cursor)
            {
                if (!_cursorControlePreparado) PrepararCursorControle(origem);
                if (cam == null) return transform.position;
                Vector3 alvo = cam.ScreenToWorldPoint(_posicaoCursorControle);
                Vector2 deslocamento = (Vector2)(alvo - origem.position);
                return origem.position + (Vector3)(ObterDirecaoAssistida(origem.position, deslocamento.normalized) * deslocamento.magnitude);
            }
            return origem.position + (Vector3)(ObterDirecaoAssistida(origem.position, _direcaoControle) * _distanciaMiraControle);
        }

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
        if (_modoMiraControle != ModoMiraControle.Cursor) AtualizarDirecaoDesejada(direcao);
        _usandoControle = true;
    }

    private void AtualizarDirecaoDesejada(Vector2 direcao)
    {
        if (_miraOitoDirecoes)
        {
            _direcaoControle = _direcaoDesejada = AjustarDirecoesFixas(direcao);
            return;
        }
        if (Vector2.Angle(_direcaoDesejada, direcao) >= _limiarMudancaDirecao)
            _direcaoDesejada = direcao.normalized;
    }

    private Vector2 ObterDirecaoAssistida(Vector2 origem, Vector2 direcao)
    {
        if (_miraOitoDirecoes || !_ativarAssistencia || _intensidadeAssistencia <= 0f || BloqueioGameplay.Bloqueado)
            return direcao;
        if (cam == null) cam = Camera.main;
        if (cam == null) return direcao;

        ContactFilter2D filtro = new ContactFilter2D { useTriggers = true };
        int quantidade = Physics2D.OverlapCircle(origem, _alcanceAssistencia, filtro, _alvosAssistencia);
        float menorAngulo = _anguloAssistencia;
        Vector2 direcaoAlvo = direcao;
        Collider2D escolhido = null;
        // Mantem o alvo proximo para evitar saltos entre varios entregaveis.
        if (TentarObterAlvoAssistencia(_alvoAssistencia, origem, direcao, out Vector2 alvoAnterior, out float anguloAnterior)
            && anguloAnterior < _anguloAssistencia)
        {
            escolhido = _alvoAssistencia;
            menorAngulo = anguloAnterior;
            direcaoAlvo = alvoAnterior;
        }
        for (int i = 0; i < quantidade; i++)
        {
            Collider2D colisor = _alvosAssistencia[i];
            if (!TentarObterAlvoAssistencia(colisor, origem, direcao, out Vector2 direcaoCandidata, out float angulo)) continue;
            if (angulo >= menorAngulo - (escolhido == _alvoAssistencia && escolhido != null ? 2f : 0f)) continue;
            menorAngulo = angulo;
            direcaoAlvo = direcaoCandidata;
            escolhido = colisor;
        }
        _alvoAssistencia = escolhido;

        float atual = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
        float alvo = Mathf.Atan2(direcaoAlvo.y, direcaoAlvo.x) * Mathf.Rad2Deg;
        float correcao = Mathf.Abs(Mathf.DeltaAngle(atual, alvo)) * _intensidadeAssistencia;
        float anguloFinal = Mathf.MoveTowardsAngle(atual, alvo, Mathf.Min(correcao, _correcaoMaximaAssistencia)) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(anguloFinal), Mathf.Sin(anguloFinal));
    }

    private bool TentarObterAlvoAssistencia(Collider2D colisor, Vector2 origem, Vector2 direcao, out Vector2 alvo, out float angulo)
    {
        alvo = direcao;
        angulo = float.MaxValue;
        if (colisor == null || !colisor.enabled || !colisor.gameObject.activeInHierarchy) return false;
        Entregavel entregavel = colisor.GetComponentInParent<Entregavel>();
        if (entregavel != null)
        {
            if (!entregavel.PodeReceberEntrega) return false;
        }
        else
        {
            ReceptorEntregaBoss boss = colisor.GetComponentInParent<ReceptorEntregaBoss>();
            if (boss == null || !boss.PodeReceberEntrega) return false;
        }
        Vector3 centro = colisor.bounds.center;
        Vector3 viewport = cam.WorldToViewportPoint(centro);
        if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f) return false;
        Vector2 deslocamento = (Vector2)centro - origem;
        if (deslocamento.sqrMagnitude < .01f || deslocamento.sqrMagnitude > _alcanceAssistencia * _alcanceAssistencia) return false;
        alvo = deslocamento.normalized;
        angulo = Vector2.Angle(direcao, alvo);
        return angulo < _anguloAssistencia;
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
                if (_modoMiraControle == ModoMiraControle.Cursor)
                {
                    if (!_cursorControlePreparado) PrepararCursorControle(_origemLancamento);
                    if (direcao.magnitude > _zonaMortaControle)
                    {
                        float intensidade = Mathf.InverseLerp(_zonaMortaControle, 1f, direcao.magnitude);
                        _posicaoCursorControle += direcao.normalized * intensidade * _velocidadeCursorControle
                            * Mathf.Lerp(.25f, 2f, _sensibilidadeControle / 10f) * (Screen.height / 1080f) * Time.deltaTime;
                    }
                    LimitarCursorNaTela();
                }
                else
                {
                    if (direcao.sqrMagnitude > _zonaMortaControle * _zonaMortaControle)
                        AtualizarDirecaoDesejada(direcao);

                    float anguloAtual = Mathf.Atan2(_direcaoControle.y, _direcaoControle.x) * Mathf.Rad2Deg;
                    float anguloDesejado = Mathf.Atan2(_direcaoDesejada.y, _direcaoDesejada.x) * Mathf.Rad2Deg;
                    float angulo = Mathf.MoveTowardsAngle(anguloAtual, anguloDesejado,
                        _velocidadeRotacaoControle * Mathf.Lerp(1f / 6f, 11f / 6f, _sensibilidadeControle / 10f)
                        * Time.deltaTime) * Mathf.Deg2Rad;
                    _direcaoControle = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));
                }
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
        _correcaoMaximaAssistencia = Mathf.Clamp(_correcaoMaximaAssistencia, 0f, 12f);
        _alcanceAssistencia = Mathf.Max(1f, _alcanceAssistencia);
        _velocidadeCursorControle = Mathf.Max(1f, _velocidadeCursorControle);
    }
}
