using UnityEngine;

public abstract class Entregavel : MonoBehaviour
{
    [Header("Configuração do Entregável")]
    public bool ativoParaEntrega = true; // booleana para alternar quando pode entregar ou n
    protected int pontosBase = 100;
    public virtual void ReceberEntrega()
    {
        if (!ativoParaEntrega) return;
        Debug.Log("Base foi ativada");
        // Pontuação e combo
        ProcessarEntrega();

        Debug.Log($"{gameObject.name} recebeu a entrega!");
    }

    protected int ProcessarEntrega()
    {
        if (!ativoParaEntrega) return 0;

        int multiplicador = ComboManager.instance.GetMultiplicador();
        int pontosFinais = pontosBase * multiplicador;

        ScoreManager.instance.AdicionarPontos(pontosFinais);
        ComboManager.instance.AumentarCombo();
        if (HordaManager.instance != null)
            HordaManager.instance.AumentarEntrega();

        return pontosFinais;
    }

    public virtual void FalharEntrega()
    {
        // Reset de combo e vida
        VidaManager.instance.PerderVida();

        Debug.Log($"{gameObject.name} NÃO recebeu a entrega!");
    }

    public virtual void PerderCombo()
    {
        // Reset Combo
        ComboManager.instance.ResetarCombo();
    }
}
