using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class CheatAtaquesNecromante : MonoBehaviour
{
    [Header("Atalhos de teste")]
    [Tooltip("Disponivel somente no Editor e em Development Build.")]
    [SerializeField] private bool _ativarCheats = true;
    [SerializeField] private bool _exigirShift = true;
    [SerializeField] private Key _teclaProjetil = Key.Digit1;
    [SerializeField] private Key _teclaTiroCarregado = Key.Digit2;
    [SerializeField] private Key _teclaSequenciaRapida = Key.Digit3;
    [SerializeField] private Key _teclaZumbiSombrio = Key.Digit4;
    [Header("Referencias")]
    [SerializeField] private ComportamentoBossNecromante _comportamento;
    [SerializeField] private AtaqueProjetilNecromante _ataqueProjetil;
    [SerializeField] private AtaqueTiroCarregadoNecromante _tiroCarregado;
    [SerializeField] private AtaqueSequenciaRapidaNecromante _sequenciaRapida;
    [SerializeField] private GerenciadorInvocadosNecromante _gerenciadorInvocados;

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Keyboard teclado = Keyboard.current;
        if (!_ativarCheats || teclado == null || BloqueioGameplay.Bloqueado)
            return;

        bool shiftPressionado = teclado.leftShiftKey.isPressed || teclado.rightShiftKey.isPressed;
        if (_exigirShift && !shiftPressionado)
            return;

        if (teclado[_teclaProjetil].wasPressedThisFrame)
            TentarIniciar(_ataqueProjetil, "projetil comum");
        else if (teclado[_teclaTiroCarregado].wasPressedThisFrame)
            TentarIniciar(_tiroCarregado, "tiro carregado");
        else if (teclado[_teclaSequenciaRapida].wasPressedThisFrame)
            TentarIniciar(_sequenciaRapida, "sequencia rapida");
        else if (teclado[_teclaZumbiSombrio].wasPressedThisFrame)
            TentarInvocarZumbiSombrio();
#endif
    }

    private void TentarIniciar(AtaqueBossBase ataque, string nomeAtaque)
    {
        if (_comportamento != null && _comportamento.TentarIniciarAtaqueTeste(ataque))
        {
            Debug.Log($"[CHEAT] Necromante iniciou {nomeAtaque}.", this);
            return;
        }

        Debug.LogWarning($"[CHEAT] Nao foi possivel iniciar {nomeAtaque}.", this);
    }

    private void TentarInvocarZumbiSombrio()
    {
        if (_gerenciadorInvocados != null
            && _gerenciadorInvocados.TentarInvocarZumbiSombrioAleatorio())
        {
            Debug.Log("[CHEAT] Necromante invocou um Zumbi Sombrio.", this);
            return;
        }

        Debug.LogWarning("[CHEAT] Nao foi possivel invocar o Zumbi Sombrio.", this);
    }
}
