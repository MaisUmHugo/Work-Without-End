using System.Collections.Generic;

public enum MotivoBloqueioGameplay
{
    Pause,
    Tutorial,
    GameOver
}

public static class BloqueioGameplay
{
    private static readonly HashSet<MotivoBloqueioGameplay> motivosAtivos = new HashSet<MotivoBloqueioGameplay>();

    public static bool Bloqueado => motivosAtivos.Count > 0;

    public static void Definir(MotivoBloqueioGameplay motivo, bool bloqueado)
    {
        if (bloqueado)
            motivosAtivos.Add(motivo);
        else
            motivosAtivos.Remove(motivo);
    }
}