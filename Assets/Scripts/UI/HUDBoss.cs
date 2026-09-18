using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HUDBoss : MonoBehaviour
{
    [Header("Boss observado")]
    [SerializeField] private string _nomeBoss = "NECROMANTE";
    [SerializeField] private VidaBoss _vida;
    [SerializeField] private ControladorFasesBossBase _controladorFases;
    [SerializeField, Tooltip("Componente que implementa IControladorEncontroBoss.")]
    private MonoBehaviour _controladorEncontro;

    [Header("Elementos visuais")]
    [SerializeField] private GameObject _painel;
    [SerializeField] private TMP_Text _textoNome;
    [SerializeField] private TMP_Text _textoFase;
    [SerializeField] private TMP_Text _textoVida;
    [SerializeField] private Image _preenchimentoVida;
    [SerializeField] private bool _ocultarAoConcluirEncontro = true;

    private IControladorEncontroBoss _fonteEncontro;
    private bool _eventosConectados;

    public float PercentualVida { get; private set; }

    private void Awake()
    {
        ResolverFonteEncontro();
        AtualizarNome();
    }

    private void OnEnable()
    {
        ConectarEventos();
        AtualizarTudo();
    }

    private void OnDisable()
    {
        DesconectarEventos();
    }

    public void ConfigurarBoss(
        string nomeBoss,
        VidaBoss vida,
        ControladorFasesBossBase controladorFases,
        MonoBehaviour controladorEncontro)
    {
        DesconectarEventos();

        _nomeBoss = nomeBoss;
        _vida = vida;
        _controladorFases = controladorFases;
        _controladorEncontro = controladorEncontro;
        ResolverFonteEncontro();

        if (isActiveAndEnabled)
            ConectarEventos();

        AtualizarTudo();
    }

    public void Exibir()
    {
        if (_painel != null)
            _painel.SetActive(true);
    }

    public void Ocultar()
    {
        if (_painel != null)
            _painel.SetActive(false);
    }

    private void ConectarEventos()
    {
        if (_eventosConectados || _vida == null || _controladorFases == null)
            return;

        _vida.VidaAlterada += AoAlterarVida;
        _controladorFases.FaseAlterada += AoAlterarFase;

        if (_fonteEncontro != null)
            _fonteEncontro.EncontroEncerrado += AoEncerrarEncontro;

        _eventosConectados = true;
    }

    private void DesconectarEventos()
    {
        if (!_eventosConectados) return;

        if (_vida != null)
            _vida.VidaAlterada -= AoAlterarVida;
        if (_controladorFases != null)
            _controladorFases.FaseAlterada -= AoAlterarFase;
        if (_fonteEncontro != null)
            _fonteEncontro.EncontroEncerrado -= AoEncerrarEncontro;

        _eventosConectados = false;
    }

    private void AtualizarTudo()
    {
        AtualizarNome();

        if (_vida != null)
            AoAlterarVida(_vida.VidaAtual, _vida.VidaMaxima);
        else
            AtualizarVida(0, 1);

        if (_controladorFases != null)
            AoAlterarFase(_controladorFases.FaseAtual);

        if (_fonteEncontro == null || !_fonteEncontro.Concluido)
            Exibir();
        else if (_ocultarAoConcluirEncontro)
            Ocultar();
    }

    private void AtualizarNome()
    {
        if (_textoNome != null)
            _textoNome.text = _nomeBoss;
    }

    private void AoAlterarVida(int vidaAtual, int vidaMaxima)
    {
        AtualizarVida(vidaAtual, vidaMaxima);

        if (vidaAtual > 0)
            Exibir();
    }

    private void AtualizarVida(int vidaAtual, int vidaMaxima)
    {
        int maximaSegura = Mathf.Max(1, vidaMaxima);
        int atualSeguro = Mathf.Clamp(vidaAtual, 0, maximaSegura);
        PercentualVida = Mathf.Clamp01((float)atualSeguro / maximaSegura);

        if (_preenchimentoVida != null)
            _preenchimentoVida.fillAmount = PercentualVida;
        if (_textoVida != null)
            _textoVida.text = $"{atualSeguro} / {maximaSegura}";
    }

    private void AoAlterarFase(FaseBoss fase)
    {
        if (_textoFase != null)
            _textoFase.text = $"FASE {(int)fase}";
    }

    private void AoEncerrarEncontro(ResultadoEncontroBoss resultado)
    {
        if (_ocultarAoConcluirEncontro)
            Ocultar();
    }

    private void ResolverFonteEncontro()
    {
        _fonteEncontro = _controladorEncontro as IControladorEncontroBoss;

        if (_controladorEncontro != null && _fonteEncontro == null)
        {
            Debug.LogError(
                "HUD do boss: o controlador de encontro deve implementar IControladorEncontroBoss.",
                this);
        }
    }

    private void OnValidate()
    {
        if (_preenchimentoVida != null)
        {
            _preenchimentoVida.type = Image.Type.Filled;
            _preenchimentoVida.fillMethod = Image.FillMethod.Horizontal;
            _preenchimentoVida.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }
}
