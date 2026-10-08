using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public string mensagem;
    private TooltipUI _tooltip;

    private TooltipUI ObterTooltip()
    {
        if (_tooltip != null) return _tooltip;
        Transform raiz = transform;
        while (raiz != null)
        {
            _tooltip = raiz.GetComponentInChildren<TooltipUI>(true);
            if (_tooltip != null) return _tooltip;
            raiz = raiz.parent;
        }
        return TooltipUI.instance;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ObterTooltip()?.Mostrar(mensagem);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ObterTooltip()?.Esconder();
    }

    public void OnSelect(BaseEventData eventData) { ObterTooltip()?.Mostrar(mensagem); }
    public void OnDeselect(BaseEventData eventData) { ObterTooltip()?.Esconder(); }
    private void OnDisable() { if (_tooltip != null) _tooltip.Esconder(); }
}
