using UnityEngine;

public class Parallax : MonoBehaviour
{
    Material mat;
    float distance;
    float MultiplicadorAtual = 1f;

    [Range(0f, 0.5f)]
    public float speed = 0.2f;

    public float multiplicadorIndividual = 1f;

    void Start()
    {
        mat = GetComponent<Renderer>().material;
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
}