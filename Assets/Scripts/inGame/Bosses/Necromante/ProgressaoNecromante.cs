using UnityEngine;

[DisallowMultipleComponent]
public class ProgressaoNecromante : MonoBehaviour
{
    public enum EstadoProgressao
    {
        HordasCidade,
        PrimeiroEncontro,
        TransicaoFloresta,
        HordasFloresta,
        EncontroFinal,
        Concluido,
        GameOver
    }

    [Header("Referencias da cena")]
    [SerializeField] private HordaManager _hordas;
    [SerializeField] private ControladorTransicaoCenario _transicao;
    [SerializeField] private ControladorEncontroNecromante _encontro;
    [SerializeField] private VidaManager _vidaJogador;

    [Header("Encontros apos concluir a horda")]
    [SerializeField, Min(1)] private int _hordaPrimeiroEncontro = 10;
    [SerializeField, Min(2)] private int _hordaEncontroFinal = 20;
    [SerializeField, Min(1f)] private float _dificuldadeEncontroFinal = 1.15f;

    [Header("Pontuacao dos encontros")]
    [SerializeField, Min(0)] private int _pontosFuga = 5000;
    [SerializeField, Min(0)] private int _pontosDerrotaDefinitiva = 10000;

    [SerializeField] private EstadoProgressao _estado;
    private ControladorBoss _boss;
    private bool _preparado;

    public EstadoProgressao EstadoAtual => _estado;
    public bool EncontroEmAndamento => _estado == EstadoProgressao.PrimeiroEncontro
        || _estado == EstadoProgressao.EncontroFinal;

    private void Start()
    {
        if (_hordas == null || _transicao == null || _encontro == null || _vidaJogador == null)
        {
            Debug.LogError("Necromante: configure hordas, transicao, encontro e vida do jogador na progressao.", this);
            enabled = false;
            return;
        }
        _boss = _encontro.GetComponent<ControladorBoss>();
        if (_boss == null)
        {
            Debug.LogError("Necromante: o encontro precisa ter um ControladorBoss.", this);
            enabled = false;
            return;
        }
        _hordas.DefinirTrocaCenarioPorHorda(false);
        _boss.EncerrarExecucao();
        _encontro.gameObject.SetActive(false);
        _transicao.SolicitarCenario(0);
        _estado = EstadoProgressao.HordasCidade;
        _hordas.HordaConcluida += AoConcluirHorda;
        _encontro.EncerramentoIniciado += AoIniciarEncerramento;
        _encontro.EncontroEncerrado += AoConcluirEncontro;
        _vidaJogador.OnGameOver += AoGameOver;
        _preparado = true;
    }

    private void Update()
    {
        if (!_preparado || _estado == EstadoProgressao.GameOver) return;
        if (_vidaJogador.vidasAtuais <= 0 || BloqueioGameplay.EstaAtivo(MotivoBloqueioGameplay.GameOver))
        {
            AoGameOver();
            return;
        }
        if (_estado == EstadoProgressao.TransicaoFloresta
            && _transicao.CenarioAtual == 1 && !_transicao.EmTransicao
            && _transicao.CenarioSolicitado == 1 && !BloqueioGameplay.Bloqueado)
        {
            _estado = EstadoProgressao.HordasFloresta;
            _hordas.DefinirSuspensoPorBoss(false);
        }
    }

    private void AoConcluirHorda(int numeroHorda)
    {
        if (_estado == EstadoProgressao.HordasCidade && numeroHorda >= _hordaPrimeiroEncontro)
            TentarIniciarPrimeiroEncontro();
        else if (_estado == EstadoProgressao.HordasFloresta && numeroHorda >= _hordaEncontroFinal)
            TentarIniciarEncontroFinal();
    }

    public bool TentarIniciarPrimeiroEncontro()
    {
        if (!PodeIniciar() || _estado != EstadoProgressao.HordasCidade
            || _transicao.CenarioAtual != 0) return false;
        IniciarEncontro(TipoEncontroNecromante.PrimeiroEncontro);
        return true;
    }

