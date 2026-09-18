using System;
using UnityEngine;

[Serializable]
public class ConfiguracaoFaseNecromante
{
    [SerializeField] private FaseBoss _fase = FaseBoss.Fase1;
    [SerializeField] private AtaqueBossBase[] _ataquesDisponiveis;
    [Header("Invocacao")]
    [SerializeField, Min(1)] private int _quantidadeOndas = 1;
    [SerializeField, Min(1)] private int _limiteInvocados = 3;
    [SerializeField, Range(0f, 1f)] private float _chanceZumbiSombrio;
    [SerializeField] private bool _garantirZumbiSombrio;

    public FaseBoss Fase => _fase;
    public AtaqueBossBase[] AtaquesDisponiveis => _ataquesDisponiveis;
    public int QuantidadeOndas => Mathf.Max(1, _quantidadeOndas);
    public int LimiteInvocados => Mathf.Max(1, _limiteInvocados);
    public float ChanceZumbiSombrio => Mathf.Clamp01(_chanceZumbiSombrio);
    public bool GarantirZumbiSombrio => _garantirZumbiSombrio;

    public bool Validar(GameObject raiz)
    {
        if (_ataquesDisponiveis == null || _ataquesDisponiveis.Length == 0)
            return false;

        for (int i = 0; i < _ataquesDisponiveis.Length; i++)
        {
            AtaqueBossBase ataque = _ataquesDisponiveis[i];
            if (ataque == null || ataque.gameObject != raiz)
                return false;
        }

        return true;
    }
}
