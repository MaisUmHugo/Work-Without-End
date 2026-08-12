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
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private Color corOriginal;
    private Coroutine rotinaInvulnerabilidade;
    private bool piscandoDuranteInvulnerabilidade;
    private float inicioPiscar;

    private bool invulneravel = false;
    [SerializeField] private float tempoInvulneravel = 0.75f;
    [SerializeField] private float tempoVermelhoDano = 0.15f;
    [SerializeField] private float intervaloPiscar = 0.1f;
    [SerializeField, Range(0f, 1f)] private float alphaInvulneravel = 0.3f;

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
        // Atalho de debug: perder 1 de vida com Shift + P
        if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed && Keyboard.current.pKey.wasPressedThisFrame)
        {
            ProcessarPerdaVida(false, false);
        }
        // Atalho de debug: ganha 1 de vida com Shift + V
        if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed && Keyboard.current.vKey.wasPressedThisFrame)
        {
            GanharVida();
        }
    }

    private void LateUpdate()
    {
        if (!piscandoDuranteInvulnerabilidade || spriteRenderer == null)
            return;

        float intervaloSeguro = Mathf.Max(0.01f, intervaloPiscar);
        bool transparente = Mathf.FloorToInt((Time.time - inicioPiscar) / intervaloSeguro) % 2 == 0;
        Color corAtual = corOriginal;
        corAtual.a = transparente ? alphaInvulneravel : corOriginal.a;
        spriteRenderer.color = corAtual;
    }

    public void ResetarVidas()
    {
        vidasAtuais = vidasIniciais;
        OnVidaMudou?.Invoke(vidasAtuais);
    }

    public void PerderVida(bool resetarCombo = true)
    {
        ProcessarPerdaVida(resetarCombo, true);
    }

    private void ProcessarPerdaVida(bool resetarCombo, bool aplicarInvulnerabilidade)
    {
        // evita perder vida se já estiver invulnerável ou morto
        if (invulneravel || vidasAtuais <= 0)
            return;
        vidasAtuais--;
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
        float inicioInvulnerabilidade = Time.time;

        if (anim != null)
        {
            anim.SetBool("Damage", true);
        }

        float duracaoVermelho = Mathf.Min(tempoVermelhoDano, tempoInvulneravel);
        yield return new WaitForSeconds(duracaoVermelho);

        if (anim != null)
        {
            anim.SetBool("Damage", false);
        }

        piscandoDuranteInvulnerabilidade = true;
        inicioPiscar = Time.time;

        while (Time.time - inicioInvulnerabilidade < tempoInvulneravel)
        {
            yield return null;
        }

        FinalizarFeedbackDano();
        invulneravel = false;
        rotinaInvulnerabilidade = null;
    }

    private void FinalizarFeedbackDano()
    {
        piscandoDuranteInvulnerabilidade = false;

        if (anim != null)
        {
            anim.SetBool("Damage", false);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = corOriginal;
        }
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

}
