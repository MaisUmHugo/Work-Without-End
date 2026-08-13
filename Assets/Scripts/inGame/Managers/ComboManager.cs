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

    }

    public int GetMultiplicador()
    {
        // 0–9 = 1x, 10–19 = 2x, etc.
        return GetMultiplicadorParaCombo(comboAtual);
    }

    public int GetMultiplicadorParaCombo(int combo)
    {
        return (Mathf.Max(0, combo) / 10) + 1;
    }

}
