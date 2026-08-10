using System.Collections;
using UnityEngine;

public class Louco : Entregavel
{
    public Transform Exclamacao;
    [Header("Configuração do Louco")]
    public float velocidadeVertical = 5f;   // velocidade no eixo Y
    public float velocidadeFrente = 2f;     // velocidade no eixo X (frente = esquerda)
    public float velocidadeSaida = 6f;      // velocidade depois que atravessou
    public float tempoAtivoEntrega = 1.2f;
    public float tempoexclamacao;

    private bool atravessando = true;
    private bool terminouTravessia = false;
    private float yDestino;
    private float yTravado;

    private bool entregaJaAtivada = false;
    private bool entregaRecebida = false;

    private bool pulou = false;
    private float yL1;
    private float yL4;

    private SpriteRenderer sr;

    [Header("Efeito Visual")]
    public PontuacaoPopup popupPontuacao;

    private Animator anim;

    private Collider2D colisor;
    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
    }
    private void Start()
    {
        anim = GetComponentInChildren<Animator>();
        colisor = GetComponent<Collider2D>();
        colisor.enabled = false;
        // Decide direção: sobe ou desce
        // bool vindoDeBaixo = Random.value > 0.5f;
        bool vindoDeBaixo = true;


        yL1 = LanesController.instance.PosicaoY(LanesController.Linhas.L1);
        yL4 = LanesController.instance.PosicaoY(LanesController.Linhas.L4);

        float offset = 6f; // para sair da "rua"

        yDestino = vindoDeBaixo ? (yL1 + offset) : (yL4 - offset);
    }

    private void Update()
    {
        if (atravessando)
        {
            // Movimento diagonal (X negativo + Y até destino)
            transform.position += new Vector3(-velocidadeFrente * Time.deltaTime, 0f, 0f);

            float novoY = Mathf.MoveTowards(
                transform.position.y,
                yDestino,
                velocidadeVertical * Time.deltaTime
            );

            transform.position = new Vector3(transform.position.x, novoY, transform.position.z);

            if (!pulou &&
       (Mathf.Abs(transform.position.y - yL1) < 0.3f ||
        Mathf.Abs(transform.position.y - yL4) < 0.3f))
            {
                pulou = true;

                if (anim != null)
                    anim.SetTrigger("Pulando");

                if (!entregaJaAtivada)
                {
                    entregaJaAtivada = true;
                    ativoParaEntrega = true;
                    colisor.enabled = true;

                    StartCoroutine(exclamacao());
                    PiscarAtivo();

                    Debug.Log("Entrega ativada durante o PULO!");
                }
            }       

            // Chegou no outro lado (passou todas as lanes)
            if ((yDestino > 0 && transform.position.y >= yDestino - 0.05f) ||
                (yDestino < 0 && transform.position.y <= yDestino + 0.05f))
            {
                atravessando = false;
                terminouTravessia = true;
                StartCoroutine(PararPiscar());

                if (!entregaRecebida)
                {
                    if (anim != null)
                        anim.SetTrigger("FalhouEntrega");

                    ativoParaEntrega = false;
                    if (colisor != null)
                        colisor.enabled = false;

                    PerderCombo();
                    Debug.Log("Louco caiu sem receber entrega, fazueli");
                }
                else
                {
                    Debug.Log("Louco terminou a travessia e recebeu a entrega!");
                }

                yTravado = transform.position.y; // trava Y
            }
        }
        else if (terminouTravessia)
        {
            // Depois da travessia, anda reto em X até sair da tela
            transform.position = new Vector3(
                transform.position.x - velocidadeSaida * Time.deltaTime,
                yTravado,
                transform.position.z
            );

            // Destrói quando sumir da tela
            Vector3 viewPos = Camera.main.WorldToViewportPoint(transform.position);
            if (viewPos.x < -0.1f) Destroy(gameObject);
        }
    }

    public override void ReceberEntrega()
    {
        if (!ativoParaEntrega)
        {
            Debug.Log("Entrega não ativa no momento!");  // Log para depuração
            return;  // Não faz nada se não estiver ativo para a entrega
        }
        sr.color = Color.white;
        StartCoroutine(PararPiscar());
        ativoParaEntrega = false;  // Desativa entrega após processar
        entregaRecebida = true;
        //entregavelPisca?.PararPiscar();
        // Calcula pontuação com bônus
        int multiplicador = ComboManager.instance.GetMultiplicador();
        int total = 100 * multiplicador;

        popupPontuacao?.MostrarPontuacao(total);
        PiscarRecebendo();
        if (colisor != null)
            colisor.enabled = false;

        if (anim != null)
            anim.SetTrigger("RecebeuEntrega");

        // base.ReceberEntrega();  // Executa a lógica da base (pontuação, combo, etc.)
        ScoreManager.instance.AdicionarPontos(100);
        ComboManager.instance.AumentarCombo();
        if (HordaManager.instance != null)
            HordaManager.instance.AumentarEntrega();

        StartCoroutine(PararPiscar());
        Debug.Log("Louco recebeu a entrega com sucesso!");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!ativoParaEntrega)
        {
            Debug.Log("Louco não está ativo para entrega no momento");  // Verifique a condição
            return;  // Evita processar a colisão se não for o momento certo
        }

        if (collision.CompareTag("Caixa"))
        {
            Debug.Log("Louco colidiu com a caixa! Iniciando entrega...");
            ReceberEntrega();

            // Opcional: destruir a caixa depois de processar a entrega
            Destroy(collision.gameObject);
        }
    }
    public void PiscarAtivo()
    {
        if (rotinaPiscar != null) StopCoroutine(rotinaPiscar);
        rotinaPiscar = StartCoroutine(Piscar(new Color(1f, 0.4f, 0.5f, 0.6f), 0.3f, 3));
    }
    public void PiscarRecebendo()
    {
        if (rotinaPiscar != null) StopCoroutine(rotinaPiscar);
        rotinaPiscar = StartCoroutine(Piscar(new Color(0.7f, 0.7f, 0.7f, 0.4f), 0.3f, 3));
    }

    private IEnumerator PararPiscar()
    {
        yield return new WaitForSeconds(0.05f);
        sr.color = Color.white;
    }
    private Coroutine rotinaPiscar;

    private IEnumerator Piscar(Color corPiscar, float intervalo, int quantidade)
    {
        Color corOriginal = sr.color;

        for (int i = 0; i < quantidade; i++)
        {
            sr.color = corPiscar;
            yield return new WaitForSeconds(intervalo);

            sr.color = corOriginal;
            yield return new WaitForSeconds(intervalo);
        }

        sr.color = corOriginal;
        rotinaPiscar = null;
    }

    private IEnumerator exclamacao()
    {
        yield return new WaitForSeconds(0.1f);
        //exclamacao
        GameObject prefab = Resources.Load<GameObject>("PontoExclamacao");
        if (prefab != null)
        {
            GameObject instancia = Instantiate(prefab, Exclamacao.position, Quaternion.identity);
            instancia.transform.SetParent(gameObject.transform, worldPositionStays: true);
            float tempo = 0;
            while (tempo < tempoexclamacao)
            {
                tempo += Time.deltaTime;
                yield return null;
            }
            Destroy(instancia);
            tempo = 0;
        }
    }
}
