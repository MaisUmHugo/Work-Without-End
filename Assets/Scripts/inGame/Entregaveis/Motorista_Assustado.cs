using UnityEngine;
using System.Collections;

public class Motorista_Assustado : Entregavel, IAjustavelDificuldade
{
    public Transform Exclamacao;
    [Header("Configuração Motorista")]
    public float velocidade;
    public float tempoAtivoEntrega;
    public float intervaloPiscar;
    public float distanciaEntrega;
    public float tempoexclamacao;

    [Header("Dificuldade")]
    [SerializeField, Range(0f, 1f)] private float intensidadeEscalaMovimento = 0.4f;
    [SerializeField, Min(1f)] private float fatorMaximoMovimento = 8.5f;
    private float velocidadeBase;

    private SpriteRenderer sr;
    //public Color corNormal = Color.white; // cor padrão
    //public Color corAtivo = Color.red;    // cor quando está ativo para receber entrega
    private bool coroutineIniciada = false;
    private bool emFluxoDeSaida = false;
    private Mov jogador;
    private bool recebeu, podereceber;
    private bool jaCausouDano;
    private float xAnterior;
    private Animator anim;

    [Header("Efeito Visual")]
    public EntregavelPisca entregavelPisca;
    public PontuacaoPopup popupPontuacao;
    private void Awake()
    {
        //sr = GetComponent<SpriteRenderer>();
        sr = GetComponentInChildren<SpriteRenderer>();
        velocidadeBase = velocidade;
        anim = GetComponentInChildren<Animator>();
    }
    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            jogador = playerObj.GetComponent<Mov>();
        }
        else
        {
            Debug.LogWarning("Player não encontrado! Verifique se o objeto do jogador tem a tag 'Player'.");
        }

        Vector3 pos = transform.position;

        if (jogador != null)
        {
            pos.y = LanesController.instance.PosicaoY(jogador.linhaAtual);
        }
        else
        {
            Debug.Log("F PLAYER, SOBROU NADA P BETINHA");
            pos.y = LanesController.instance.PosicaoY(
            (LanesController.Linhas)Random.Range(0, 4)
        );
        }
        transform.position = pos;
        xAnterior = transform.position.x;
    }
    private void Update()
    {
        if (recebeu)
        {
            // Já entregou - apenas vai embora para a esquerda
            transform.position += Vector3.left * velocidade * Time.deltaTime;
            if (transform.position.x < jogador.transform.position.x - 30f)
            {
                Destroy(gameObject);
                return;
            }
            return;
        }

        // Se está em range de entrega, pode esperar pela caixa
        if (EntregaPendente && !coroutineIniciada && EntrouNoAlcance(distanciaEntrega))
        {
            coroutineIniciada = true;
            emFluxoDeSaida = true;
            StartCoroutine(ProntoparaEntrega());
        }

        xAnterior = transform.position.x;

        // --- MOVIMENTO ---
        if (emFluxoDeSaida)
        {
            // Continua andando para a esquerda mesmo que esteja esperando entrega
            transform.position += Vector3.left * velocidade * Time.deltaTime;
        }
        else
        {
            // Se ainda não está em range → segue em direção ao jogador
            Vector3 direcao = (jogador.transform.position - transform.position).normalized;
            transform.position += new Vector3(direcao.x, 0, 0) * velocidade * Time.deltaTime;
        }

        // saiu da tela
        Vector3 viewPos = Camera.main.WorldToViewportPoint(transform.position);
        if (viewPos.x < -0.1f)
        {
            if (EntregaPendente) // saiu sem receber -> falha
            {
                RegistrarFalhaEntrega();
                Debug.Log($"{gameObject.name} saiu da tela sem entrega!");
            }
            else
            {
                Debug.Log($"{gameObject.name} saiu da tela após entrega.");
            }

            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Caixa") && podereceber && EntregaPendente)
        {
            ReceberEntrega();
        }
        if (collision.CompareTag("Player") && !jaCausouDano)
        {
            jaCausouDano = true;
            emFluxoDeSaida = true;
            FalharEntrega();
            podereceber = false;
            ativoParaEntrega = false;
            entregavelPisca?.PararPiscar();
        }
    }
    public override void ReceberEntrega()
    {
        if (!podereceber || !EntregaPendente) return;

        //sr.color = corNormal;
        int pontosRecebidos = ProcessarEntrega();

        entregavelPisca?.PiscarRecebendo();

        // Calcula pontuação com bônus
        popupPontuacao?.MostrarPontuacao(pontosRecebidos);
        anim.SetTrigger("ReceberEntrega");


        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false; // desliga colisão
        }
        recebeu = true;

        StartCoroutine(FinalizarFeedbackRecebimento());
    }
    private System.Collections.IEnumerator ProntoparaEntrega()
    {
        if (!EntregaPendente) yield break;

        podereceber = true;
        ativoParaEntrega = true;
        //sr.color = corAtivo; // piscar (feedback visual)
        entregavelPisca?.PiscarAtivo();
        Debug.Log("MAssustado proximo, entregue agora!");
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

        // espera a janela de tempo para aceitar a entrega
        yield return new WaitForSeconds(tempoAtivoEntrega);

        if (!recebeu && RegistrarFalhaEntrega())
        {
            podereceber = false;
            ativoParaEntrega = false;
            anim.SetTrigger("FalhouEntrega");
            entregavelPisca?.PararPiscar();
        }
    }
    private IEnumerator FinalizarFeedbackRecebimento()
    {
        yield return new WaitForSeconds(0.75f);
        entregavelPisca?.PararPiscar();

        Color cor = sr.color;
        cor.a = 0.5f;
        sr.color = cor;
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
        velocidade = velocidadeBase * fatorMovimento;
    }
}
