using System;
using System.Collections.Generic;
using UnityEngine;

public enum MotivoBloqueioGameplay
{
    Pause,
    Tutorial,
    GameOver,
    TransicaoCenario,
    EntradaBoss
}

public static class BloqueioGameplay
{
    private static readonly HashSet<MotivoBloqueioGameplay> motivosAtivos = new HashSet<MotivoBloqueioGameplay>();

    public static bool Bloqueado => motivosAtivos.Count > 0;
    public static bool VisuaisBloqueados
    {
        get
        {
            foreach (MotivoBloqueioGameplay motivo in motivosAtivos)
            {
                if (motivo != MotivoBloqueioGameplay.EntradaBoss) return true;
            }
            return false;
        }
    }
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
        bool alterado = bloqueado ? motivosAtivos.Add(motivo) : motivosAtivos.Remove(motivo);
        if (!alterado) return;
        if (estavaBloqueado != Bloqueado)
            TempoGameplay.AtualizarBloqueio(Bloqueado);
        // Pause pode mudar o bloqueio visual mesmo com a entrada do boss ainda ativa.
        BloqueioAlterado?.Invoke(Bloqueado);
    }
}
