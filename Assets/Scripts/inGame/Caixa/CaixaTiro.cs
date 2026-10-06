using UnityEngine;
using UnityEngine.InputSystem;

public class CaixaTiro : MonoBehaviour
{
    [Header("Configuração do Tiro")]
    public GameObject prefabCaixa; // Prefab da caixa
    public Transform pontoLancamento; // De onde a caixa sai (pode ser a posição do jogador)
    public float velocidadeCaixa;
    public float delayLancamento;

    private INPUTS inputs;
    private Camera cam;
    private Mira mira;
    private float tempoUltimoLancamento;
    private Animator anim;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        inputs = new INPUTS();
        cam = Camera.main;
        mira = FindFirstObjectByType<Mira>();
        if (pontoLancamento == null) pontoLancamento = transform;
        if (mira != null) mira.DefinirOrigemLancamento(pontoLancamento);
    }

    private void OnEnable()
    {
        inputs.Gameplay.Enable();
        inputs.Gameplay.Shoot.performed += Atirar; // ação no Input Actions
    }

    private void OnDisable()
    {
        inputs.Gameplay.Shoot.performed -= Atirar;
        inputs.Gameplay.Disable();
    }

    public void SpawnCaixa()
    {
        if (BloqueioGameplay.Bloqueado) return;
        // Pega posição da mira
        if (mira == null || prefabCaixa == null) return;
        Vector3 posMira = mira.ObterPosicaoAlvo(pontoLancamento);

        // Direção da caixa (da origem até a mira)
        Vector2 direcao = ((Vector2)(posMira - pontoLancamento.position)).normalized;

        // Instancia a caixa
        GameObject novaCaixa = Instantiate(prefabCaixa, pontoLancamento.position, Quaternion.identity);

        // Adiciona movimento
        Rigidbody2D rb = novaCaixa.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = direcao * velocidadeCaixa;
    }

    private void Atirar(InputAction.CallbackContext ctx)
    {
        if (BloqueioGameplay.Bloqueado) return;
        if (TempoGameplay.Tempo < tempoUltimoLancamento + delayLancamento) return;
        if (prefabCaixa == null) return;

        anim.SetTrigger("Throw");
        tempoUltimoLancamento = TempoGameplay.Tempo;

        // Inicia cooldown visual
        StartCoroutine(CooldownVisual());
    }

    private System.Collections.IEnumerator CooldownVisual()
    {
        if (mira == null) yield break;

        mira.emCooldown = true;
        float t = 0;

        while (t < delayLancamento)
        {
            t += TempoGameplay.DeltaTime;
            mira.cooldownProgresso = Mathf.Clamp01(t / delayLancamento);
            yield return null;
        }

        mira.emCooldown = false;
        mira.cooldownProgresso = 0;
    }
}
