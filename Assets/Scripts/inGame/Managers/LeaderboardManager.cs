using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager instance;

    // Limite de entradas no ranking
    private const int limiteRanking = 10;

    // Lista de pontuações carregada
    private List<EntradaRanking> ranking = new List<EntradaRanking>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Mantém entre cenas 
            CarregarRanking();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ADICIONAR NOVA PONTUAÇÃO
    public void AdicionarEntrada(string nome, int pontuacao)
    {
        ranking.Add(new EntradaRanking(nome, pontuacao));

        // Ordena pela maior pontuação primeiro
        ranking.Sort((a, b) => b.pontuacao.CompareTo(a.pontuacao));

        // Mantém top X
        if (ranking.Count > limiteRanking)
            ranking.RemoveAt(ranking.Count - 1);

        SalvarRanking();
    }

    public bool Top10(int pontuacao)
    {
        if (ranking.Count < limiteRanking)
            return true;

        return pontuacao > ranking[ranking.Count - 1].pontuacao;
    }

    // ACESSAR LISTA
    public List<EntradaRanking> ObterRanking()
    {
        return ranking;
    }

    // SALVAR
    private void SalvarRanking()
    {
        PlayerPrefs.SetInt("rank_tamanho", ranking.Count);

        for (int i = 0; i < ranking.Count; i++)
        {
            PlayerPrefs.SetString($"rank_nome_{i}", ranking[i].nome);
            PlayerPrefs.SetInt($"rank_pontuacao_{i}", ranking[i].pontuacao);
        }
        Debug.Log("Salvando ranking com " + ranking.Count + " entradas");
        PlayerPrefs.Save();
    }
   
    // CARREGAR
    public void CarregarRanking()
    {
        ranking.Clear();

        int tamanho = PlayerPrefs.GetInt("rank_tamanho", 0);
        Debug.Log("Carregando Ranking. Tamanho = " + tamanho);

        for (int i = 0; i < tamanho; i++)
        {
            string nome = PlayerPrefs.GetString($"rank_nome_{i}", "---");
            int pontos = PlayerPrefs.GetInt($"rank_pontuacao_{i}", 0);

            ranking.Add(new EntradaRanking(nome, pontos));
        }
    }

}
