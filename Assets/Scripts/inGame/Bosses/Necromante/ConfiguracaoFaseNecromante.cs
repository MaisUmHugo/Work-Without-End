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
    [Header("Ritmo da fase")]
    [SerializeField, Min(0.1f)] private float _multiplicadorDecisao = 1f;
    [SerializeField, Min(0.1f)] private float _multiplicadorRitmoAtaques = 1f;
    [SerializeField, Min(0.1f)] private float _multiplicadorVelocidadeProjeteis = 1f;
    [SerializeField, Min(0.1f)] private float _multiplicadorVelocidadeInvocados = 1f;
    [SerializeField, Min(1)] private int _limitePressaoAmbiente = 2;
    [SerializeField] private Vector2 _intervaloPressaoAmbiente = new Vector2(3.5f, 5.5f);

    public FaseBoss Fase => _fase;
    public AtaqueBossBase[] AtaquesDisponiveis => _ataquesDisponiveis;
    public int QuantidadeOndas => Mathf.Max(1, _quantidadeOndas);
    public int LimiteInvocados => Mathf.Max(1, _limiteInvocados);
    public float ChanceZumbiSombrio => Mathf.Clamp01(_chanceZumbiSombrio);
    public bool GarantirZumbiSombrio => _garantirZumbiSombrio;
    public float MultiplicadorDecisao => Mathf.Max(0.1f, _multiplicadorDecisao);
    public float MultiplicadorRitmoAtaques => Mathf.Max(0.1f, _multiplicadorRitmoAtaques);
    public float MultiplicadorVelocidadeProjeteis => Mathf.Max(0.1f, _multiplicadorVelocidadeProjeteis);
    public float MultiplicadorVelocidadeInvocados => Mathf.Max(0.1f, _multiplicadorVelocidadeInvocados);
    public int LimitePressaoAmbiente => Mathf.Max(1, _limitePressaoAmbiente);
    public Vector2 IntervaloPressaoAmbiente => _intervaloPressaoAmbiente;

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
