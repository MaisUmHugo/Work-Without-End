using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FeedbackSelecaoUI : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerMoveHandler
{
    [Header("Destaque da selecao")]
    [SerializeField, Range(1f, 1.2f)] private float _escalaSelecionado = 1.06f;
    [SerializeField, Range(0f, 5f)] private float _anguloBalanco = 1.5f;
    [SerializeField, Min(0f)] private float _frequenciaBalanco = 2f;
    [SerializeField, Min(0.01f)] private float _tempoTransicao = 0.12f;

    private Selectable _elemento;
    private RectTransform _retangulo;
    private Vector3 _escalaOriginal;
    private Quaternion _rotacaoOriginal;
    private bool _selecionado;
    private float _destaque;
    private float _tempoBalanco;
    private readonly Vector3[] _cantos = new Vector3[4];

    private void Awake()
    {
        _elemento = GetComponent<Selectable>();
        _retangulo = (RectTransform)transform;
        _escalaOriginal = _retangulo.localScale;
        _rotacaoOriginal = _retangulo.localRotation;
    }

    private void OnEnable()
    {
        _selecionado = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
    }

    private void Update()
    {
        FocoUI.AtualizarSelecaoControle();
        if (!(_elemento is Button) && !(_elemento is Slider) && !(_elemento is Toggle)) return;

        bool destacar = _selecionado && _elemento.IsInteractable();
        _destaque = Mathf.MoveTowards(_destaque, destacar ? 1f : 0f, Time.unscaledDeltaTime / _tempoTransicao);
        _tempoBalanco += Time.unscaledDeltaTime;
        _retangulo.localScale = _escalaOriginal * Mathf.Lerp(1f, _escalaSelecionado, _destaque);
        float angulo = _elemento is Button
            ? Mathf.Sin(_tempoBalanco * _frequenciaBalanco * Mathf.PI * 2f) * _anguloBalanco * _destaque : 0f;
        _retangulo.localRotation = _rotacaoOriginal * Quaternion.Euler(0f, 0f, angulo);
    }

    public void OnSelect(BaseEventData eventData)
    {
        _selecionado = true;
        FocoUI.RegistrarSelecao(_elemento);
        _tempoBalanco = 0f;
        MostrarNaRolagem();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _selecionado = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SelecionarComMouse();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        // Um cursor parado nao deve disputar a selecao com o controle.
        if (eventData.delta.sqrMagnitude > 0.01f)
            SelecionarComMouse();
    }

    private void SelecionarComMouse()
    {
        EventSystem sistema = EventSystem.current;
        if (_elemento == null || !_elemento.isActiveAndEnabled || !_elemento.IsInteractable()
            || sistema == null || sistema.alreadySelecting || sistema.currentSelectedGameObject == gameObject) return;

        sistema.SetSelectedGameObject(gameObject);
    }

    private void MostrarNaRolagem()
    {
        ScrollRect rolagem = GetComponentInParent<ScrollRect>();
        if (rolagem == null || !rolagem.vertical || rolagem.content == null || rolagem.viewport == null
            || !_retangulo.IsChildOf(rolagem.content)) return;

        _retangulo.GetWorldCorners(_cantos);
        float inferior = float.MaxValue;
        float superior = float.MinValue;
        foreach (Vector3 canto in _cantos)
        {
            float altura = rolagem.viewport.InverseTransformPoint(canto).y;
            inferior = Mathf.Min(inferior, altura);
            superior = Mathf.Max(superior, altura);
        }

        // A legenda acompanha o slider quando o controle rola ate ele.
        if (_elemento is Slider)
        {
            foreach (TMP_Text texto in GetComponentsInChildren<TMP_Text>())
            {
                texto.rectTransform.GetWorldCorners(_cantos);
                foreach (Vector3 canto in _cantos)
                {
                    float altura = rolagem.viewport.InverseTransformPoint(canto).y;
                    inferior = Mathf.Min(inferior, altura);
                    superior = Mathf.Max(superior, altura);
                }
            }
        }

        Rect janela = rolagem.viewport.rect;
        // Reserva espaco para o aumento visual sem mudar o tamanho do Content.
        if (_elemento is Button || _elemento is Slider || _elemento is Toggle)
        {
            float margem = (superior - inferior) * (_escalaSelecionado - 1f) * 0.5f + 4f;
            superior += margem;
            inferior -= margem;
        }
        float deslocamento = superior > janela.yMax ? janela.yMax - superior
            : inferior < janela.yMin ? janela.yMin - inferior : 0f;
        if (Mathf.Approximately(deslocamento, 0f)) return;

        rolagem.StopMovement();
        Vector3 ajuste = rolagem.viewport.TransformVector(new Vector3(0f, deslocamento, 0f));
        rolagem.content.localPosition += rolagem.content.parent.InverseTransformVector(ajuste);
    }

    private void OnDisable()
    {
        _selecionado = false;
        _destaque = 0f;
        _tempoBalanco = 0f;
        if (_retangulo == null || (!(_elemento is Button) && !(_elemento is Slider) && !(_elemento is Toggle))) return;
        _retangulo.localScale = _escalaOriginal;
        _retangulo.localRotation = _rotacaoOriginal;
    }
}
