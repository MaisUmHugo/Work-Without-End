using UnityEngine;
using System.Collections;

public class Motorista_Assustado : Entregavel
{
    public Transform Exclamacao;
    [Header("Configuração Motorista")]
    public float velocidade;
    public float tempoAtivoEntrega;
    public float intervaloPiscar;
    public float distanciaEntrega;
    public float tempoexclamacao;

    private SpriteRenderer sr;
    //public Color corNormal = Color.white; // cor padrão
    //public Color corAtivo = Color.red;    // cor quando está ativo para receber entrega
    private bool coroutineIniciada = false;
    private Mov jogador;
    private bool recebeu, podereceber;
    private Animator anim;

    [Header("Efeito Visual")]
    public EntregavelPisca entregavelPisca;
    public PontuacaoPopup popupPontuacao;
    private void Awake()
    {
        //sr = GetComponent<SpriteRenderer>();
        sr = GetComponentInChildren<SpriteRenderer>();
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
        //sr.color = corNormal;
        base.ReceberEntrega();

        entregavelPisca?.PiscarRecebendo();

        // Calcula pontuação com bônus
        int multiplicador = ComboManager.instance.GetMultiplicador();
        int total = 100 * multiplicador;

        popupPontuacao?.MostrarPontuacao(total);
        Color cor = sr.color;
        cor.a = 0.5f; // meio transparente
        sr.color = cor;
        anim.SetTrigger("ReceberEntrega");


        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false; // desliga colisão
        }
        recebeu = true;

        StartCoroutine(PararPiscar());
    }
    private System.Collections.IEnumerator ProntoparaEntrega()
    {
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

        if (ativoParaEntrega && !recebeu)
        {
            // não recebeu a entrega → falha
            anim.SetTrigger("FalhouEntrega");
            PerderCombo();
            PararPiscar();
        }
    }
    private IEnumerator PararPiscar()
    {
        yield return new WaitForSeconds(0.75f);
        entregavelPisca?.PararPiscar();
    }
}