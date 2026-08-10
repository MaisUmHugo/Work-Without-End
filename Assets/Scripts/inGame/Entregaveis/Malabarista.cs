using System.Collections;
using UnityEngine;

public class Malabarista : Entregavel
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


    [Header("Exclamação")]
    public Transform Exclamacao;
    public float tempoexclamacao;

    [Header("Cores")]
    private SpriteRenderer sr;
    public Color corNormal = Color.white;
    public Color corAtivo = Color.red;

    private bool coroutineIniciada = false;
    private bool recebeu = false;
    private bool podereceber = false;

    private bool podeatirar = false;
    private bool ComecouCoroutineAtirar = false;

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
        if (!ComecouCoroutineAtirar &&
            Mathf.Abs(transform.position.x - jogador.transform.position.x) <= DistanciaTiro)
        {
            ComecouCoroutineAtirar = true;
            podeatirar = true;
            StartCoroutine(atirar());
        }

        // SISTEMA DE ENTREGA
        if (!coroutineIniciada &&
            Mathf.Abs(transform.position.x - jogador.transform.position.x) <= distanciaEntrega)
        {
            coroutineIniciada = true;
            StartCoroutine(ProntoparaEntrega());
        }

        // MOVIMENTO
        if (ativoParaEntrega && !recebeu) // já está no modo de espera
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
            if (!recebeu)
                PerderCombo();

            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Caixa") && podereceber)
            ReceberEntrega();

        if (collision.CompareTag("Player"))
            FalharEntrega();
    }

    public override void ReceberEntrega()
    {
        base.ReceberEntrega();
        recebeu = true;
        anim.SetTrigger("RecebeuEntrega");
        int multiplicador = ComboManager.instance.GetMultiplicador();
        int total = 100 * multiplicador;

        popupPontuacao?.MostrarPontuacao(total);
        entregavelPisca?.PiscarRecebendo();

        // Desativa colisão
        Collider2D col = GetComponent<Collider2D>();
        if (col) col.enabled = false;

        StartCoroutine(DelayTransparente());
        StartCoroutine(PararPiscar());
    }

    private IEnumerator ProntoparaEntrega()
    {
        yield return new WaitForSeconds(0.1f);

        podereceber = true;
        ativoParaEntrega = true;

        StartCoroutine(exclamacao());
        entregavelPisca?.PiscarAtivo();

        yield return new WaitForSeconds(tempoAtivoEntrega);

        entregavelPisca?.PararPiscar();

        if (!recebeu)
        {
            PerderCombo();
            sr.color = corNormal;
        }
    }

    private IEnumerator DelayTransparente()
    {
        yield return new WaitForSeconds(1f);
        Color c = sr.color;
        c.a = 0.5f;
        sr.color = c;
    }

    // TIRO 
    public IEnumerator atirar()
    {
        if (!podeatirar || podereceber)
            yield break;
        
            Vector3 posSpawn =
                transform.position +
                Vector3.left * offsetBolaX +
                Vector3.up * offsetBolaY;

            GameObject novaBola = Instantiate(bola, posSpawn, Quaternion.identity);
            
            // deslocamento aleatório
            float randomOffsetX = Random.Range(10f, 25f);
            // destino da bola - Lane onde vai cair
            Vector3 destino = new Vector3(
                posSpawn.x - randomOffsetX, 
                LanesController.instance.PosicaoY(jogador.linhaAtual),
                0f
            );


            Bola b = novaBola.GetComponent<Bola>();
            b.CaminhoBola(destino);




            yield return new WaitForSeconds(IntervaloTiro);
            
            StartCoroutine(atirar());
        }

    private IEnumerator PararPiscar()
    {
        yield return new WaitForSeconds(1.5f);
        entregavelPisca?.PararPiscar();
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
}
