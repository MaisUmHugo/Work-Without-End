using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string mensagem;

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipUI.instance.Mostrar(mensagem);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipUI.instance.Esconder();
    }
}
