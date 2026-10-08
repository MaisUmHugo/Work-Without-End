using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class MiraOitoDirecoesUI : MonoBehaviour
{
    private Toggle _toggle;

    private void Awake()
    {
        _toggle = GetComponent<Toggle>();
    }

    private void OnEnable()
    {
        _toggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(Mira.ChaveMiraOitoDirecoes, 1) == 1);
        _toggle.onValueChanged.AddListener(Aplicar);
    }

    private void OnDisable()
    {
        _toggle.onValueChanged.RemoveListener(Aplicar);
    }

    private void Aplicar(bool ativada)
    {
        PlayerPrefs.SetInt(Mira.ChaveMiraOitoDirecoes, ativada ? 1 : 0);
        PlayerPrefs.Save();
        Mira mira = FindFirstObjectByType<Mira>(FindObjectsInactive.Include);
        if (mira != null) mira.DefinirMiraOitoDirecoes(ativada);
    }
}
