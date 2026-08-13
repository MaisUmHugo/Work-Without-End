using UnityEngine;
using System.Collections;

public class Pessoa_janela : Entregavel, IAjustavelDificuldade
{
    [Header("Configuração da Janela")]
    public float velocidade;
    public float tempoAtivoEntrega;
    public float intervaloPiscar;
    public float distanciaEntrega;
    public Vector3 offset;

    [Header("Dificuldade")]
    [SerializeField, Range(0f, 1f)] private float intensidadeEscalaMovimento = 0.25f;
    [SerializeField, Min(1f)] private float fatorMaximoMovimento = 6f;

    private float velocidadeBase;
    private float xAnterior;
    private SpriteRenderer sr;
    public Color corNormal = Color.blue; // cor padrão
    public Color corAtivo = Color.red;    // cor quando está ativo para receber entrega
    private bool coroutineIniciada = false;
    private bool emFluxoDeSaida = false;
    private Mov jogador;
    private bool recebeu, podereceber;
    private Animator anim;

    [Header("Efeito Visual")]
    public EntregavelPisca entregavelPisca;
    public PontuacaoPopup popupPontuacao;


    [Header("Exclamação")]
    public Transform Exclamacao;
    public float tempoExclamacao = 1.5f;

    private void Awake()
    {
        //sr = GetComponent<SpriteRenderer>();
        sr = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
        velocidadeBase = velocidade;
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
        pos.y = LanesController.instance.PosicaoY((LanesController.Linhas.L1));
        transform.position = pos + offset;
        xAnterior = transform.position.x;
    }
    private void Update()
    {
        if (recebeu)
        {
            // Já entregou → apenas vai embora para a esquerda
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

    private bool EntrouNoAlcance(float distancia)
    {
        float xJogador = jogador.transform.position.x;
        float limiteDireito = xJogador + distancia;
        float xAtual = transform.position.x;

        return Mathf.Abs(xAtual - xJogador) <= distancia ||
               (xAnterior > limiteDireito && xAtual <= limiteDireito);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Caixa") && podereceber && EntregaPendente)
        {
            ReceberEntrega();
        }
    }
    public override void ReceberEntrega()
    {
        if (!podereceber || !EntregaPendente) return;

        int pontosRecebidos = ProcessarEntrega();

        entregavelPisca?.PiscarRecebendo();
        popupPontuacao?.MostrarPontuacao(pontosRecebidos);

        if (anim != null)
            anim.SetTrigger("RecebeuEntrega");

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        recebeu = true;

        StartCoroutine(EsperarAnimacaoDepoisTransparente());
    }
    private IEnumerator ProntoparaEntrega()
    {
        if (anim != null)
            anim.SetTrigger("AbrirJanela");

        yield return new WaitForSeconds(0.1f);

        if (!EntregaPendente) yield break;

        podereceber = true;
        ativoParaEntrega = true;

        StartCoroutine(ExibirExclamacaoTemporaria());
        entregavelPisca?.PiscarAtivo();  

        Debug.Log("Janela próxima — pode entregar!");

        // Mantém sua janela de entrega normal
        yield return new WaitForSeconds(tempoAtivoEntrega);

        if (!recebeu && RegistrarFalhaEntrega())
        {
            entregavelPisca?.PararPiscar();
            sr.color = corNormal;
            podereceber = false;
            ativoParaEntrega = false;

            if (anim != null)
                anim.SetTrigger("FalhouEntrega");
        }
    }


    private IEnumerator ExibirExclamacaoTemporaria()
    {
        GameObject prefab = Resources.Load<GameObject>("PontoExclamacao");
        if (prefab == null) yield break;

        GameObject instancia = Instantiate(prefab, Exclamacao.position, Quaternion.identity);
        instancia.transform.SetParent(transform, true);

        yield return new WaitForSeconds(tempoExclamacao);

        Destroy(instancia);

    }

    private IEnumerator EsperarAnimacaoDepoisTransparente()
    {
        // Garantir que entrou no estado certo
        yield return null;
        entregavelPisca?.PararPiscar();
        // Duração real da animação atual
        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
        float duracao = info.length;

        float delayFinal = 0.5f; 

        // Esperar animação e delay
        yield return new WaitForSeconds(duracao + delayFinal);

        // transparente
        Color cor = sr.color;
        cor.a = 0.5f;
        sr.color = cor;

    }

    public void AplicarDificuldade(float multiplicadorGlobal)
    {
        float fatorMovimento = CalculoDificuldade.CalcularFator(multiplicadorGlobal, intensidadeEscalaMovimento, fatorMaximoMovimento);
        velocidade = velocidadeBase * fatorMovimento;
    }

}
