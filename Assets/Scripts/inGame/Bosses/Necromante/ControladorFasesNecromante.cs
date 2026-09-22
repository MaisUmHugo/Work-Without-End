using UnityEngine;

[DisallowMultipleComponent]
public class ControladorFasesNecromante : ControladorFasesBossBase
{
    [Header("Componentes")]
    [SerializeField] private SeletorAtaquesBoss _seletorAtaques;
    [SerializeField] private AtaqueInvocacaoNecromante _ataqueInvocacao;
    [SerializeField] private GerenciadorInvocadosNecromante _gerenciadorInvocados;
    [SerializeField] private ComportamentoBossNecromante _comportamento;
    [SerializeField] private AtaqueProjetilNecromante _ataqueProjetil;
    [SerializeField] private AtaqueTiroCarregadoNecromante _tiroCarregado;
    [SerializeField] private AtaqueSequenciaRapidaNecromante _sequenciaRapida;
    [SerializeField] private PressaoAmbienteNecromante _pressaoAmbiente;
    [Header("Configuracoes")]
    [SerializeField] private ConfiguracaoFaseNecromante[] _configuracoes;

    protected override void AoAplicarFase(FaseBoss fase)
    {
        ConfiguracaoFaseNecromante configuracao = ObterConfiguracao(fase);
        if (configuracao == null)
        {
            Debug.LogError($"Necromante: configuracao da {fase} nao encontrada.", this);
            return;
        }

        if (!_seletorAtaques.DefinirAtaquesPermitidos(configuracao.AtaquesDisponiveis))
            return;

        _ataqueInvocacao.ConfigurarFase(
            configuracao.QuantidadeOndas,
            configuracao.ChanceZumbiSombrio,
            configuracao.GarantirZumbiSombrio);
        _gerenciadorInvocados.DefinirLimiteSimultaneo(configuracao.LimiteInvocados);
        _gerenciadorInvocados.DefinirMultiplicadorVelocidade(
            configuracao.MultiplicadorVelocidadeInvocados);
        _comportamento.ConfigurarMultiplicadorDecisao(configuracao.MultiplicadorDecisao);
        _ataqueProjetil.ConfigurarRitmo(
            configuracao.MultiplicadorRitmoAtaques,
            configuracao.MultiplicadorVelocidadeProjeteis);
        _tiroCarregado.ConfigurarRitmo(
            configuracao.MultiplicadorRitmoAtaques,
            configuracao.MultiplicadorVelocidadeProjeteis);
        _sequenciaRapida.ConfigurarRitmo(
            configuracao.MultiplicadorRitmoAtaques,
            configuracao.MultiplicadorVelocidadeProjeteis);
        _ataqueInvocacao.ConfigurarRitmo(configuracao.MultiplicadorRitmoAtaques);
        _pressaoAmbiente.ConfigurarFase(
            configuracao.LimitePressaoAmbiente,
            configuracao.IntervaloPressaoAmbiente,
            configuracao.MultiplicadorVelocidadeInvocados);
    }

    protected override bool ValidarReferencias()
    {
        if (!base.ValidarReferencias()) return false;

        if (_seletorAtaques == null || _ataqueInvocacao == null || _gerenciadorInvocados == null
            || _comportamento == null || _ataqueProjetil == null || _tiroCarregado == null
            || _sequenciaRapida == null || _pressaoAmbiente == null
            || _seletorAtaques.gameObject != gameObject
            || _ataqueInvocacao.gameObject != gameObject
            || _gerenciadorInvocados.gameObject != gameObject
            || _comportamento.gameObject != gameObject
            || _ataqueProjetil.gameObject != gameObject
            || _tiroCarregado.gameObject != gameObject
            || _sequenciaRapida.gameObject != gameObject
            || _pressaoAmbiente.gameObject != gameObject)
        {
            Debug.LogError("Necromante: configure seletor, invocacao e gerenciador na raiz do boss.", this);
            return false;
        }

        for (int valor = (int)FaseBoss.Fase1; valor <= (int)FaseBoss.Fase3; valor++)
        {
            ConfiguracaoFaseNecromante configuracao = ObterConfiguracao((FaseBoss)valor);
            if (configuracao == null || !configuracao.Validar(gameObject))
            {
                Debug.LogError($"Necromante: configure corretamente a {(FaseBoss)valor}.", this);
                return false;
            }
        }

        return true;
    }

    private ConfiguracaoFaseNecromante ObterConfiguracao(FaseBoss fase)
    {
        if (_configuracoes == null) return null;

        for (int i = 0; i < _configuracoes.Length; i++)
        {
            ConfiguracaoFaseNecromante configuracao = _configuracoes[i];
            if (configuracao != null && configuracao.Fase == fase)
                return configuracao;
        }

        return null;
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        if (_seletorAtaques == null)
            _seletorAtaques = GetComponent<SeletorAtaquesBoss>();
        if (_ataqueInvocacao == null)
            _ataqueInvocacao = GetComponent<AtaqueInvocacaoNecromante>();
        if (_gerenciadorInvocados == null)
            _gerenciadorInvocados = GetComponent<GerenciadorInvocadosNecromante>();
        if (_comportamento == null)
            _comportamento = GetComponent<ComportamentoBossNecromante>();
        if (_ataqueProjetil == null)
            _ataqueProjetil = GetComponent<AtaqueProjetilNecromante>();
        if (_tiroCarregado == null)
            _tiroCarregado = GetComponent<AtaqueTiroCarregadoNecromante>();
        if (_sequenciaRapida == null)
            _sequenciaRapida = GetComponent<AtaqueSequenciaRapidaNecromante>();
        if (_pressaoAmbiente == null)
            _pressaoAmbiente = GetComponent<PressaoAmbienteNecromante>();
    }
}
