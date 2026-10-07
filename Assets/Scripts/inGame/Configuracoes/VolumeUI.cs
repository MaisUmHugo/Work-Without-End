using UnityEngine;
using UnityEngine.UI;

public class VolumeUI : MonoBehaviour
{
    public string mixerParameter;
    public Slider slider;

    private void Awake()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        // Detecta automaticamente o parâmetro pelo nome do objeto, se não estiver definido
        if (string.IsNullOrEmpty(mixerParameter))
            mixerParameter = DetectarParametroAutomatico();

        float valorInicial = PlayerPrefs.GetFloat(mixerParameter, 1f);
        slider.value = valorInicial;
        AplicarVolume(valorInicial);

        slider.onValueChanged.AddListener(AplicarVolume);
    }

    private string DetectarParametroAutomatico()
    {
        string nome = gameObject.name.ToLower();

        if (nome.Contains("master")) return "masterParameter";
        if (nome.Contains("bgm")) return "bgmParameter";
        if (nome.Contains("sfx")) return "sfxParameter";

        Debug.LogWarning($"[VolumeUI] Não foi possível detectar parâmetro para {gameObject.name}. Usando masterParameter por padrão.");
        return "masterParameter";
    }

    private void AplicarVolume(float valor)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.AjustarVolume(mixerParameter, valor);
        else
            Debug.LogWarning($"[VolumeUI] AudioManager.instance não encontrado para {mixerParameter}");

        PlayerPrefs.SetFloat(mixerParameter, valor);
        PlayerPrefs.Save();
    }
}
