using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Mira : MonoBehaviour
{
    private Camera cam;
    private INPUTS inputs;
    private SpriteRenderer sr;
    private Color corNormal;

    [Header("Mira no controle")]
    [SerializeField] private Transform _origemLancamento;
    [SerializeField, Min(0.1f)] private float _distanciaMiraControle = 12f;
    [SerializeField, Range(0f, 0.95f)] private float _zonaMortaControle = 0.2f;
    [SerializeField, Min(1f), Tooltip("Velocidade de ajuste da direcao, em graus por segundo.")]
    private float _velocidadeRotacaoControle = 720f;

    private InputAction _mirarControle;
    private bool _usandoControle;
    private Vector2 _direcaoControle = Vector2.right;
    private Vector2 _direcaoDesejada = Vector2.right;

    [Header("Cooldown Visual")]
    public Image cooldownUI;
    [HideInInspector] public float cooldownProgresso;
    [HideInInspector] public bool emCooldown;

    private void Awake()
    {
        cam = Camera.main;
        inputs = new INPUTS();
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

    public Vector3 ObterPosicaoAlvo(Transform origem)
    {
        if (_usandoControle && origem != null)
            return origem.position + (Vector3)(_direcaoControle * _distanciaMiraControle);

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
        _direcaoDesejada = direcao.normalized;
        if (!_usandoControle) _direcaoControle = _direcaoDesejada;
        _usandoControle = true;
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
                    _direcaoDesejada = direcao.normalized;

                float anguloAtual = Mathf.Atan2(_direcaoControle.y, _direcaoControle.x) * Mathf.Rad2Deg;
                float anguloDesejado = Mathf.Atan2(_direcaoDesejada.y, _direcaoDesejada.x) * Mathf.Rad2Deg;
                float angulo = Mathf.MoveTowardsAngle(anguloAtual, anguloDesejado,
                    _velocidadeRotacaoControle * Time.deltaTime) * Mathf.Deg2Rad;
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
        _velocidadeRotacaoControle = Mathf.Max(1f, _velocidadeRotacaoControle);
    }
}
