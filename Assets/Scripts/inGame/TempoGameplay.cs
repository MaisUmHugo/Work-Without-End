using System.Collections;
using UnityEngine;

public static class TempoGameplay
{
    private static double _tempoBloqueado;
    private static double _inicioBloqueio;

    public static float DeltaTime => BloqueioGameplay.Bloqueado ? 0f : Time.deltaTime;
    public static float FixedDeltaTime => BloqueioGameplay.Bloqueado ? 0f : Time.fixedDeltaTime;
    public static float Tempo => (float)((BloqueioGameplay.Bloqueado ? _inicioBloqueio : Time.timeAsDouble) - _tempoBloqueado);

    public static void AtualizarBloqueio(bool bloqueado)
    {
        if (bloqueado)
            _inicioBloqueio = Time.timeAsDouble;
        else
            _tempoBloqueado += Time.timeAsDouble - _inicioBloqueio;
    }

    public static void Reiniciar()
    {
        _tempoBloqueado = 0d;
        _inicioBloqueio = 0d;
    }

    public static IEnumerator DestruirDepois(Object alvo, float duracao)
    {
        yield return new EsperaGameplay(duracao);
        if (alvo != null)
            Object.Destroy(alvo);
    }
}

public class EsperaGameplay : CustomYieldInstruction
{
    private readonly float _fim;

    public EsperaGameplay(float duracao)
    {
        _fim = TempoGameplay.Tempo + Mathf.Max(0f, duracao);
    }

    public override bool keepWaiting => BloqueioGameplay.Bloqueado || TempoGameplay.Tempo < _fim;
}
