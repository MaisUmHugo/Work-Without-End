using System.Collections.Generic;
using UnityEngine;

public readonly struct ParLanes
{
    public int Primeira { get; }
    public int Segunda { get; }

    public ParLanes(int primeira, int segunda)
    {
        Primeira = primeira;
        Segunda = segunda;
    }

    public bool Contem(int indiceLane)
    {
        return Primeira == indiceLane || Segunda == indiceLane;
    }
}

public static class SeletorParLanes
{
    public static bool TentarSortear(
        int quantidadeLanes,
        int laneAlvo,
        float chanceIncluirAlvo,
        out ParLanes par)
    {
        par = default;
        if (quantidadeLanes < 2) return false;

        var contendoAlvo = new List<int>();
        var semAlvo = new List<int>();

        for (int primeira = 0; primeira < quantidadeLanes - 1; primeira++)
        {
            bool contemAlvo = primeira == laneAlvo || primeira + 1 == laneAlvo;
            (contemAlvo ? contendoAlvo : semAlvo).Add(primeira);
        }

        bool incluirAlvo = laneAlvo >= 0 && laneAlvo < quantidadeLanes
            && Random.value < Mathf.Clamp01(chanceIncluirAlvo);
        List<int> candidatos = incluirAlvo ? contendoAlvo : semAlvo;

        if (candidatos.Count == 0)
            candidatos = IncluirFallback(contendoAlvo, semAlvo);
        if (candidatos.Count == 0) return false;

        int inicio = candidatos[Random.Range(0, candidatos.Count)];
        par = new ParLanes(inicio, inicio + 1);
        return true;
    }

    private static List<int> IncluirFallback(List<int> contendoAlvo, List<int> semAlvo)
    {
        return contendoAlvo.Count > 0 ? contendoAlvo : semAlvo;
    }
}
