using System.Collections;
using UnityEngine;

public class Bebado : Entregavel
{
    public Transform Exclamacao;

    [Header("Configuração do Bêbado")]
    public float velocidade;
    public float trocaLaneIntervalo;
    public float velocidadeTrocaLane;

    [Header("Entrega")]
    public float distanciaEntrega;
    public float tempoexclamacao;

    private Mov jogador;
    private float tempoUltimaTroca;
    private LanesController.Linhas minhaLane;
    private SpriteRenderer sr;

    private bool podeReceber = false;       // agora é permanente
    private bool avisoExecutado = false;    // controla o piscar único

    private float yTravado;
    private bool parado = false;

    private Collider2D colisor;
    private Animator anim;

    public EntregavelPisca entregavelPisca;
    public PontuacaoPopup popupPontuacao;

    [Header("Ajuste de posição")]
    public float offsetY;


    private void Start()
    {
        colisor = GetComponent<Collider2D>();
        colisor.enabled = false; // só ativa quando pode receber

        if (Exclamacao == null) Exclamacao = transform;

        jogador = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Mov>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();

        // começa numa lane aleatória
        int laneIndex = Random.Range(0, 4);
        minhaLane = (LanesController.Linhas)laneIndex;

        Vector3 pos = transform.position;
        pos.y = LanesController.instance.PosicaoY(minhaLane) + offsetY;
        transform.position = pos;

        tempoUltimaTroca = Time.time;
    }

    private void Update()
    {
        if (jogador == null) return;

        if (!parado)
        {
            // anda pra esquerda
            transform.position += Vector3.left * velocidade * Time.deltaTime;

            // troca de lane
            if (Time.time >= tempoUltimaTroca + trocaLaneIntervalo)
            {
                TrocarLaneAleatoria();
                tempoUltimaTroca = Time.time;
            }

            // suaviza mudança de lane
            float novoY = Mathf.MoveTowards(
                transform.position.y,
                LanesController.instance.PosicaoY(minhaLane) + offsetY,
                velocidadeTrocaLane * Time.deltaTime
            );

            transform.position = new Vector3(transform.position.x, novoY, transform.position.z);

            float distancia = Mathf.Abs(transform.position.x - jogador.transform.position.x);

            if (!podeReceber && distancia <= distanciaEntrega)
            {
                podeReceber = true;        
                colisor.enabled = true;  

                if (!avisoExecutado)
                {
                    avisoExecutado = true;
                    StartCoroutine(EfeitoDeAviso());
                }
            }
        }
        else
        {
            // travado no Y quando recebeu a entrega
            transform.position = new Vector3(
                transform.position.x + (-velocidade * Time.deltaTime),
                yTravado,
                transform.position.z
            );
        }

        // saiu da tela
        Vector3 viewPos = Camera.main.WorldToViewportPoint(transform.position);
        if (viewPos.x < -0.1f)
        {
            if (!parado)
            {
                Debug.Log("Saiu sem entrega -> perdeu combo");
                PerderCombo();
            }

            Destroy(gameObject);
        }
    }

    private void TrocarLaneAleatoria()
    {
        int novaLane;
        do
        {
            novaLane = Random.Range(0, 4);
        } while (novaLane == (int)minhaLane);

        minhaLane = (LanesController.Linhas)novaLane;
    }
    public override void ReceberEntrega()
    {
        if (!podeReceber) return;

        anim.SetTrigger("RecebeuEntrega");
        base.ReceberEntrega();

        parado = true;
        yTravado = transform.position.y;
        colisor.enabled = false;

        // pontuação com combo
        int total = 100 * ComboManager.instance.GetMultiplicador();
        popupPontuacao?.MostrarPontuacao(total);

        entregavelPisca?.PiscarRecebendo();
        StartCoroutine(PararPiscar());

        Debug.Log("Entrega realizada com sucesso no bêbado!");
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            FalharEntrega();
        }
        else if (collision.CompareTag("Caixa"))
        {
            if (podeReceber)
                ReceberEntrega();
        }
    }


    private IEnumerator PararPiscar()
    {
        yield return new WaitForSeconds(1.5f);
        entregavelPisca?.PararPiscar();
    }

    private IEnumerator EfeitoDeAviso()
    {
        // piscar
        entregavelPisca?.PiscarAtivo();

        // exclamação
        GameObject prefab = Resources.Load<GameObject>("PontoExclamacao");
        GameObject instancia = null;

        if (prefab != null)
        {
            instancia = Instantiate(prefab, Exclamacao.position, Quaternion.identity);
            instancia.transform.SetParent(transform, true);
        }

        // tempo que a exclamação dura
        yield return new WaitForSeconds(tempoexclamacao);

        // para exclamação
        if (instancia != null)
            Destroy(instancia);

        // para o piscar
        entregavelPisca?.PararPiscar();
    }
}
