using System;
using UnityEngine;

public interface IAjustavelDificuldade
{
    void AplicarDificuldade(float multiplicadorGlobal);
}

public static class CalculoDificuldade
{
    public static float CalcularFator(float multiplicadorGlobal, float intensidade, float fatorMaximo)
    {
        float progresso = Mathf.Max(0f, multiplicadorGlobal - 1f);
        float fator = 1f + progresso * Mathf.Max(0f, intensidade);
        return Mathf.Clamp(fator, 1f, Mathf.Max(1f, fatorMaximo));
    }
}

public abstract class Entregavel : MonoBehaviour
{
    private enum EstadoEntrega
    {
        Pendente,
        Sucesso,
        Falha
    }

    [Header("Configuração do Entregável")]
    public bool ativoParaEntrega = true;
    protected int pontosBase = 100;

    private EstadoEntrega estadoEntrega = EstadoEntrega.Pendente;

    protected bool EntregaPendente => estadoEntrega == EstadoEntrega.Pendente;
    public bool Resolvido => estadoEntrega != EstadoEntrega.Pendente;

    public event Action<Entregavel> EntregaResolvida;

    protected virtual void OnEnable()
    {
        PausaGameplay.Registrar(gameObject);
    }

    public virtual void ReceberEntrega()
    {
        if (BloqueioGameplay.Bloqueado || !ativoParaEntrega || !EntregaPendente) return;

        ProcessarEntrega();
        Debug.Log($"{gameObject.name} recebeu a entrega!");
    }

    protected int ProcessarEntrega(bool contabilizarNaHorda = true)
    {
        if (BloqueioGameplay.Bloqueado || !ativoParaEntrega || !EntregaPendente) return 0;

        estadoEntrega = EstadoEntrega.Sucesso;
        ativoParaEntrega = false;

        int comboAposEntrega = ComboManager.instance.comboAtual + 1;
        int multiplicador = ComboManager.instance.GetMultiplicadorParaCombo(comboAposEntrega);
        int pontosFinais = pontosBase * multiplicador;

        ScoreManager.instance.AdicionarPontos(pontosFinais);
        ComboManager.instance.AumentarCombo();
        ScoreManager.instance.RegistrarEntrega(ComboManager.instance.comboAtual);

        if (contabilizarNaHorda && HordaManager.instance != null)
            HordaManager.instance.AumentarEntrega();

        EntregaResolvida?.Invoke(this);
        return pontosFinais;
    }

    public virtual void FalharEntrega()
    {
        if (!RegistrarFalhaEntrega()) return;

        Debug.Log($"{gameObject.name} NÃO recebeu a entrega!");
    }

    public virtual void PerderCombo()
    {
        ComboManager.instance.ResetarCombo();
    }

    protected bool RegistrarFalhaEntrega(bool perderVida = true, bool porContato = false)
    {
        if (BloqueioGameplay.Bloqueado) return false;
        if (!MarcarEntregaComoFalha()) return false;

        ScoreManager.instance?.RegistrarEntregaPerdida();
        PerderCombo();
        if (perderVida)
        {
            if (porContato)
                VidaManager.instance?.PerderVida(false);
            else
                VidaManager.instance?.PerderVidaPorEntrega();
        }
        EntregaResolvida?.Invoke(this);
        return true;
    }

    private bool MarcarEntregaComoFalha()
    {
        if (!EntregaPendente) return false;

        estadoEntrega = EstadoEntrega.Falha;
        ativoParaEntrega = false;
        return true;
    }
}
