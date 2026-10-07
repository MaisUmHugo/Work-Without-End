using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class FocoUI
{
    private static GameObject _painelAtivo;
    private static readonly Dictionary<GameObject, Selectable> _ultimosElementos = new Dictionary<GameObject, Selectable>();
    private static int _ultimoFrameAtualizado = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void LimparMemoria()
    {
        _painelAtivo = null;
        _ultimosElementos.Clear();
        _ultimoFrameAtualizado = -1;
    }

    public static void SelecionarPrimeiroBotao(GameObject painel)
    {
        if (EventSystem.current == null || painel == null || !painel.activeInHierarchy) return;

        if (_painelAtivo == null) _ultimosElementos.Clear();
        _painelAtivo = painel;

        if (EventSystem.current.currentInputModule is InputSystemUIInputModule modulo)
            modulo.deselectOnBackgroundClick = false;

        Canvas.ForceUpdateCanvases();
        ConfigurarNavegacao(painel);

        if (_ultimosElementos.TryGetValue(painel, out Selectable ultimo) && SelecaoValida(ultimo))
        {
            EventSystem.current.SetSelectedGameObject(ultimo.gameObject);
            return;
        }

        foreach (Selectable elemento in painel.GetComponentsInChildren<Selectable>(true))
        {
            if (!SelecaoValida(elemento)) continue;
            EventSystem.current.SetSelectedGameObject(elemento.gameObject);
            return;
        }
    }

    public static void RegistrarSelecao(Selectable elemento)
    {
        if (SelecaoValida(elemento))
            _ultimosElementos[_painelAtivo] = elemento;
    }

    public static void AtualizarSelecaoControle()
    {
        if (_ultimoFrameAtualizado == Time.frameCount) return;
        _ultimoFrameAtualizado = Time.frameCount;
        if (_painelAtivo == null || !_painelAtivo.activeInHierarchy || EventSystem.current == null) return;

        GameObject selecionado = EventSystem.current.currentSelectedGameObject;
        Selectable elemento = selecionado != null ? selecionado.GetComponent<Selectable>() : null;
        if (SelecaoValida(elemento))
        {
            RegistrarSelecao(elemento);
            return;
        }

        Gamepad controle = Gamepad.current;
        if (controle == null) return;
        bool navegando = controle.leftStick.ReadValue().sqrMagnitude > 0.25f
            || controle.rightStick.ReadValue().sqrMagnitude > 0.25f
            || controle.dpad.ReadValue().sqrMagnitude > 0.25f;
        if (navegando) SelecionarPrimeiroBotao(_painelAtivo);
    }

    private static bool SelecaoValida(Selectable elemento)
    {
        return _painelAtivo != null && elemento != null && (elemento is Button || elemento is Slider || elemento is Toggle)
            && elemento.isActiveAndEnabled && elemento.IsInteractable()
            && elemento.transform.IsChildOf(_painelAtivo.transform);
    }

    private static void ConfigurarNavegacao(GameObject painel)
    {
        Selectable[] elementos = painel.GetComponentsInChildren<Selectable>();
        foreach (Selectable elemento in elementos)
        {
            if (!elemento.isActiveAndEnabled || !elemento.IsInteractable() || elemento is Scrollbar) continue;

            Navigation navegacao = new Navigation { mode = Navigation.Mode.Explicit };
            bool horizontal = elemento is Slider sliderHorizontal
                && (sliderHorizontal.direction == Slider.Direction.LeftToRight || sliderHorizontal.direction == Slider.Direction.RightToLeft);
            bool vertical = elemento is Slider sliderVertical
                && (sliderVertical.direction == Slider.Direction.BottomToTop || sliderVertical.direction == Slider.Direction.TopToBottom);

            // O eixo do slider ajusta o valor; o outro eixo troca o foco.
            if (!horizontal)
            {
                navegacao.selectOnLeft = EncontrarVizinho(elemento, elementos, Vector2.left, painel.transform);
                navegacao.selectOnRight = EncontrarVizinho(elemento, elementos, Vector2.right, painel.transform);
            }
            if (!vertical)
            {
                navegacao.selectOnUp = EncontrarVizinho(elemento, elementos, Vector2.up, painel.transform);
                navegacao.selectOnDown = EncontrarVizinho(elemento, elementos, Vector2.down, painel.transform);
            }
            elemento.navigation = navegacao;
        }
    }

    private static Selectable EncontrarVizinho(Selectable origem, Selectable[] elementos, Vector2 direcao, Transform painel)
    {
        RectTransform retanguloOrigem = (RectTransform)origem.transform;
        Vector2 centroOrigem = painel.InverseTransformPoint(retanguloOrigem.TransformPoint(retanguloOrigem.rect.center));
        Selectable melhor = null;
        float melhorPontuacao = 0f;

        foreach (Selectable candidato in elementos)
        {
            if (candidato == origem || !candidato.isActiveAndEnabled || !candidato.IsInteractable() || candidato is Scrollbar) continue;

            RectTransform retangulo = (RectTransform)candidato.transform;
            Vector2 deslocamento = (Vector2)painel.InverseTransformPoint(retangulo.TransformPoint(retangulo.rect.center)) - centroOrigem;
            float alinhamento = Vector2.Dot(direcao, deslocamento);
            if (alinhamento <= 0.01f) continue;

            float pontuacao = alinhamento / deslocamento.sqrMagnitude;
            if (pontuacao <= melhorPontuacao) continue;
            melhorPontuacao = pontuacao;
            melhor = candidato;
        }
        return melhor;
    }
}
