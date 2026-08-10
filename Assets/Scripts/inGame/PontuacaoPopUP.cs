using UnityEngine;
using TMPro;
using System.Collections;

public class PontuacaoPopup : MonoBehaviour
{
    public TMP_Text textoPontuacao;
    public float duracao = 1.5f;
    public float distanciaY = 1f;
    public float fadeSpeed = 1.5f;

    private Color corInicial;

    private void Awake()
    {
        if (textoPontuacao != null)
        {
            corInicial = textoPontuacao.color;
            textoPontuacao.alpha = 0;
        }
    }

    public void MostrarPontuacao(int valor)
    {
        if (textoPontuacao == null) return;

        textoPontuacao.text = $"+{valor}";
        StopAllCoroutines();
        StartCoroutine(AnimarPopup());
    }

    private IEnumerator AnimarPopup()
    {
        float tempo = 0f;
        Vector3 posInicial = textoPontuacao.transform.localPosition;
        Vector3 posFinal = posInicial + Vector3.up * distanciaY;

        // fade in e subida
        while (tempo < duracao)
        {
            tempo += Time.deltaTime;
            float t = tempo / duracao;

            textoPontuacao.alpha = Mathf.Lerp(1, 0, t);
            textoPontuacao.transform.localPosition = Vector3.Lerp(posInicial, posFinal, t);

            yield return null;
        }

        textoPontuacao.alpha = 0;
        textoPontuacao.transform.localPosition = posInicial;
    }
}
