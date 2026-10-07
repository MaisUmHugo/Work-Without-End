using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ParallaxDinamicoUI : MonoBehaviour
{
    private Toggle _toggle;

    private void Awake()
    {
        _toggle = GetComponent<Toggle>();
    }

    private void OnEnable()
    {
        _toggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt("parallaxAumentar", 1) == 1);
        _toggle.onValueChanged.AddListener(AplicarParallax);
    }

    private void OnDisable()
    {
        _toggle.onValueChanged.RemoveListener(AplicarParallax);
    }

    private void AplicarParallax(bool ativado)
    {
        PlayerPrefs.SetInt("parallaxAumentar", ativado ? 1 : 0);
        if (HordaManager.instance != null)
            HordaManager.instance.DefinirAumentoParallax(ativado);
        PlayerPrefs.Save();
    }
}
