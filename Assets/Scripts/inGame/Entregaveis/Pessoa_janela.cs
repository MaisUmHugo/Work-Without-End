using UnityEngine;
using System.Collections;

public class Pessoa_janela : Entregavel
{
    [Header("Configuração da Janela")]
    public float velocidade;
    public float tempoAtivoEntrega;
    public float intervaloPiscar;
    public float distanciaEntrega;
    public Vector3 offset;

    private SpriteRenderer sr;
    public Color corNormal = Color.blue; // cor padrão
    public Color corAtivo = Color.red;    // cor quando está ativo para receber entrega
    private bool coroutineIniciada = false;
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
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Caixa") && podereceber)
        {
            ReceberEntrega();
        }
    }
    public override void ReceberEntrega()
    {
        base.ReceberEntrega();

        entregavelPisca?.PiscarRecebendo();
        popupPontuacao?.MostrarPontuacao(100 * ComboManager.instance.GetMultiplicador());

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

        podereceber = true;
        ativoParaEntrega = true;

        StartCoroutine(ExibirExclamacaoTemporaria());
        entregavelPisca?.PiscarAtivo();  

        Debug.Log("Janela próxima — pode entregar!");

        // Mantém sua janela de entrega normal
        yield return new WaitForSeconds(tempoAtivoEntrega);

        if (ativoParaEntrega && !recebeu)
        {
            PerderCombo();
            sr.color = corNormal;
            podereceber = false;

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

        yield return new WaitForSeconds(1.2f);

        Destroy(instancia);

        entregavelPisca?.PararPiscar();
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

}
