using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;
using UnityEngine.Rendering;

public class VidaManager : MonoBehaviour
{
    public static VidaManager instance;

    [Header("Configuração de Vidas")]
    public int vidasIniciais = 3;
    [HideInInspector] public int vidasAtuais;

    public event Action<int> OnVidaMudou; // evento para HUD
    public event Action OnGameOver;
    [SerializeField, Min(1)] private int _perdasSeguidasParaDano = 3;
    private int _entregasPerdidasSeguidas;
    public int EntregasPerdidasSeguidas => _entregasPerdidasSeguidas;
    public int PerdasRestantesParaDano => Mathf.Max(0, _perdasSeguidasParaDano - _entregasPerdidasSeguidas);
    public event Action<int> PerdasSeguidasAlteradas;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private Color corOriginal;
    private Coroutine rotinaInvulnerabilidade;
    private bool piscandoDuranteInvulnerabilidade;
    private float inicioPiscar;
    private Transform visualDano;
    private Vector3 escalaOriginalVisual;
    private bool pulsandoDano;
    private float inicioPulsoDano;

    private bool invulneravel = false;
    [SerializeField] private float tempoInvulneravel = 0.6f;
    [SerializeField] private float tempoVermelhoDano = 0.12f;
    [SerializeField] private float intervaloPiscar = 0.08f;
    [SerializeField, Range(0f, 1f)] private float alphaInvulneravel = 0.3f;
    [SerializeField, Min(1f)] private float fatorEscalaDano = 1.12f;
    [SerializeField, Min(1)] private int quantidadePulsosDano = 2;
    [SerializeField, Min(0.01f)] private float duracaoPulsoDano = 0.16f;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        spriteRenderer = anim != null ? anim.GetComponent<SpriteRenderer>() : null;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            corOriginal = spriteRenderer.color;
            visualDano = spriteRenderer.transform;
            escalaOriginalVisual = visualDano.localScale;
        }
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        ResetarVidas();
    }
    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (BloqueioGameplay.Bloqueado) return;

        // Atalho de debug: perder 1 de vida com Shift + P
        if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed && Keyboard.current.pKey.wasPressedThisFrame)
        {
            ProcessarPerdaVida(false, false, registrarDano: false);
        }
        // Atalho de debug: ganha 1 de vida com Shift + V
        if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed && Keyboard.current.vKey.wasPressedThisFrame)
        {
            GanharVida();
        }
