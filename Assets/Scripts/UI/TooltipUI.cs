using UnityEngine;
using TMPro;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI instance;

    public RectTransform painel;
    public TextMeshProUGUI texto;

    private void Awake()
    {
        instance = this;
        painel.gameObject.SetActive(false);
    }

    public void Mostrar(string msg)
    {
        texto.text = msg;
        painel.gameObject.SetActive(true);
    }

    public void Esconder()
    {
        painel.gameObject.SetActive(false);
    }

   /*private void Update()
    {
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            painel.parent as RectTransform,
            Input.mousePosition,
            null,
            out pos
        );
        painel.anchoredPosition = pos + new Vector2(15, -15);
    } */
}
