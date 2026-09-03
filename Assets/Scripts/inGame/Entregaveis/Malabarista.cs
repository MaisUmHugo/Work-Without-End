using System.Collections;
using UnityEngine;

public class Malabarista : Entregavel, IAjustavelDificuldade
{
    [Header("Configuração Principal")]
    public float velocidade;
    public float tempoAtivoEntrega;
    public float intervaloPiscar;
    public float distanciaEntrega;

    [Header("Tiro")]
    public GameObject bola;
    public float IntervaloTiro;
    public float DistanciaTiro;


    [Header("Dificuldade")]
    [SerializeField, Range(0f, 1f)] private float intensidadeEscalaMovimento = 0.58f;
    [SerializeField, Min(1f)] private float fatorMaximoMovimento = 12f;
    [SerializeField, Range(0f, 2f)] private float intensidadeEscalaBola = 1.09f;
    [SerializeField, Min(1f)] private float fatorMaximoBola = 13f;
    [SerializeField, Range(0f, 1f)] private float intensidadeReducaoIntervaloTiro = 0.08f;
    [SerializeField, Min(0.1f)] private float intervaloMinimoTiro = 2.5f;
    [SerializeField, Range(0f, 1f)] private float intensidadeEscalaDistanciaEntrega = 0.07f;
    [SerializeField, Min(1f)] private float fatorMaximoDistanciaEntrega = 1.8f;
    [SerializeField, Range(0f, 1f)] private float intensidadeEscalaDistanciaTiro = 0.02f;
    [SerializeField, Min(1f)] private float fatorMaximoDistanciaTiro = 1.2f;

    private float velocidadeBase;
    private float intervaloTiroBase;
    private float distanciaEntregaBase;
    private float distanciaTiroBase;
    private float fatorVelocidadeBola = 1f;

    [Header("Exclamação")]
    public Transform Exclamacao;
    public float tempoexclamacao;

    [Header("Cores")]
    private SpriteRenderer sr;
    public Color corNormal = Color.white;
    public Color corAtivo = Color.red;

    private bool coroutineIniciada = false;
    private bool emFluxoDeSaida = false;
    private bool recebeu = false;
    private bool podereceber = false;

    private bool podeatirar = false;
    private bool jaCausouDano = false;
    private Coroutine rotinaAtaque;
    private float xAnterior;

    private Mov jogador;

    [Header("Ajuste de posições")]
    public float offsetY;
    public float offsetBolaX;
    public float offsetBolaY;

    [Header("Efeitos Visuais")]
    public EntregavelPisca entregavelPisca;
    public PontuacaoPopup popupPontuacao;

