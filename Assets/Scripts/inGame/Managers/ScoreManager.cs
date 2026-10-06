using UnityEngine;
using System;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;

    public int pontuacaoAtual { get; private set; }
    public int EntregasRealizadas { get; private set; }
    public int EntregasPerdidas { get; private set; }
    public int VezesAtingido { get; private set; }
    public int MaiorCombo { get; private set; }
    public bool PartidaEncerrada { get; private set; }

    public event Action<int> OnScoreMudou;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public void AdicionarPontos(int valor)
    {
        if (PartidaEncerrada) return;
        pontuacaoAtual += valor;
        OnScoreMudou?.Invoke(pontuacaoAtual);
    }

    public void Resetar()
    {
        pontuacaoAtual = 0;
        EntregasRealizadas = 0;
        EntregasPerdidas = 0;
        VezesAtingido = 0;
        MaiorCombo = 0;
        PartidaEncerrada = false;
        OnScoreMudou?.Invoke(pontuacaoAtual);
    }

    public void RegistrarEntrega(int comboAtual)
    {
        if (PartidaEncerrada) return;
        EntregasRealizadas++;
        MaiorCombo = Mathf.Max(MaiorCombo, comboAtual);
    }

    public void RegistrarEntregaPerdida()
    {
        if (!PartidaEncerrada) EntregasPerdidas++;
    }

    public void RegistrarDanoRecebido()
    {
        if (!PartidaEncerrada) VezesAtingido++;
    }

    public void EncerrarRegistroPartida()
    {
        PartidaEncerrada = true;
    }

    public string ObterResumoPartida()
    {
        return $"Entregas realizadas: {EntregasRealizadas}\nEntregas perdidas: {EntregasPerdidas}"
            + $"\nVezes atingido: {VezesAtingido}\nMaior combo: {MaiorCombo}";
    }
}
