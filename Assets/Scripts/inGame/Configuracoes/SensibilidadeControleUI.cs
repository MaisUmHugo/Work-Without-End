using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SensibilidadeControleUI : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    [SerializeField] private TMP_Text _textoValor;
    private Mira _mira;

    private void Awake()
    {
        if (_slider == null) _slider = GetComponent<Slider>();
        _mira = FindFirstObjectByType<Mira>(FindObjectsInactive.Include);
        _slider.minValue = 0f;
        _slider.maxValue = 10f;
        _slider.wholeNumbers = false;
    }

    private void OnEnable()
    {
        float valor = Mathf.Clamp(PlayerPrefs.GetFloat(Mira.ChaveSensibilidadeControle,
            Mira.SensibilidadeControlePadrao), 0f, 10f);
        _slider.SetValueWithoutNotify(valor);
        _mira?.DefinirSensibilidadeControle(valor);
        AtualizarTexto(valor);
        _slider.onValueChanged.AddListener(AplicarSensibilidade);
    }

    private void OnDisable()
    {
        _slider.onValueChanged.RemoveListener(AplicarSensibilidade);
    }

    private void AplicarSensibilidade(float valor)
    {
        valor = Mathf.Clamp(valor, 0f, 10f);
        if (_mira == null) _mira = FindFirstObjectByType<Mira>(FindObjectsInactive.Include);
        _mira?.DefinirSensibilidadeControle(valor);
        PlayerPrefs.SetFloat(Mira.ChaveSensibilidadeControle, valor);
        PlayerPrefs.Save();
        AtualizarTexto(valor);
    }

    private void AtualizarTexto(float valor)
    {
        if (_textoValor != null)
            _textoValor.text = $"SENSIBILIDADE CONTROLE: {valor:0.#}";
    }
}
