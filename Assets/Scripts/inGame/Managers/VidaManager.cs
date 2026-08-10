using UnityEngine;
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

    private bool invulneravel = false;
    private bool cheat = false;
    [SerializeField] private float tempoInvulneravel = 0.75f;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
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
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.P))
        {
            cheat = true;
            PerderVida();
        }
        // Atalho de debug: ganha 1 de vida com Shift + V
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.V))
        {
            GanharVida();
        }
    }

    public void ResetarVidas()
    {
        vidasAtuais = vidasIniciais;
        OnVidaMudou?.Invoke(vidasAtuais);
    }

    public void PerderVida()
    {
        // evita perder vida se já estiver invulnerável ou morto
        if (invulneravel || vidasAtuais <= 0)
            return;
        vidasAtuais--;
        anim.SetBool("Damage", true);
        OnVidaMudou?.Invoke(vidasAtuais);
        StartCoroutine(DelayVida());
        if (vidasAtuais <= 0)
        {
            Debug.Log("GAME OVER!");
            OnGameOver?.Invoke();
        }
        else
        {
            if (cheat)
            {
                cheat = false;
                return;
            }
            StartCoroutine(InvulnerabilidadeTemporaria());
        }
    }

    public void GanharVida()
    {
        vidasAtuais++;
        OnVidaMudou?.Invoke(vidasAtuais);
    }
    private IEnumerator DelayVida()
    {
        yield return new WaitForSeconds(0.5f);
        anim.SetBool("Damage", false);
    }

    private IEnumerator InvulnerabilidadeTemporaria()
    {
        invulneravel = true;
        yield return new WaitForSeconds(tempoInvulneravel);
        invulneravel = false;
    }

}
