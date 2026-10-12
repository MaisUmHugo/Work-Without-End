using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Slider))]
public class ModoMiraControleUI : MonoBehaviour, IPointerClickHandler, ISubmitHandler, ICancelHandler
{
    [SerializeField] private TMP_Text _textoModo;
    [SerializeField] private TMP_Text _textoDescricao;
    private Slider _slider;

    private void Awake()
    {
        _slider = GetComponent<Slider>();
        _slider.minValue = (int)ModoMiraControle.Livre;
        _slider.maxValue = (int)ModoMiraControle.Cursor;
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
        int quantidade = (int)(_slider.maxValue - _slider.minValue) + 1;
        int indice = (int)(_slider.value - _slider.minValue);
        _slider.value = _slider.minValue + (indice + passo + quantidade) % quantidade;
    }

    public void OnSubmit(BaseEventData eventData)
    {
        SairDoSeletor(eventData);
    }

    public void OnCancel(BaseEventData eventData)
    {
        SairDoSeletor(eventData);
    }

    private void SairDoSeletor(BaseEventData eventData)
    {
        if (_slider == null || !_slider.IsInteractable() || EventSystem.current == null) return;

        Navigation navegacao = _slider.navigation;
        Selectable destino = navegacao.selectOnDown;
        if (destino == null || !destino.isActiveAndEnabled || !destino.IsInteractable())
            destino = navegacao.selectOnUp;
        if (destino == null || !destino.isActiveAndEnabled || !destino.IsInteractable()) return;

        EventSystem.current.SetSelectedGameObject(destino.gameObject);
        eventData.Use();
    }

    private void Aplicar(float valor)
    {
        var modo = Mira.NormalizarModoControle((ModoMiraControle)Mathf.RoundToInt(valor));
        PlayerPrefs.SetInt(Mira.ChaveModoMiraControle, (int)modo);
        PlayerPrefs.Save();
        Mira mira = FindFirstObjectByType<Mira>(FindObjectsInactive.Include);
        if (mira != null) mira.DefinirModoControle(modo);
        AtualizarTexto(modo);
    }

    private void AtualizarTexto(ModoMiraControle modo)
    {
        string nome = modo == ModoMiraControle.Livre ? "LIVRE" : "CURSOR";
        string descricao = modo == ModoMiraControle.Livre ? "Aponte o analógico para mirar em qualquer direção."
            : "Mova a mira pela tela com o analógico, como um mouse.";
        if (_textoModo != null) _textoModo.text = $"< {nome} >";
        if (_textoDescricao != null) _textoDescricao.text = descricao;
        TooltipTrigger ajuda = GetComponent<TooltipTrigger>();
        if (ajuda != null)
        {
            ajuda.mensagem = descricao;
            TooltipUI tooltip = GetComponentInChildren<TooltipUI>(true);
            if (tooltip != null && tooltip.painel.gameObject.activeInHierarchy) tooltip.Mostrar(ajuda.mensagem);
        }
    }
}
