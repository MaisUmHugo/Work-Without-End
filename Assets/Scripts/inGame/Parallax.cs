using UnityEngine;

public class Parallax : MonoBehaviour
{
    Material mat;
    float distance;
    float MultiplicadorAtual = 1f;

    [Range(0f, 0.5f)]
    public float speed = 0.2f;

    public float multiplicadorIndividual = 1f;

    [Header("Materiais")]
    public Material materialAtual;
    public Material[] materiais;

    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();

        if (materialAtual != null)
            renderer.material = materialAtual;

        mat = renderer.material;
    }

    void Update()
    {
        distance += Time.deltaTime * speed * MultiplicadorAtual;
        mat.SetTextureOffset("_MainTex", Vector2.right * distance);
    }

    public void AtualizarVelocidadeParallax(float novoMultiplicador)
    {
        MultiplicadorAtual = novoMultiplicador * multiplicadorIndividual;
    }

    public void TrocarMaterial(int indice)
    {
        if (materiais == null || materiais.Length == 0)
            return;

        if (indice < 0 || indice >= materiais.Length)
            return;

        Renderer renderer = GetComponent<Renderer>();

        renderer.material = materiais[indice];
        mat = renderer.material;
    }
}