    private Animator anim;
    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        velocidadeBase = velocidade;
        intervaloTiroBase = IntervaloTiro;
        distanciaEntregaBase = distanciaEntrega;
        distanciaTiroBase = DistanciaTiro;
        anim = GetComponent<Animator>();
        if (Exclamacao == null) Exclamacao = transform;
    }

    private void Start()
    {
        // Busca player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            jogador = playerObj.GetComponent<Mov>();
        else
            Debug.LogWarning("Player não encontrado!");

        // Posiciona na lane inicial
        Vector3 pos = transform.position;
        pos.y = LanesController.instance.PosicaoY((LanesController.Linhas)Random.Range(0, 4)) + offsetY;
        transform.position = pos;
        xAnterior = transform.position.x;
    }

    private void Update()
    {
        // Se recebeu, apenas sai andando
        if (recebeu)
        {
            transform.position += Vector3.left * velocidade * Time.deltaTime;
            if (transform.position.x < jogador.transform.position.x - 30f)
            {
                Destroy(gameObject);
                return;
            }
            return;
        }

        // SISTEMA DE TIRO
        if (rotinaAtaque == null &&
            EntrouNoAlcance(DistanciaTiro))
        {
            podeatirar = true;
            rotinaAtaque = StartCoroutine(atirar());
        }

        // SISTEMA DE ENTREGA
        if (EntregaPendente &&
            !coroutineIniciada &&
            EntrouNoAlcance(distanciaEntrega))
        {
            coroutineIniciada = true;
            emFluxoDeSaida = true;
            StartCoroutine(ProntoparaEntrega());
        }

        xAnterior = transform.position.x;

        // MOVIMENTO
        if (emFluxoDeSaida)
        {
            transform.position += Vector3.left * velocidade * Time.deltaTime;
        }
        else
        {
            Vector3 direcao = (jogador.transform.position - transform.position).normalized;
            transform.position += new Vector3(direcao.x, 0, 0) * velocidade * Time.deltaTime;
        }

        // Se sair da tela
        Vector3 viewPos = Camera.main.WorldToViewportPoint(transform.position);
        if (viewPos.x < -0.1f)
        {
            if (EntregaPendente)
                RegistrarFalhaEntrega();

            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Caixa") && podereceber && EntregaPendente)
            ReceberEntrega();

        if (collision.CompareTag("Player") && !jaCausouDano)
        {
            jaCausouDano = true;
            emFluxoDeSaida = true;
            FalharEntrega();
            podereceber = false;
            ativoParaEntrega = false;
            podeatirar = true;

            entregavelPisca?.PararPiscar();
        }
    }

    public override void ReceberEntrega()
    {
        if (!podereceber || !EntregaPendente) return;

        int pontosRecebidos = ProcessarEntrega();
        recebeu = true;
        anim.SetTrigger("RecebeuEntrega");
        popupPontuacao?.MostrarPontuacao(pontosRecebidos);
        entregavelPisca?.PiscarRecebendo();

        // Desativa colisão
        Collider2D col = GetComponent<Collider2D>();
        if (col) col.enabled = false;

        StartCoroutine(DelayTransparente());
    }

    private IEnumerator ProntoparaEntrega()
    {
        yield return new WaitForSeconds(0.1f);

        if (!EntregaPendente) yield break;

        podeatirar = false;
        if (rotinaAtaque != null)
        {
            StopCoroutine(rotinaAtaque);
            rotinaAtaque = null;
        }

        podereceber = true;
        ativoParaEntrega = true;

        StartCoroutine(exclamacao());
        entregavelPisca?.PiscarAtivo();

        yield return new WaitForSeconds(tempoAtivoEntrega);

        if (!recebeu && RegistrarFalhaEntrega())
        {
            entregavelPisca?.PararPiscar();
            podereceber = false;
            ativoParaEntrega = false;
            podeatirar = true;

            sr.color = corNormal;
        }
    }

    private IEnumerator DelayTransparente()
    {
        yield return new WaitForSeconds(1.5f);
        entregavelPisca?.PararPiscar();

        Color c = sr.color;
        c.a = 0.5f;
        sr.color = c;
    }

    // TIRO 
    public IEnumerator atirar()
    {
        while (podeatirar && !podereceber && !recebeu)
        {
            Vector3 posSpawn =
                transform.position +
                Vector3.left * offsetBolaX +
                Vector3.up * offsetBolaY;

            GameObject novaBola = Instantiate(bola, posSpawn, Quaternion.identity);

            float randomOffsetX = Random.Range(10f, 25f);
            Vector3 destino = new Vector3(
                posSpawn.x - randomOffsetX,
                LanesController.instance.PosicaoY(jogador.linhaAtual),
                0f
            );

            Bola componenteBola = novaBola.GetComponent<Bola>();
            componenteBola.CaminhoBola(destino);
            componenteBola.DefinirFatorVelocidade(fatorVelocidadeBola);

            yield return new WaitForSeconds(IntervaloTiro);
        }

        rotinaAtaque = null;
    }


    private IEnumerator exclamacao()
    {
        yield return new WaitForSeconds(0.1f);

        GameObject prefab = Resources.Load<GameObject>("PontoExclamacao");
        if (prefab != null)
        {
            GameObject instancia =
                Instantiate(prefab, Exclamacao.position, Quaternion.identity);

            instancia.transform.SetParent(transform, worldPositionStays: true);

            yield return new WaitForSeconds(tempoexclamacao);
            Destroy(instancia);
        }
    }

    private bool EntrouNoAlcance(float distancia)
    {
        float xJogador = jogador.transform.position.x;
        float limiteDireito = xJogador + distancia;
        float xAtual = transform.position.x;

        return Mathf.Abs(xAtual - xJogador) <= distancia ||
               (xAnterior > limiteDireito && xAtual <= limiteDireito);
    }

    public void AplicarDificuldade(float multiplicadorGlobal)
    {
        float fatorMovimento = CalculoDificuldade.CalcularFator(multiplicadorGlobal, intensidadeEscalaMovimento, fatorMaximoMovimento);
        fatorVelocidadeBola = Mathf.Clamp(
            1f + (fatorMovimento - 1f) * intensidadeEscalaBola,
            1f,
            Mathf.Max(1f, fatorMaximoBola));
        float fatorCadencia = CalculoDificuldade.CalcularFator(multiplicadorGlobal, intensidadeReducaoIntervaloTiro, 2f);
        float fatorDistanciaEntrega = CalculoDificuldade.CalcularFator(multiplicadorGlobal, intensidadeEscalaDistanciaEntrega, fatorMaximoDistanciaEntrega);
        float fatorDistanciaTiro = CalculoDificuldade.CalcularFator(multiplicadorGlobal, intensidadeEscalaDistanciaTiro, fatorMaximoDistanciaTiro);

        velocidade = velocidadeBase * fatorMovimento;
        IntervaloTiro = Mathf.Max(intervaloMinimoTiro, intervaloTiroBase / fatorCadencia);
        distanciaEntrega = distanciaEntregaBase * fatorDistanciaEntrega;
        DistanciaTiro = distanciaTiroBase * fatorDistanciaTiro;
    }
}
