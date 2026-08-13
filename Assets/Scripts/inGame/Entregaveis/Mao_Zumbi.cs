using UnityEngine;
using System.Collections;

public class Mao_Zumbi : Entregavel, IAjustavelDificuldade
{
    public Transform Exclamacao;
    [Header("Configuração da mão")]
    public float velocidade;
    public float intervaloPiscar;
    public float distanciaEntrega;
    public float tempoexclamacao;

    [Header("Dificuldade")]
    [SerializeField, Range(0f, 1f)] private float intensidadeEscalaMovimento = 0.4f;
    [SerializeField, Min(1f)] private float fatorMaximoMovimento = 8.5f;
    private float velocidadeBase;

    // Sprite renderer para fazer o efeito de piscar
    private SpriteRenderer sr;
    public Color corNormal = Color.white; // cor padrão
    public Color corAtivo = Color.red;    // cor quando está ativo para receber entrega
    private bool coroutineIniciada = false;
    private bool emFluxoDeSaida = false;
    private Mov jogador;
    private bool recebeu, podereceber;
    private bool jaCausouDano;
    private float xAnterior;
    public EntregavelPisca entregavelPisca;
    public PontuacaoPopup popupPontuacao;

    [Header("Ajuste de posição")]
    public float offsetY = 0f;

    private Animator anim;
    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        velocidadeBase = velocidade;

        if (entregavelPisca == null)
            entregavelPisca = GetComponent<EntregavelPisca>();

        if (entregavelPisca == null)
            Debug.LogWarning(name + ": componente EntregavelPisca não encontrado.", this);

        anim = GetComponentInChildren<Animator>();
    }
    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            jogador = playerObj.GetComponent<Mov>();
        else
            Debug.LogWarning("Player não encontrado! Verifique a tag 'Player'.");

        LanesController.Linhas laneEscolhida =
            (LanesController.Linhas)Random.Range(0, 4);

        Debug.Log($"Mão Zumbi spawnou na LANE: {laneEscolhida}");

        Vector3 pos = transform.position;
        pos.y = LanesController.instance.PosicaoY(laneEscolhida) + offsetY;
        transform.position = pos;
        xAnterior = transform.position.x;

        Debug.Log($" Y da lane = {LanesController.instance.PosicaoY(laneEscolhida)} | " +
                  $"OffsetY = {offsetY} | Y final = {transform.position.y}");
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
        if (EntregaPendente && !coroutineIniciada && EntrouNoAlcance(CalcularDistanciaInicioTelegraph()))
        {
            coroutineIniciada = true;
            emFluxoDeSaida = true;
            StartCoroutine(ProntoparaEntrega());
        }

        xAnterior = transform.position.x;

        // --- MOVIMENTO ---
        if (emFluxoDeSaida)
        {
            transform.position += Vector3.left * velocidade * Time.deltaTime;
        }
        else
        {
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
            return;
        }

        if (transform.position.x < jogador.transform.position.x - 20f)
        {
            if (EntregaPendente)
                RegistrarFalhaEntrega();

            Destroy(gameObject);
            return;
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

        int pontosRecebidos = ProcessarEntrega();
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false; // desliga colisão
        }
        recebeu = true;
        anim.SetTrigger("ReceberEntrega");
        //entregavelPisca.PararPiscar();
        // Calcula pontuação com bônus
        popupPontuacao?.MostrarPontuacao(pontosRecebidos);
        entregavelPisca?.PiscarRecebendo();
        StartCoroutine(DelayTransparente());
        StartCoroutine(PararPiscar());
    }
    private IEnumerator ProntoparaEntrega()
    {
        anim.SetTrigger("Surgir");
        yield return new WaitForSeconds(1.5f);


        if (!EntregaPendente) yield break;

        podereceber = true;
        ativoParaEntrega = true;
        anim.SetTrigger("MaoAberta");
        entregavelPisca?.PiscarAtivo();

        GameObject prefab = Resources.Load<GameObject>("PontoExclamacao");
        if (prefab != null)
        {
            GameObject instancia = Instantiate(prefab, Exclamacao.position, Quaternion.identity);
            instancia.transform.SetParent(gameObject.transform, worldPositionStays: true);

            yield return new WaitForSeconds(tempoexclamacao);
            Destroy(instancia);
        }

        Debug.Log("Mao proxima, entregue agora!");
    }

    private IEnumerator DelayTransparente()
    {
        yield return new WaitForSeconds(1.25f);
        anim.SetTrigger("Transparente");
    }

    private IEnumerator PararPiscar()
    {
        yield return new WaitForSeconds(1.5f);
        entregavelPisca?.PararPiscar();
    }
    private float CalcularDistanciaInicioTelegraph()
    {
        const float tempoTelegraph = 1.5f;
        return distanciaEntrega + velocidade * tempoTelegraph;
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
