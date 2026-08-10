using UnityEngine;
using TMPro;

public class HUDManager : MonoBehaviour
{
    [Header("Referências UI")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI vidasText;
    [SerializeField] private TextMeshProUGUI comboText;
    //[SerializeField] private TextMeshProUGUI TurnoText;

    private void Start()
    {
        // Inicializa HUD com valores atuais
        AtualizarScore(ScoreManager.instance.pontuacaoAtual);
        AtualizarVidas(VidaManager.instance.vidasAtuais);
        AtualizarCombo(ComboManager.instance.comboAtual);

        // Conecta eventos
        ScoreManager.instance.OnScoreMudou += AtualizarScore;
        VidaManager.instance.OnVidaMudou += AtualizarVidas;
        ComboManager.instance.OnComboMudou += AtualizarCombo;
    }

    private void OnDestroy()
    {
        // Desconectar eventos para evitar leaks
        if (ScoreManager.instance != null)
            ScoreManager.instance.OnScoreMudou -= AtualizarScore;

        if (VidaManager.instance != null)
            VidaManager.instance.OnVidaMudou -= AtualizarVidas;

        if (ComboManager.instance != null)
            ComboManager.instance.OnComboMudou -= AtualizarCombo;
    }

    private void AtualizarScore(int pontos)
    {
        scoreText.text = $"PONTUAÇÃO: {pontos}";
    }

    private void AtualizarVidas(int vida)
    {
        vidasText.text = $"VIDAS: {vida}";
    }

    private void AtualizarCombo(int combo)
    {
        if (combo > 0)
            comboText.text = $"COMBO: {combo}";
        else
            comboText.text = ""; // esconde quando não há combo
    }
}
