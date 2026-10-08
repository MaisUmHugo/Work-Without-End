using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Slider))]
public class ModoMiraControleUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TMP_Text _textoModo;
    [SerializeField] private TMP_Text _textoDescricao;
    private Slider _slider;

    private void Awake()
    {
        _slider = GetComponent<Slider>();
        _slider.minValue = 0f;
        _slider.maxValue = 2f;
        _slider.wholeNumbers = true;
    }

    private void OnEnable()
    {
        _slider.SetValueWithoutNotify((int)Mira.ObterModoSalvo());
        AtualizarTexto((ModoMiraControle)(int)_slider.value);
        _slider.onValueChanged.AddListener(Aplicar);
    }

    private void OnDisable() { _slider.onValueChanged.RemoveListener(Aplicar); }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !_slider.IsInteractable()) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,
            eventData.position, eventData.pressEventCamera, out Vector2 posicao);
        int passo = posicao.x < 0f ? -1 : 1;
        _slider.value = ((int)_slider.value + passo + 3) % 3;
    }

    private void Aplicar(float valor)
    {
        var modo = (ModoMiraControle)Mathf.Clamp(Mathf.RoundToInt(valor), 0, 2);
        PlayerPrefs.SetInt(Mira.ChaveModoMiraControle, (int)modo);
        PlayerPrefs.Save();
        Mira mira = FindFirstObjectByType<Mira>(FindObjectsInactive.Include);
        if (mira != null) mira.DefinirModoControle(modo);
        AtualizarTexto(modo);
    }

    private void AtualizarTexto(ModoMiraControle modo)
    {
        string nome = modo == ModoMiraControle.DirecoesFixas ? "DIREÇÕES FIXAS"
            : modo == ModoMiraControle.Livre ? "LIVRE" : "CURSOR";
        string descricao = modo == ModoMiraControle.DirecoesFixas ? "Mais diagonais para mirar à frente."
            : modo == ModoMiraControle.Livre ? "Aponte o analógico para orientar a mira." : "Mova a mira pela tela como um mouse.";
        if (_textoModo != null) _textoModo.text = $"< {nome} >";
        if (_textoDescricao != null) _textoDescricao.text = descricao;
        TooltipTrigger ajuda = GetComponent<TooltipTrigger>();
        if (ajuda != null)
        {
            ajuda.mensagem = "Esquerda/direita ou clique troca o modo. " + descricao + " O mouse continua com mira livre.";
            TooltipUI tooltip = GetComponentInChildren<TooltipUI>(true);
            if (tooltip != null && tooltip.painel.gameObject.activeInHierarchy) tooltip.Mostrar(ajuda.mensagem);
        }
    }
}
