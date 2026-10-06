using System;
using System.Collections.Generic;
using UnityEngine;

public enum MotivoBloqueioGameplay
{
    Pause,
    Tutorial,
    GameOver,
    TransicaoCenario
}

public static class BloqueioGameplay
{
    private static readonly HashSet<MotivoBloqueioGameplay> motivosAtivos = new HashSet<MotivoBloqueioGameplay>();

    public static bool Bloqueado => motivosAtivos.Count > 0;
    public static event Action<bool> BloqueioAlterado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        motivosAtivos.Clear();
        BloqueioAlterado = null;
        TempoGameplay.Reiniciar();
    }

    public static bool BloqueadoSemTransicao
    {
        get
        {
            foreach (MotivoBloqueioGameplay motivo in motivosAtivos)
            {
                if (motivo != MotivoBloqueioGameplay.TransicaoCenario) return true;
            }
            return false;
        }
    }

    public static bool EstaAtivo(MotivoBloqueioGameplay motivo)
    {
        return motivosAtivos.Contains(motivo);
    }

    public static void Definir(MotivoBloqueioGameplay motivo, bool bloqueado)
    {
        bool estavaBloqueado = Bloqueado;
        if (bloqueado)
            motivosAtivos.Add(motivo);
        else
            motivosAtivos.Remove(motivo);
        if (estavaBloqueado != Bloqueado)
        {
            TempoGameplay.AtualizarBloqueio(Bloqueado);
            BloqueioAlterado?.Invoke(Bloqueado);
        }
    }
}
