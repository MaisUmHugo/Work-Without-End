using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI instance;

    public RectTransform painel;
    public TextMeshProUGUI texto;
    private Vector2 _posicaoOriginal;
    private bool _posicaoRegistrada;
    private readonly Vector3[] _cantos = new Vector3[4];

    private void Awake()
    {
        instance = this;
        painel.gameObject.SetActive(false);
    }

    public void Mostrar(string msg)
    {
        if (!_posicaoRegistrada)
        {
            _posicaoOriginal = painel.anchoredPosition;
            _posicaoRegistrada = true;
        }
        painel.anchoredPosition = _posicaoOriginal;
        texto.text = msg;
        painel.gameObject.SetActive(true);
        ManterDentroDaRolagem();
    }

    public void Esconder()
    {
        painel.gameObject.SetActive(false);
        if (_posicaoRegistrada) painel.anchoredPosition = _posicaoOriginal;
    }

    private void ManterDentroDaRolagem()
    {
        ScrollRect rolagem = GetComponentInParent<ScrollRect>();
        if (rolagem == null || rolagem.viewport == null) return;
        Canvas.ForceUpdateCanvases();
        painel.GetWorldCorners(_cantos);
        Vector2 minimo = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 maximo = new Vector2(float.MinValue, float.MinValue);
        foreach (Vector3 canto in _cantos)
        {
            Vector2 ponto = rolagem.viewport.InverseTransformPoint(canto);
            minimo = Vector2.Min(minimo, ponto);
            maximo = Vector2.Max(maximo, ponto);
        }
        Rect janela = rolagem.viewport.rect;
        Vector2 ajuste = new Vector2(
            minimo.x < janela.xMin ? janela.xMin - minimo.x : maximo.x > janela.xMax ? janela.xMax - maximo.x : 0f,
            minimo.y < janela.yMin ? janela.yMin - minimo.y : maximo.y > janela.yMax ? janela.yMax - maximo.y : 0f);
        painel.localPosition += painel.parent.InverseTransformVector(rolagem.viewport.TransformVector(ajuste));
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
