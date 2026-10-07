using UnityEngine;

public class Caixa : MonoBehaviour
{
    [Header("Configuração")]
    public float tempoMaximo = 5f; // some depois de 5s se não colidir

    [HideInInspector] public string layerEntregavel = "Entregavel";
    private bool _consumida;
    private Rigidbody2D _corpo;
    private Collider2D _colisor;
    private readonly RaycastHit2D[] _contatosTrajeto = new RaycastHit2D[32];
    private void Awake()
    {
        _corpo = GetComponent<Rigidbody2D>();
        _colisor = GetComponent<Collider2D>();
        PausaGameplay.Registrar(gameObject);
    }

    private void FixedUpdate()
    {
        if (_consumida || BloqueioGameplay.Bloqueado || _corpo == null || _colisor == null
            || !_corpo.simulated || !_colisor.enabled) return;

        Vector2 deslocamento = _corpo.linearVelocity * TempoGameplay.FixedDeltaTime;
        if (deslocamento.sqrMagnitude <= 0f) return;

        // Triggers podem ser atravessados entre passos da fisica, mesmo com CCD ativo.
        ContactFilter2D filtro = new ContactFilter2D();
        filtro.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        filtro.useTriggers = true;
        int quantidade = _colisor.Cast(deslocamento.normalized, filtro, _contatosTrajeto, deslocamento.magnitude);
        ZumbiSombrio sombrioMaisProximo = null;
        float menorDistancia = float.MaxValue;
        for (int i = 0; i < quantidade; i++)
        {
            Collider2D contato = _contatosTrajeto[i].collider;
            if (contato == null || contato.CompareTag("Player")) continue;
            ZumbiSombrio sombrio = contato.GetComponentInParent<ZumbiSombrio>();
            if (sombrio == null && contato.isTrigger && contato.GetComponentInParent<Entregavel>() == null)
                continue;
            if (_contatosTrajeto[i].distance >= menorDistancia) continue;
            menorDistancia = _contatosTrajeto[i].distance;
            sombrioMaisProximo = sombrio;
        }
        // Outros destinatarios continuam usando suas colisoes; nao entrega atraves deles.
        sombrioMaisProximo?.TentarReceberCaixa(this);
    }

    private void Start()
    {
        StartCoroutine(TempoGameplay.DestruirDepois(gameObject, tempoMaximo));
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer(layerEntregavel))
        {
            // só some a caixa, quem recebeu decide se ganha ponto
            TentarConsumir();
        }
    }

    public bool TentarConsumir()
    {
        if (BloqueioGameplay.Bloqueado || _consumida) return false;

        _consumida = true;
        Destroy(gameObject);
        return true;
    }
}
