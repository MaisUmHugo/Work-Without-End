using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ComboManager))]
public class RecuperacaoVidaPorCombo : MonoBehaviour
{
    [SerializeField] private ComboManager _combo;
    [SerializeField, Min(1)] private int _entregasPorVida = 20;

    private int _ultimoMarcoProcessado;

    private void OnEnable()
    {
        if (_combo == null)
            _combo = GetComponent<ComboManager>();

        _ultimoMarcoProcessado = _combo.comboAtual / Mathf.Max(1, _entregasPorVida);
        _combo.OnComboMudou += AoAlterarCombo;
    }

    private void OnDisable()
    {
        if (_combo != null)
            _combo.OnComboMudou -= AoAlterarCombo;
    }

    private void AoAlterarCombo(int comboAtual)
    {
        int marcoAtual = Mathf.Max(0, comboAtual) / Mathf.Max(1, _entregasPorVida);
        if (marcoAtual < _ultimoMarcoProcessado)
        {
            _ultimoMarcoProcessado = marcoAtual;
            return;
        }

        if (marcoAtual == _ultimoMarcoProcessado) return;

        // O marco conta mesmo com vida cheia; a cura nao fica acumulada para depois.
        _ultimoMarcoProcessado = marcoAtual;
        VidaManager vida = VidaManager.instance;
        if (vida == null || vida.vidasAtuais <= 0
            || vida.vidasAtuais >= Mathf.Max(1, vida.vidasIniciais))
            return;

        vida.GanharVida();
    }

    private void OnValidate()
    {
        _entregasPorVida = Mathf.Max(1, _entregasPorVida);
    }
}
