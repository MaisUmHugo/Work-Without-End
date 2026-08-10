using UnityEngine;
using System;

public class ComboManager : MonoBehaviour
{
    public static ComboManager instance;

    public int comboAtual { get; private set; }
    public int melhorCombo { get; private set; }

    public event Action<int> OnComboMudou;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public void ResetarCombo()
    {
        comboAtual = 0;
        OnComboMudou?.Invoke(comboAtual);
    }

    public void AumentarCombo()
    {
        comboAtual++;
        melhorCombo = Mathf.Max(melhorCombo, comboAtual);

        OnComboMudou?.Invoke(comboAtual);

        // bônus a cada 20 entregas
        if (comboAtual % 20 == 0)
        {
            VidaManager.instance.GanharVida();
            Debug.Log("Ganhou 1 Vida pelo combo!");
        }
    }

    public int GetMultiplicador()
    {
        // 0–9 = 1x, 10–19 = 2x, etc.
        return (comboAtual / 10) + 1;
    }

}
