using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour, IHudTransicaoCenario
{
    [Header("Referências UI")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI vidasText;
    [SerializeField] private TextMeshProUGUI comboText;
    [Header("Vidas em coracoes")]
    [SerializeField] private RectTransform _grupoCoracoes;
    [SerializeField] private Sprite _spriteCoracao;
    private readonly List<Image> _coracoes = new List<Image>();
    private Color _corCombo;
    //[SerializeField] private TextMeshProUGUI TurnoText;

    public GameObject GrupoHudTransicao => scoreText != null && scoreText.canvas != null
        ? scoreText.canvas.gameObject : gameObject;

    private void Start()
    {
        _corCombo = comboText != null ? comboText.color : Color.white;
        // A HUD usa referencias da cena; o grupo novo acompanha o texto antigo no mesmo Canvas.
        if (_grupoCoracoes == null && vidasText != null)
            _grupoCoracoes = vidasText.transform.parent.Find("VidasCoracoes") as RectTransform;
        if (_grupoCoracoes != null)
        {
            _coracoes.AddRange(_grupoCoracoes.GetComponentsInChildren<Image>(true));
            if (_spriteCoracao == null && _coracoes.Count > 0) _spriteCoracao = _coracoes[0].sprite;
        }
        // Inicializa HUD com valores atuais
        AtualizarScore(ScoreManager.instance.pontuacaoAtual);
        AtualizarVidas(VidaManager.instance.vidasAtuais);
        AtualizarAvisoEntregas(VidaManager.instance.EntregasPerdidasSeguidas);

        // Conecta eventos
        ScoreManager.instance.OnScoreMudou += AtualizarScore;
        VidaManager.instance.OnVidaMudou += AtualizarVidas;
        VidaManager.instance.PerdasSeguidasAlteradas += AtualizarAvisoEntregas;
    }

    private void OnDestroy()
    {
        // Desconectar eventos para evitar leaks
        if (ScoreManager.instance != null)
            ScoreManager.instance.OnScoreMudou -= AtualizarScore;

        if (VidaManager.instance != null)
        {
            VidaManager.instance.OnVidaMudou -= AtualizarVidas;
            VidaManager.instance.PerdasSeguidasAlteradas -= AtualizarAvisoEntregas;
        }

    }

    private void AtualizarScore(int pontos)
    {
        scoreText.text = $"PONTUAÇÃO: {pontos}";
    }

    private void AtualizarVidas(int vida)
    {
        if (_grupoCoracoes == null || _spriteCoracao == null || _coracoes.Count == 0)
        {
            if (vidasText != null) vidasText.text = $"VIDAS: {vida}";
            return;
        }
        if (vidasText != null) vidasText.gameObject.SetActive(false);
        int quantidade = Mathf.Max(0, vida);
        while (_coracoes.Count < quantidade)
        {
            Image novo = Instantiate(_coracoes[0], _grupoCoracoes);
            novo.name = $"Coracao{_coracoes.Count + 1}";
            _coracoes.Add(novo);
        }
        for (int i = 0; i < _coracoes.Count; i++)
        {
            _coracoes[i].sprite = _spriteCoracao;
            _coracoes[i].gameObject.SetActive(i < quantidade);
        }
    }

    private void AtualizarAvisoEntregas(int perdas)
    {
        if (comboText == null) return;
        int restantes = VidaManager.instance != null ? VidaManager.instance.PerdasRestantesParaDano : 3;
        comboText.color = perdas == 0 ? _corCombo
            : restantes == 1 ? new Color(1f, .3f, .25f) : new Color(1f, .85f, .15f);
        int limite = perdas + restantes;
        comboText.text = $"ENTREGAS PERDIDAS: {perdas}/{limite}";
    }
}
