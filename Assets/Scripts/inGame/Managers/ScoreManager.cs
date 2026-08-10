using UnityEngine;
using System;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;

    public int pontuacaoAtual { get; private set; }

    public event Action<int> OnScoreMudou;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public void AdicionarPontos(int valor)
    {
        pontuacaoAtual += valor;
        OnScoreMudou?.Invoke(pontuacaoAtual);
    }

    public void Resetar()
    {
        pontuacaoAtual = 0;
        OnScoreMudou?.Invoke(pontuacaoAtual);
    }
}
