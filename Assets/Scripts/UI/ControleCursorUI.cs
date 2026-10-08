using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class ControleCursorUI : MonoBehaviour
{
    [SerializeField] private bool _permitirPonteiroControle;
    [SerializeField, Min(1f)] private float _velocidadePonteiro = 900f;
    [SerializeField, Range(0f, 0.95f)] private float _zonaMorta = 0.25f;
    private Mira _mira;
    private Mouse _mouseClicado;

    private void Start()
    {
        _mira = FindFirstObjectByType<Mira>(FindObjectsInactive.Include);
    }

    private void Update()
    {
        bool mostrarCursor = _mira == null || !_mira.isActiveAndEnabled || BloqueioGameplay.BloqueadoSemTransicao;
        Cursor.visible = mostrarCursor;
        Cursor.lockState = CursorLockMode.None;
        if (!_permitirPonteiroControle || !mostrarCursor || !Application.isFocused)
        {
            LiberarClique();
            return;
        }

        Gamepad controle = Gamepad.current;
        Mouse mouse = Mouse.current;
        if (controle == null || mouse == null) { LiberarClique(); return; }
        Vector2 direcao = controle.rightStick.ReadValue();
        if (direcao.magnitude > _zonaMorta)
            MoverPonteiro(mouse, direcao, Time.unscaledDeltaTime);

        // A/X continua confirmando a selecao; RT/R2 clica onde esta o ponteiro.
        bool clicar = controle.rightTrigger.isPressed;
        if (clicar && _mouseClicado == null)
        {
            _mouseClicado = mouse;
            DefinirClique(mouse, true);
        }
        else if (!clicar) LiberarClique();
    }

    private void MoverPonteiro(Mouse mouse, Vector2 direcao, float deltaTime)
    {
        float intensidade = Mathf.InverseLerp(_zonaMorta, 1f, direcao.magnitude);
        Vector2 anterior = mouse.position.ReadValue();
        Vector2 posicao = anterior + direcao.normalized * intensidade
            * _velocidadePonteiro * (Screen.height / 1080f) * deltaTime;
        posicao.x = Mathf.Clamp(posicao.x, 0f, Screen.width - 1f);
        posicao.y = Mathf.Clamp(posicao.y, 0f, Screen.height - 1f);
        mouse.WarpCursorPosition(posicao);
        InputState.Change(mouse.position, posicao);
        InputState.Change(mouse.delta, posicao - anterior);
    }

    private void LiberarClique()
    {
        if (_mouseClicado != null && _mouseClicado.added)
            DefinirClique(_mouseClicado, false);
        _mouseClicado = null;
    }

    private void DefinirClique(Mouse mouse, bool pressionado)
    {
        mouse.CopyState<MouseState>(out var estado);
        estado.WithButton(MouseButton.Left, pressionado);
        InputState.Change(mouse, estado);
    }

    private void OnDisable()
    {
        LiberarClique();
        Cursor.visible = true;
    }
}
