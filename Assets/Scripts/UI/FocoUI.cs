using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class FocoUI
{
    public static void SelecionarPrimeiroBotao(GameObject painel)
    {
        if (EventSystem.current == null || painel == null || !painel.activeInHierarchy) return;

        if (EventSystem.current.currentInputModule is InputSystemUIInputModule modulo)
            modulo.deselectOnBackgroundClick = false;

        Canvas.ForceUpdateCanvases();
        ConfigurarNavegacao(painel);

        foreach (Button botao in painel.GetComponentsInChildren<Button>(true))
        {
            if (!botao.isActiveAndEnabled || !botao.IsInteractable()) continue;
            EventSystem.current.SetSelectedGameObject(botao.gameObject);
            return;
        }
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
