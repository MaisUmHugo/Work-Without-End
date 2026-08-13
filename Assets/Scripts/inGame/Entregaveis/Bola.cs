using UnityEngine;

public class Bola : MonoBehaviour
{
    private Vector3 pontoFinal;

    [Header("Velocidade")]
    public float velocidade = 10f;
    private float velocidadeAtual;
    private bool chegouNaLane = false;

    private void Awake()
    {
        velocidadeAtual = velocidade;
    }

    public void CaminhoBola(Vector3 destino)
    {
        pontoFinal = destino;
    }

    public void DefinirFatorVelocidade(float fator)
    {
        velocidadeAtual = velocidade * Mathf.Max(1f, fator);
    }


    void Update()
    {
        if (!chegouNaLane)
        {
            // move até a lane
            transform.position = Vector3.MoveTowards(
                transform.position,
                pontoFinal,
                velocidadeAtual * Time.deltaTime
            );

            // quando chegar na lane, começa mover para esquerda
            if (Vector3.Distance(transform.position, pontoFinal) < 0.05f)
            {
                chegouNaLane = true;
            }
        }
        else
        {
            // movimento contínuo para a esquerda
            transform.position += Vector3.left * velocidadeAtual * Time.deltaTime;
        }

        // destruir quando sair da tela
        Vector3 view = Camera.main.WorldToViewportPoint(transform.position);
        if (view.x < -0.1f)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            VidaManager.instance.PerderVida();
        }
    }
}
