using UnityEngine;
using System.Collections;

public class EntregavelPisca : MonoBehaviour
{
    private SpriteRenderer sr;
    private Coroutine rotina;

    [Header("Configurações - Piscar Ativo (pode receber)")]
    private float intervaloAtivo = 0.3f;
    private int quantidadePiscadasAtivo = 3;
    private Color corPiscarAtivo = new Color(1f, 0.4f, 0.5f, 0.6f);

    [Header("Configurações - Piscar Recebendo")]
    private float intervaloRecebendo = 0.3f;
    private int quantidadePiscadasRecebendo = 3;
    private Color corPiscarRecebendo = new Color(0.7f, 0.7f, 0.7f, 0.4f);

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        Debug.Log("SpriteRenderer encontrado para piscar: " + sr);
        Debug.Log("SpriteRenderer usado no piscar: " + sr.gameObject.name);
    }

    public void PiscarAtivo()
    {
        if (rotina != null) StopCoroutine(rotina);
        rotina = StartCoroutine(Piscar(corPiscarAtivo, intervaloAtivo, quantidadePiscadasAtivo));
    }

    public void PiscarRecebendo()
    {
        if (rotina != null) StopCoroutine(rotina);
        rotina = StartCoroutine(Piscar(corPiscarRecebendo, intervaloRecebendo, quantidadePiscadasRecebendo));
    }

    private IEnumerator Piscar(Color corPiscar, float intervalo, int quantidade)
    {
        if (sr == null) yield break;
        Color corOriginal = sr.material.color;

        for (int i = 0; i < quantidade; i++)
        {
            sr.material.color = corPiscar;
            yield return new WaitForSeconds(intervalo);

            sr.material.color = corOriginal;
            yield return new WaitForSeconds(intervalo);
        }

        // garante volta final
        sr.material.color = corOriginal;
        rotina = null;
    }

    public void PararPiscar()
    {
        if (rotina != null)
        {
            StopCoroutine(rotina);
            rotina = null;
        }

        if (sr != null)
            sr.material.color = Color.white;
    }
}
