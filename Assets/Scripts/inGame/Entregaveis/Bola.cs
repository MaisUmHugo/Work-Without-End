using UnityEngine;

public class Bola : MonoBehaviour
{
    private Vector3 pontoFinal;

    [Header("Velocidade")]
    public float velocidade = 10f;
    public float multiplicadorVelocidade;
    private bool chegouNaLane = false;

    public void CaminhoBola(Vector3 destino)
    {
        pontoFinal = destino;
    }


    void Update()
    {
        if (!chegouNaLane)
        {
            // move até a lane
            transform.position = Vector3.MoveTowards(
                transform.position,
                pontoFinal,
                (velocidade * multiplicadorVelocidade) * Time.deltaTime
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
            transform.position += Vector3.left * (velocidade * multiplicadorVelocidade) * Time.deltaTime;
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
