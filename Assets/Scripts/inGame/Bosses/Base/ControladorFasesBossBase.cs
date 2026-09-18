using System;
using UnityEngine;

public abstract class ControladorFasesBossBase : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private VidaBoss _vida;
    [Header("Limites de vida normalizados")]
    [SerializeField, Range(0f, 1f)] private float _inicioFase2 = 0.66f;
    [SerializeField, Range(0f, 1f)] private float _inicioFase3 = 0.33f;
    [SerializeField] private FaseBoss _faseMaxima = FaseBoss.Fase3;

    [SerializeField, Tooltip("Fase exibida para depuracao durante o Play Mode.")]
    private FaseBoss _faseAtual = FaseBoss.Fase1;
    private bool _faseAplicada;

    public VidaBoss Vida => _vida;
    public FaseBoss FaseAtual => _faseAtual;
    public FaseBoss FaseMaxima => _faseMaxima;
    public float InicioFase2 => _inicioFase2;
    public float InicioFase3 => _inicioFase3;
    public float PercentualVida => _vida == null || _vida.VidaMaxima <= 0
        ? 0f
        : (float)_vida.VidaAtual / _vida.VidaMaxima;

    public event Action<FaseBoss> FaseAlterada;

    protected virtual void OnEnable()
    {
        if (_vida != null)
        {
            _vida.VidaAlterada += AoAlterarVida;
            if (_faseAplicada)
                AvaliarFase(_vida.VidaAtual, _vida.VidaMaxima, false);
        }
    }

    protected virtual void Start()
    {
        if (!ValidarReferencias())
        {
            enabled = false;
            return;
        }

        AvaliarFase(_vida.VidaAtual, _vida.VidaMaxima, true);
    }

    protected virtual void OnDisable()
    {
        if (_vida != null)
            _vida.VidaAlterada -= AoAlterarVida;
    }

    public void ReiniciarFases()
    {
        AplicarFase(FaseBoss.Fase1, true);
    }

    public void DefinirFaseMaxima(FaseBoss faseMaxima)
    {
        _faseMaxima = LimitarFase(faseMaxima);
        if (_faseAplicada && _vida != null)
            AvaliarFase(_vida.VidaAtual, _vida.VidaMaxima, false);
    }

    public void DefinirLimites(float inicioFase2, float inicioFase3)
    {
        _inicioFase2 = Mathf.Clamp01(inicioFase2);
        _inicioFase3 = Mathf.Clamp(inicioFase3, 0f, _inicioFase2);

        if (_faseAplicada && _vida != null)
            AvaliarFase(_vida.VidaAtual, _vida.VidaMaxima, false);
    }

    private void AoAlterarVida(int vidaAtual, int vidaMaxima)
    {
        AvaliarFase(vidaAtual, vidaMaxima, false);
    }

    private void AvaliarFase(int vidaAtual, int vidaMaxima, bool forcarAplicacao)
    {
        if (vidaMaxima <= 0) return;

        float percentualVida = Mathf.Clamp01((float)vidaAtual / vidaMaxima);
        FaseBoss fase = FaseBoss.Fase1;

        if ((int)_faseMaxima >= (int)FaseBoss.Fase3 && percentualVida <= _inicioFase3)
            fase = FaseBoss.Fase3;
        else if ((int)_faseMaxima >= (int)FaseBoss.Fase2 && percentualVida <= _inicioFase2)
            fase = FaseBoss.Fase2;

        AplicarFase(fase, forcarAplicacao);
    }

    private void AplicarFase(FaseBoss fase, bool forcarAplicacao)
    {
        fase = (int)fase > (int)_faseMaxima ? _faseMaxima : fase;
        if (!forcarAplicacao && _faseAplicada && _faseAtual == fase) return;

        _faseAtual = fase;
        _faseAplicada = true;
        AoAplicarFase(fase);
        FaseAlterada?.Invoke(fase);
    }

    protected abstract void AoAplicarFase(FaseBoss fase);

    protected virtual bool ValidarReferencias()
    {
        if (_vida != null && _vida.gameObject == gameObject)
            return true;

        Debug.LogError("Boss: configure a vida da mesma raiz no controlador de fases.", this);
        return false;
    }

    protected virtual void OnValidate()
    {
        _inicioFase2 = Mathf.Clamp01(_inicioFase2);
        _inicioFase3 = Mathf.Clamp(_inicioFase3, 0f, _inicioFase2);
        _faseMaxima = LimitarFase(_faseMaxima);
    }

    private static FaseBoss LimitarFase(FaseBoss fase)
    {
        return (FaseBoss)Mathf.Clamp((int)fase, (int)FaseBoss.Fase1, (int)FaseBoss.Fase3);
    }
}
