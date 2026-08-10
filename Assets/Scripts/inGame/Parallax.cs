using UnityEngine;

public class Parallax : MonoBehaviour
{
    Material mat;
    float distance;
    float multiplicadorAtual = 1f;

    [Range(0f, 0.5f)]
    public float speed = 0.2f;

    void Start()
    {
        mat = GetComponent<Renderer>().material;
    }

    void Update()
    {
        distance += Time.deltaTime * speed * multiplicadorAtual;
        mat.SetTextureOffset("_MainTex", Vector2.right * distance);
    }

    public void AtualizarVelocidadeParallax(float novoMultiplicador)
    {
        multiplicadorAtual = novoMultiplicador;
    }
}