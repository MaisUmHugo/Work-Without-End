using UnityEngine;

[DisallowMultipleComponent]
public class ControladorFasesNecromante : ControladorFasesBossBase
{
    [Header("Componentes")]
    [SerializeField] private SeletorAtaquesBoss _seletorAtaques;
    [SerializeField] private AtaqueInvocacaoNecromante _ataqueInvocacao;
    [SerializeField] private GerenciadorInvocadosNecromante _gerenciadorInvocados;
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
    }

    protected override bool ValidarReferencias()
    {
        if (!base.ValidarReferencias()) return false;

        if (_seletorAtaques == null || _ataqueInvocacao == null || _gerenciadorInvocados == null
            || _seletorAtaques.gameObject != gameObject
            || _ataqueInvocacao.gameObject != gameObject
            || _gerenciadorInvocados.gameObject != gameObject)
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
    }
}
