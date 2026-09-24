using System.Collections;
using UnityEngine;

public class Parallax : MonoBehaviour
{
    [Header("Parallax")]
    [Range(0f, 0.5f)]
    public float speed = 0.2f;

    [Header("Materiais")]
    [SerializeField] private Material material1;
    [SerializeField] private Material material2;

    [Header("Repetição dos Materiais")]
    [SerializeField] private float repeticoesMaterial1 = 1f;
    [SerializeField] private float repeticoesMaterial2 = 1f;

    [Header("Fade da Transição")]
    [SerializeField] private float tempoFadeIn = 0.5f;
    [SerializeField] private float tempoVisivel = 1f;
    [SerializeField] private float tempoFadeOut = 0.5f;

    private Renderer render;
    private Material mat;

    private float distance;
    private float multiplicadorAtual = 1f;
    private bool materialInicializado;

    private int materialAtual = -1;

    private static SpriteRenderer fadeRenderer;
    private static bool fadeInicializado;

    private void Awake()
    {
        render = GetComponent<Renderer>();

        if (!fadeInicializado)
        {
            GameObject fadeObject = GameObject.FindGameObjectWithTag("FadeIn");

            if (fadeObject != null)
            {
                fadeRenderer = fadeObject.GetComponent<SpriteRenderer>();

                if (fadeRenderer != null)
                {
                    Color cor = fadeRenderer.color;
                    cor.a = 0f;
                    fadeRenderer.color = cor;
                }
            }

            fadeInicializado = true;
        }
    }

    private void Start()
    {
        AtualizarMaterial(1);
    }

    private void Update()
    {
        distance += Time.deltaTime * speed * multiplicadorAtual;
        distance %= 1f;

        if (mat != null)
        {
            float repeticoes = materialAtual == 0
                ? repeticoesMaterial1
                : repeticoesMaterial2;

            mat.SetTextureOffset(
                "_MainTex",
                Vector2.right * distance * repeticoes
            );
        }
    }

    public void AtualizarVelocidadeParallax(float novoMultiplicador)
    {
        multiplicadorAtual = novoMultiplicador;
    }

    public void AtualizarMaterial(int horda)
    {
        int grupo = (horda - 1) / 2;
        int indiceMaterial = grupo % 2;

        if (indiceMaterial == materialAtual)
            return;

        Material materialBase = indiceMaterial == 0
            ? material1
            : material2;

        if (materialBase == null)
            return;

        if (!materialInicializado)
        {
            AplicarMaterial(indiceMaterial, materialBase);
            materialInicializado = true;
            return;
        }

        if (fadeRenderer != null)
        {
            StartCoroutine(FadeTransicao(indiceMaterial, materialBase));
        }
        else
        {
            AplicarMaterial(indiceMaterial, materialBase);
        }
    }

    private IEnumerator FadeTransicao(int indiceMaterial, Material materialBase)
    {
        Color cor = fadeRenderer.color;

        float tempo = 0f;

        while (tempo < tempoFadeIn)
        {
            tempo += Time.deltaTime;

            cor.a = Mathf.Lerp(0f, 0.5f, tempo / tempoFadeIn);
            fadeRenderer.color = cor;

            yield return null;
        }

        cor.a = 1f;
        fadeRenderer.color = cor;

        AplicarMaterial(indiceMaterial, materialBase);

        yield return new WaitForSeconds(tempoVisivel);

        tempo = 0f;

        while (tempo < tempoFadeOut)
        {
            tempo += Time.deltaTime;

            cor.a = Mathf.Lerp(0.5f, 0f, tempo / tempoFadeOut);
            fadeRenderer.color = cor;

            yield return null;
        }

        cor.a = 0f;
        fadeRenderer.color = cor;
    }

    private void AplicarMaterial(int indiceMaterial, Material materialBase)
    {
        mat = new Material(materialBase);

        float repeticoes = indiceMaterial == 0
            ? repeticoesMaterial1
            : repeticoesMaterial2;

        mat.SetTextureOffset(
            "_MainTex",
            Vector2.right * distance * repeticoes
        );

        render.material = mat;

        materialAtual = indiceMaterial;
    }
}