    public bool TentarIniciarEncontroFinal()
    {
        if (!PodeIniciar() || _estado != EstadoProgressao.HordasFloresta
            || _transicao.CenarioAtual != 1) return false;
        IniciarEncontro(TipoEncontroNecromante.EncontroFinal);
        return true;
    }

    private bool PodeIniciar()
    {
        return _preparado && _vidaJogador.vidasAtuais > 0 && !BloqueioGameplay.Bloqueado
            && !_transicao.EmTransicao && _transicao.CenarioAtual == _transicao.CenarioSolicitado;
    }

    private void IniciarEncontro(TipoEncontroNecromante tipo)
    {
        _estado = tipo == TipoEncontroNecromante.PrimeiroEncontro
            ? EstadoProgressao.PrimeiroEncontro : EstadoProgressao.EncontroFinal;
        _hordas.DefinirSuspensoPorBoss(true);
        _encontro.gameObject.SetActive(true);
        _encontro.PrepararEncontro(tipo, tipo == TipoEncontroNecromante.EncontroFinal,
            tipo == TipoEncontroNecromante.EncontroFinal ? _dificuldadeEncontroFinal : 1f);
        IntegradorHUDBoss integrador = _encontro.GetComponent<IntegradorHUDBoss>();
        if (integrador != null && integrador.HudAtual != null)
            integrador.HudAtual.DefinirTextoVidaVisivel(false);
        ScoreManager.instance?.RegistrarInicioNecromante(tipo);
        _boss.IniciarEncontro();
    }

    private void AoIniciarEncerramento(ResultadoEncontroBoss resultado)
    {
        if (!EncontroEmAndamento) return;
        int pontos = resultado == ResultadoEncontroBoss.Fuga ? _pontosFuga : _pontosDerrotaDefinitiva;
        // Registra a vitoria quando a vida do boss acaba, antes da animacao de encerramento.
        ScoreManager.instance?.RegistrarResultadoNecromante(resultado, pontos);
    }

    private void AoConcluirEncontro(ResultadoEncontroBoss resultado)
    {
        if (!EncontroEmAndamento || _vidaJogador.vidasAtuais <= 0) return;
        _encontro.gameObject.SetActive(false);
        if (resultado == ResultadoEncontroBoss.Fuga)
        {
            _estado = EstadoProgressao.TransicaoFloresta;
            _transicao.SolicitarCenario(1);
        }
        else
        {
            _estado = EstadoProgressao.Concluido;
            _hordas.DefinirSuspensoPorBoss(false);
        }
    }

    private void AoGameOver()
    {
        if (_estado == EstadoProgressao.GameOver) return;
        _estado = EstadoProgressao.GameOver;
        _hordas.DefinirSuspensoPorBoss(true);
        _boss.EncerrarExecucao();
        _transicao.CancelarTransicao();
        _encontro.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (!_preparado) return;
        _hordas.HordaConcluida -= AoConcluirHorda;
        _encontro.EncerramentoIniciado -= AoIniciarEncerramento;
        _encontro.EncontroEncerrado -= AoConcluirEncontro;
        _vidaJogador.OnGameOver -= AoGameOver;
        _preparado = false;
    }

    private void OnValidate()
    {
        _hordaPrimeiroEncontro = Mathf.Max(1, _hordaPrimeiroEncontro);
        _hordaEncontroFinal = Mathf.Max(_hordaPrimeiroEncontro + 1, _hordaEncontroFinal);
        _dificuldadeEncontroFinal = Mathf.Max(1f, _dificuldadeEncontroFinal);
        _pontosFuga = Mathf.Max(0, _pontosFuga);
        _pontosDerrotaDefinitiva = Mathf.Max(0, _pontosDerrotaDefinitiva);
    }
}
