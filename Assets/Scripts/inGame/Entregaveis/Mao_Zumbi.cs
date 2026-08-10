using UnityEngine;
using System.Collections;

public class Mao_Zumbi : Entregavel
{
    public Transform Exclamacao;
    [Header("Configuração da mão")]
    public float velocidade;
    public float tempoAtivoEntrega;
    public float intervaloPiscar;
    public float distanciaEntrega;
    public float tempoexclamacao;

    // Sprite renderer para fazer o efeito de piscar
    private SpriteRenderer sr;
    public Color corNormal = Color.white; // cor padrão
    public Color corAtivo = Color.red;    // cor quando está ativo para receber entrega
    private bool coroutineIniciada = false;
    private Mov jogador;
    private bool recebeu, podereceber;
    public EntregavelPisca entregavelPisca;
    public PontuacaoPopup popupPontuacao;

    [Header("Ajuste de posição")]
    public float offsetY = 0f;

    private Animator anim;
    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        //sr = GetComponentInChildren<SpriteRenderer>();
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
        if (!coroutineIniciada && Mathf.Abs(transform.position.x - jogador.transform.position.x) <= distanciaEntrega)
        {
            coroutineIniciada = true;
            StartCoroutine(ProntoparaEntrega());
        }

        // --- MOVIMENTO ---
        if (ativoParaEntrega && !recebeu)
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
            if (!recebeu) // saiu sem receber -> falha
            {
                PerderCombo();
                Debug.Log($"{gameObject.name} saiu da tela sem entrega!");
            }
            else
            {
                Debug.Log($"{gameObject.name} saiu da tela após entrega.");
            }

            Destroy(gameObject);
        }

        if (transform.position.x < jogador.transform.position.x - 20f)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Caixa") && podereceber)
        {
            ReceberEntrega();
        }
        if (collision.CompareTag("Player"))
        {
            FalharEntrega();
        }
    }
    public override void ReceberEntrega()
    {
        base.ReceberEntrega();
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false; // desliga colisão
        }
        recebeu = true;
        anim.SetTrigger("ReceberEntrega");
        //entregavelPisca.PararPiscar();
        // Calcula pontuação com bônus
        int multiplicador = ComboManager.instance.GetMultiplicador();
        int total = 100 * multiplicador;

        popupPontuacao?.MostrarPontuacao(total);
        entregavelPisca?.PiscarRecebendo();
        StartCoroutine(DelayTransparente());
        StartCoroutine(PararPiscar());
    }
    private IEnumerator ProntoparaEntrega()
    {
        anim.SetTrigger("Surgir");
        yield return new WaitForSeconds(1.5f);
        podereceber = true;
        ativoParaEntrega = true;
        anim.SetTrigger("MaoAberta");
        entregavelPisca?.PiscarAtivo();
        // Exclamação
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
        Debug.Log("Mão proxima, entregue agora!");

        // espera a janela de tempo para aceitar a entrega
        yield return new WaitForSeconds(tempoAtivoEntrega);

       /* if (ativoParaEntrega && !recebeu)
        {
            // não recebeu a entrega → falha
            PerderCombo();
            sr.color = corNormal;
        }
       */
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
}
