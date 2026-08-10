using UnityEngine;
using TMPro;
using System.Collections.Generic; // Necessário para List<>

public class ExibirRanking : MonoBehaviour
{
    public TextMeshProUGUI[] grupoEsquerda;
    public TextMeshProUGUI[] grupoDireita;

    private void OnEnable()
    {
        AtualizarRanking();
    }

    public void AtualizarRanking()
    {
        // Pega a lista original 
        var listaOriginal = LeaderboardManager.instance.ObterRanking();

        // Cria uma Cópia
        List<EntradaRanking> listaParaExibir = new List<EntradaRanking>(listaOriginal);

        // Adicionando tracinho APENAS NA CÓPIA
        while (listaParaExibir.Count < 10)
        {
            listaParaExibir.Add(new EntradaRanking("---", 0));
        }

        // Exibe a cópia na tela
        for (int i = 0; i < 5; i++)
        {
            if (grupoEsquerda[i] != null)
                grupoEsquerda[i].text = $"{i + 1}. {listaParaExibir[i].nome} - {listaParaExibir[i].pontuacao}";
        }

        for (int i = 5; i < 10; i++)
        {
            if (grupoDireita[i - 5] != null)
                grupoDireita[i - 5].text = $"{i + 1}. {listaParaExibir[i].nome} - {listaParaExibir[i].pontuacao}";
        }
    }
}