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

    public virtual void ReceberEntrega()
    {
        if (!ativoParaEntrega || !EntregaPendente) return;

        ProcessarEntrega();
        Debug.Log($"{gameObject.name} recebeu a entrega!");
    }

    protected int ProcessarEntrega()
    {
        if (!ativoParaEntrega || !EntregaPendente) return 0;

        estadoEntrega = EstadoEntrega.Sucesso;
        ativoParaEntrega = false;

        int comboAposEntrega = ComboManager.instance.comboAtual + 1;
        int multiplicador = ComboManager.instance.GetMultiplicadorParaCombo(comboAposEntrega);
        int pontosFinais = pontosBase * multiplicador;

        ScoreManager.instance.AdicionarPontos(pontosFinais);
        ComboManager.instance.AumentarCombo();

        if (HordaManager.instance != null)
            HordaManager.instance.AumentarEntrega();

        return pontosFinais;
    }

    public virtual void FalharEntrega()
    {
        if (estadoEntrega == EstadoEntrega.Sucesso) return;

        bool primeiraFalha = MarcarEntregaComoFalha();
        VidaManager.instance.PerderVida();

        if (primeiraFalha)
            Debug.Log($"{gameObject.name} NÃO recebeu a entrega!");
    }

    public virtual void PerderCombo()
    {
        ComboManager.instance.ResetarCombo();
    }

    protected bool RegistrarFalhaEntrega()
    {
        if (!MarcarEntregaComoFalha()) return false;

        PerderCombo();
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