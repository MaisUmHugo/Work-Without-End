using UnityEngine;
using System.Collections;

public class EntregavelPisca : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRendererAlvo;

    private SpriteRenderer sr;
    private Material materialAlvo;
    private Coroutine rotina;
    private Color corOriginal;

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
        sr = spriteRendererAlvo;

        if (sr == null)
            sr = GetComponent<SpriteRenderer>();

        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>(true);

        if (sr == null)
        {
            Debug.LogWarning($"{name}: nenhum SpriteRenderer foi encontrado para o efeito de piscar.", this);
            return;
        }

        materialAlvo = sr.material;
        if (materialAlvo == null)
        {
            Debug.LogWarning(name + ": o SpriteRenderer não possui material para o efeito de piscar.", this);
            return;
        }

        corOriginal = materialAlvo.color;
    }
    public void PiscarAtivo()
    {
        IniciarPiscar(corPiscarAtivo, intervaloAtivo, quantidadePiscadasAtivo);
    }

    public void PiscarRecebendo()
    {
        IniciarPiscar(corPiscarRecebendo, intervaloRecebendo, quantidadePiscadasRecebendo);
    }

    private void IniciarPiscar(Color corPiscar, float intervalo, int quantidade)
    {
        if (materialAlvo == null) return;

        if (rotina != null)
            StopCoroutine(rotina);

        rotina = StartCoroutine(Piscar(corPiscar, intervalo, quantidade));
    }

    private IEnumerator Piscar(Color corPiscar, float intervalo, int quantidade)
    {
        for (int i = 0; i < quantidade; i++)
        {
            materialAlvo.color = corPiscar;
            yield return new WaitForSeconds(intervalo);

            materialAlvo.color = corOriginal;
            yield return new WaitForSeconds(intervalo);
        }

        materialAlvo.color = corOriginal;
        rotina = null;
    }

    public void PararPiscar()
    {
        if (rotina != null)
        {
            StopCoroutine(rotina);
            rotina = null;
        }

        if (materialAlvo != null)
            materialAlvo.color = corOriginal;
    }

    private void OnDisable()
    {
        PararPiscar();
    }
}
