using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class AssistenciaMiraUI : MonoBehaviour
{
    private Toggle _toggle;
    private void Awake() { _toggle = GetComponent<Toggle>(); }
    private void OnEnable()
    {
        _toggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(Mira.ChaveAssistenciaControle, 1) == 1);
        _toggle.onValueChanged.AddListener(Aplicar);
    }
    private void OnDisable() { _toggle.onValueChanged.RemoveListener(Aplicar); }
    private void Aplicar(bool ativada)
    {
        PlayerPrefs.SetInt(Mira.ChaveAssistenciaControle, ativada ? 1 : 0);
        PlayerPrefs.Save();
        Mira mira = FindFirstObjectByType<Mira>(FindObjectsInactive.Include);
        if (mira != null) mira.DefinirAssistencia(ativada);
    }
}
