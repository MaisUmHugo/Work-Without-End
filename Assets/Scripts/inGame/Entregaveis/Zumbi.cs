using UnityEngine;
using System.Collections;

public class Zumbi : Entregavel, IAjustavelDificuldade
{
    public Transform Exclamacao;
    [Header("Configuração do Zumbi")]
    public float velocidadeCaminhada;
    public float velocidadeCorrida;
    public float velocidadeTrocaLane;
    public float dCorrida; // distancia de corrida
    public float dColisao; // Distancia De colisão
    public float tempoexclamacao;

    [Header("Dificuldade")]
    [SerializeField, Range(0f, 1f)] private float intensidadeEscalaCaminhada = 0.42f;
    [SerializeField, Min(1f)] private float fatorMaximoCaminhada = 9f;
    [SerializeField, Range(0f, 1f)] private float intensidadeEscalaCorrida = 0.32f;
    [SerializeField, Min(1f)] private float fatorMaximoCorrida = 7f;
    [SerializeField, Range(0f, 1f)] private float intensidadeEscalaTrocaLane = 0.115f;
    [SerializeField, Min(1f)] private float fatorMaximoTrocaLane = 3.2f;

    private float velocidadeCaminhadaBase;
    private float velocidadeCorridaBase;
    private float velocidadeTrocaLaneBase;

    private bool correndo = false;
    private bool caiu = false;
    private bool recebeuEntrega = false;
    private float yTravado;
    private SpriteRenderer sr;
    private Mov jogador;
    private Animator anim;

    [Header("Efeitos")]
    public EntregavelPisca entregavelPisca;
    public PontuacaoPopup popupPontuacao;

    private bool jaDeuDano = false;

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
        velocidadeCaminhadaBase = velocidadeCaminhada;
        velocidadeCorridaBase = velocidadeCorrida;
        velocidadeTrocaLaneBase = velocidadeTrocaLane;

        if (entregavelPisca == null)
            entregavelPisca = GetComponent<EntregavelPisca>();

        if (entregavelPisca == null)
            Debug.LogWarning(name + ": componente EntregavelPisca não encontrado.", this);
    }
    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            jogador = playerObj.GetComponent<Mov>();

        Vector3 pos = transform.position;
        pos.y = LanesController.instance.PosicaoY((LanesController.Linhas)Random.Range(0, 4));
        transform.position = pos;
    }

    private void Update()
    {
        if (jogador == null) return;

        if (caiu)
            MoverParaEsquerda(velocidadeCaminhada);
        else if (recebeuEntrega)
            MoverParaEsquerda(velocidadeCaminhada);
        else if (!correndo)
        {
            anim.SetBool("Andar", true);
            transform.position += Vector3.left * velocidadeCaminhada * Time.deltaTime;

            if (Mathf.Abs(transform.position.x - jogador.transform.position.x) <= dCorrida)
                IniciarCorrida();
        }
        else
        {
            float xAnterior = transform.position.x;
            float yAlvo = LanesController.instance.PosicaoY(jogador.linhaAtual);
            float novoY = Mathf.MoveTowards(transform.position.y, yAlvo, velocidadeTrocaLane * Time.deltaTime);

            transform.position = new Vector3(
                transform.position.x - velocidadeCorrida * Time.deltaTime,
                novoY,
                transform.position.z
            );

            float xJogador = jogador.transform.position.x;
            bool ultrapassouJogador = xAnterior > xJogador && transform.position.x <= xJogador;

            if (Vector3.Distance(transform.position, jogador.transform.position) <= dColisao || ultrapassouJogador)
                CairECausarDano();
        }

        Vector3 viewPos = Camera.main.WorldToViewportPoint(transform.position);
        if (viewPos.x < -0.1f)
            Destroy(gameObject);

        if (transform.position.x < jogador.transform.position.x - 30f)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void MoverParaEsquerda(float velocidade)
    {
        transform.position = new Vector3(
            transform.position.x - velocidade * Time.deltaTime,
            yTravado,
            transform.position.z
        );
    }

    private void IniciarCorrida()
    {
        Debug.Log("Zumbi Correndo");
        correndo = true;
        anim.SetBool("Correr", true);
        anim.SetBool("Andar", false);
        ativoParaEntrega = true;
        StartCoroutine(exclamacao());
        entregavelPisca?.PiscarAtivo();
    }

    private void CairECausarDano()
    {
        if (caiu) return;

        anim.SetBool("Caiu", true);
        caiu = true;
        correndo = false;
        ativoParaEntrega = false;
        entregavelPisca?.PararPiscar();
        RegistrarFalhaEntrega();

        yTravado = transform.position.y;

        /*Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        */

        //StartCoroutine(DelayCair());
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (correndo && ativoParaEntrega && collision.CompareTag("Caixa"))
        {
            ReceberEntrega();
            return;
        }

        // Se ele está caindo, e encosta no player → dá dano imediato
        if (caiu && !jaDeuDano && collision.CompareTag("Player"))
        {
            jaDeuDano = true;
            Debug.Log("DANO DURANTE A ANIMAÇÃO DE CAIR!");
            VidaManager.instance.PerderVida();
        }
    }

    public override void ReceberEntrega()
    {
        if (!correndo || !ativoParaEntrega || !EntregaPendente) return;

        int pontosRecebidos = ProcessarEntrega();

        anim.SetBool("RecebeuEntrega", true);
        ativoParaEntrega = false;
        correndo = false;
        recebeuEntrega = true;
        yTravado = transform.position.y;

        // Calcula pontuação com bônus
        popupPontuacao?.MostrarPontuacao(pontosRecebidos);

        entregavelPisca?.PiscarRecebendo();
        StartCoroutine(DelayTransparente());

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }

    private IEnumerator DelayTransparente()
    {
        yield return new WaitForSeconds(1.5f);
        entregavelPisca?.PararPiscar();
        anim.SetBool("Transparente", true);
    }

   /* private IEnumerator DelayCair()
    {
        yield return new WaitForSeconds(1.5f);
        FalharEntrega();
    }
   */
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

    public void AplicarDificuldade(float multiplicadorGlobal)
    {
        float fatorCaminhada = CalculoDificuldade.CalcularFator(multiplicadorGlobal, intensidadeEscalaCaminhada, fatorMaximoCaminhada);
        float fatorCorrida = CalculoDificuldade.CalcularFator(multiplicadorGlobal, intensidadeEscalaCorrida, fatorMaximoCorrida);
        float fatorTrocaLane = CalculoDificuldade.CalcularFator(multiplicadorGlobal, intensidadeEscalaTrocaLane, fatorMaximoTrocaLane);

        velocidadeCaminhada = velocidadeCaminhadaBase * fatorCaminhada;
        velocidadeCorrida = velocidadeCorridaBase * fatorCorrida;
        velocidadeTrocaLane = velocidadeTrocaLaneBase * fatorTrocaLane;
    }
}