#endif
    }

    private void LateUpdate()
    {
        if (BloqueioGameplay.Bloqueado) return;
        if (piscandoDuranteInvulnerabilidade && spriteRenderer != null)
        {
            float intervaloSeguro = Mathf.Max(0.01f, intervaloPiscar);
            bool transparente = Mathf.FloorToInt(
                (TempoGameplay.Tempo - inicioPiscar) / intervaloSeguro) % 2 == 0;
            Color corAtual = corOriginal;
            corAtual.a = transparente ? alphaInvulneravel : corOriginal.a;
            spriteRenderer.color = corAtual;
        }

        if (pulsandoDano)
            AtualizarEscalaDano(TempoGameplay.Tempo - inicioPulsoDano);
    }

    public void ResetarVidas()
    {
        RegistrarEntregaBemSucedida();
        vidasAtuais = vidasIniciais;
        OnVidaMudou?.Invoke(vidasAtuais);
    }

    public void PerderVida(bool resetarCombo = true)
    {
        PerderVidas(1, resetarCombo);
    }

    public void PerderVidas(int quantidade, bool resetarCombo = true)
    {
        ProcessarPerdaVida(resetarCombo, true, quantidade);
    }

    public void PerderVidaPorEntrega()
    {
        if (BloqueioGameplay.Bloqueado || vidasAtuais <= 0) return;
        _entregasPerdidasSeguidas++;
        if (_entregasPerdidasSeguidas < _perdasSeguidasParaDano)
        {
            PerdasSeguidasAlteradas?.Invoke(_entregasPerdidasSeguidas);
            return;
        }
        RegistrarEntregaBemSucedida();
        // As tres perdas formam uma penalidade propria, mesmo durante o piscar de um contato.
        ProcessarPerdaVida(false, true, registrarDano: false, respeitarInvulnerabilidade: false);
    }

    public void RegistrarEntregaBemSucedida()
    {
        _entregasPerdidasSeguidas = 0;
        PerdasSeguidasAlteradas?.Invoke(0);
    }

    private void ProcessarPerdaVida(bool resetarCombo, bool aplicarInvulnerabilidade, int quantidade = 1, bool registrarDano = true, bool respeitarInvulnerabilidade = true)
    {
        // evita perder vida se já estiver invulnerável ou morto
        if (BloqueioGameplay.Bloqueado || (respeitarInvulnerabilidade && invulneravel) || vidasAtuais <= 0)
            return;
        if (rotinaInvulnerabilidade != null)
        {
            StopCoroutine(rotinaInvulnerabilidade);
            rotinaInvulnerabilidade = null;
            FinalizarFeedbackDano();
        }
        vidasAtuais = Mathf.Max(0, vidasAtuais - Mathf.Max(1, quantidade));
        if (registrarDano)
            ScoreManager.instance?.RegistrarDanoRecebido();
        if (vidasAtuais <= 0)
        {
            ScoreManager.instance?.EncerrarRegistroPartida();
            BloqueioGameplay.Definir(MotivoBloqueioGameplay.GameOver, true);
        }
        if (resetarCombo)
            ComboManager.instance?.ResetarCombo();

        OnVidaMudou?.Invoke(vidasAtuais);
        if (vidasAtuais <= 0)
        {
            FinalizarFeedbackDano();
            Debug.Log("GAME OVER!");
            OnGameOver?.Invoke();
            return;
        }

        if (aplicarInvulnerabilidade)
        {
            rotinaInvulnerabilidade = StartCoroutine(InvulnerabilidadeTemporaria());
        }
        else
        {
            FinalizarFeedbackDano();
        }
    }

    public void GanharVida()
    {
        vidasAtuais++;
        OnVidaMudou?.Invoke(vidasAtuais);
    }
    private IEnumerator InvulnerabilidadeTemporaria()
    {
        invulneravel = true;
        float inicioInvulnerabilidade = TempoGameplay.Tempo;
        float duracaoVermelho = Mathf.Min(tempoVermelhoDano, tempoInvulneravel);
        bool encerrouAnimacaoDano = false;
        pulsandoDano = visualDano != null;
        inicioPulsoDano = TempoGameplay.Tempo;

        if (anim != null)
            anim.SetBool("Damage", true);

        while (TempoGameplay.Tempo - inicioInvulnerabilidade < tempoInvulneravel)
        {
            if (BloqueioGameplay.Bloqueado)
            {
                yield return null;
                continue;
            }
            float tempoDecorrido = TempoGameplay.Tempo - inicioInvulnerabilidade;
            if (!encerrouAnimacaoDano && tempoDecorrido >= duracaoVermelho)
            {
                encerrouAnimacaoDano = true;
                if (anim != null)
                    anim.SetBool("Damage", false);

                piscandoDuranteInvulnerabilidade = true;
                inicioPiscar = TempoGameplay.Tempo;
            }
            yield return null;
        }

        FinalizarFeedbackDano();
        invulneravel = false;
        rotinaInvulnerabilidade = null;
    }

    private void AtualizarEscalaDano(float tempoDecorrido)
    {
        if (visualDano == null) return;

        int quantidadePulsos = Mathf.Max(1, quantidadePulsosDano);
        float duracaoTotal = Mathf.Min(
            tempoInvulneravel,
            Mathf.Max(0.01f, duracaoPulsoDano) * quantidadePulsos);
        if (tempoDecorrido >= duracaoTotal)
        {
            visualDano.localScale = escalaOriginalVisual;
            pulsandoDano = false;
            return;
        }

        float progresso = Mathf.Clamp01(tempoDecorrido / duracaoTotal);
        float pulso = Mathf.Abs(Mathf.Sin(progresso * Mathf.PI * quantidadePulsos));
        float escala = Mathf.Lerp(1f, Mathf.Max(1f, fatorEscalaDano), pulso);
        visualDano.localScale = escalaOriginalVisual * escala;
    }

    private void FinalizarFeedbackDano()
    {
        piscandoDuranteInvulnerabilidade = false;
        pulsandoDano = false;

        if (anim != null)
        {
            anim.SetBool("Damage", false);
        }

        if (spriteRenderer != null)
            spriteRenderer.color = corOriginal;

        if (visualDano != null)
            visualDano.localScale = escalaOriginalVisual;
    }

    private void OnDisable()
    {
        if (rotinaInvulnerabilidade != null)
        {
            StopCoroutine(rotinaInvulnerabilidade);
            rotinaInvulnerabilidade = null;
        }

        invulneravel = false;
        FinalizarFeedbackDano();
    }

    private void OnValidate()
    {
        tempoInvulneravel = Mathf.Max(0.05f, tempoInvulneravel);
        tempoVermelhoDano = Mathf.Clamp(tempoVermelhoDano, 0f, tempoInvulneravel);
        intervaloPiscar = Mathf.Max(0.01f, intervaloPiscar);
        alphaInvulneravel = Mathf.Clamp01(alphaInvulneravel);
        fatorEscalaDano = Mathf.Max(1f, fatorEscalaDano);
        quantidadePulsosDano = Mathf.Max(1, quantidadePulsosDano);
        duracaoPulsoDano = Mathf.Max(0.01f, duracaoPulsoDano);
        _perdasSeguidasParaDano = Mathf.Max(1, _perdasSeguidasParaDano);
    }

}
