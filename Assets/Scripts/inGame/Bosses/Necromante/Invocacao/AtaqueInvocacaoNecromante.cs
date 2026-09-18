using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class AtaqueInvocacaoNecromante : AtaqueBossBase, IAtaqueVisualNecromante
{
    private enum EtapaAtaque
    {
        Inativo,
        Preparando,
        AvisandoOnda,
        Intervalo,
        Recuperando
    }

    [Header("Referencias")]
    [SerializeField] private GerenciadorInvocadosNecromante _gerenciadorInvocados;
    [SerializeField] private AvisoLanesBoss _avisoLanes;
    [SerializeField] private Transform _pontoAviso;
    [SerializeField] private GameObject _prefabAviso;
    [SerializeField] private Transform _alvoPadrao;

    [Header("Ondas")]
    [SerializeField, Min(1)] private int _quantidadeOndas = 2;
    [SerializeField, Range(0f, 1f)] private float _chanceZumbiSombrio = 0.34f;
    [SerializeField] private bool _garantirZumbiSombrio = true;
    [SerializeField] private bool _evitarRepetirLaneSegura = true;

    [Header("Tempos em segundos")]
    [SerializeField, Min(0f)] private float _tempoPreparacao = 0.8f;
    [SerializeField, Min(0f)] private float _tempoAvisoOnda = 0.7f;
    [SerializeField, Min(0f)] private float _intervaloEntreOndas = 1f;
    [SerializeField, Min(0f)] private float _tempoRecuperacao = 0.5f;

    [Header("Integracoes futuras")]
    [SerializeField] private UnityEvent _aoIniciarRitual = new UnityEvent();
    [SerializeField] private UnityEvent _aoAvisarOnda = new UnityEvent();
    [SerializeField] private UnityEvent _aoInvocarOnda = new UnityEvent();
    [SerializeField] private UnityEvent _aoFinalizar = new UnityEvent();

    private readonly List<int> _lanesOcupadas = new List<int>();
    private Transform _alvoAtual;
    private GameObject _avisoAtual;
    private int _ondasExecutadas;
    private int _ultimaLaneSegura = -1;
    private float _tempoRestante;
    private bool _invocouZumbiSombrio;
    private EtapaAtaque _etapa;

    public override bool EmExecucao => _etapa != EtapaAtaque.Inativo;
    public override bool EmPreparacao => _etapa == EtapaAtaque.Preparando
        || _etapa == EtapaAtaque.AvisandoOnda;
    public TipoAtaqueNecromante TipoAnimacao => TipoAtaqueNecromante.Invocacao;

    public override bool Inicializar()
    {
        Cancelar();
        RestaurarDisponibilidade();

        if (_gerenciadorInvocados == null || _avisoLanes == null || _pontoAviso == null
            || _gerenciadorInvocados.gameObject != gameObject || _avisoLanes.gameObject != gameObject
            || !_pontoAviso.IsChildOf(transform))
        {
            Debug.LogError("Necromante: configure gerenciador, lanes e ponto de aviso da invocacao.", this);
            return false;
        }

        return _avisoLanes.Inicializar();
    }

    public override bool TentarIniciar(Transform alvo)
    {
        if (!Disponivel || _gerenciadorInvocados.EspacosDisponiveis <= 0)
            return false;

        _alvoAtual = alvo != null ? alvo : ObterAlvo();
        if (_alvoAtual == null)
        {
            Debug.LogWarning("Necromante: jogador nao encontrado para o ataque de invocacao.", this);
            return false;
        }

        _ondasExecutadas = 0;
        _ultimaLaneSegura = -1;
        _invocouZumbiSombrio = false;
        _lanesOcupadas.Clear();
        CriarAvisoGeral();
        _tempoRestante = Mathf.Max(0f, _tempoPreparacao);
        _etapa = EtapaAtaque.Preparando;
        _aoIniciarRitual?.Invoke();
        return true;
    }

    public override void Atualizar(float deltaTime)
    {
        if (!EmExecucao || deltaTime <= 0f) return;

        if (_etapa == EtapaAtaque.AvisandoOnda)
            _avisoLanes.Atualizar(deltaTime);

        _tempoRestante -= deltaTime;
        if (_tempoRestante > 0f) return;

        switch (_etapa)
        {
            case EtapaAtaque.Preparando:
                RemoverAvisoGeral();
                PrepararProximaOnda();
                break;

            case EtapaAtaque.AvisandoOnda:
                InvocarOnda();
                _avisoLanes.Ocultar();
                _ondasExecutadas++;

                if (_ondasExecutadas >= Mathf.Max(1, _quantidadeOndas)
                    || _gerenciadorInvocados.EspacosDisponiveis <= 0)
                {
                    _tempoRestante = Mathf.Max(0f, _tempoRecuperacao);
                    _etapa = EtapaAtaque.Recuperando;
                    if (_tempoRestante <= 0f)
                        FinalizarExecucao(true);
                }
                else
                {
                    _tempoRestante = Mathf.Max(0f, _intervaloEntreOndas);
                    _etapa = EtapaAtaque.Intervalo;
                }
                break;

            case EtapaAtaque.Intervalo:
                PrepararProximaOnda();
                break;

            case EtapaAtaque.Recuperando:
                FinalizarExecucao(true);
                break;
        }
    }

    private void PrepararProximaOnda()
    {
        int quantidadeLanes = _avisoLanes.QuantidadeLanes;
        int espacosDisponiveis = _gerenciadorInvocados.EspacosDisponiveis;
        if (quantidadeLanes < 2 || espacosDisponiveis <= 0)
        {
            FinalizarExecucao(true);
            return;
        }

        int laneSegura = SortearLaneSegura(quantidadeLanes);
        _ultimaLaneSegura = laneSegura;
        _lanesOcupadas.Clear();

        for (int i = 0; i < quantidadeLanes; i++)
        {
            if (i != laneSegura)
                _lanesOcupadas.Add(i);
        }

        Embaralhar(_lanesOcupadas);
        if (_lanesOcupadas.Count > espacosDisponiveis)
            _lanesOcupadas.RemoveRange(espacosDisponiveis, _lanesOcupadas.Count - espacosDisponiveis);

        if (!_avisoLanes.Exibir(_lanesOcupadas.ToArray()))
        {
            Debug.LogWarning("Necromante: nao foi possivel avisar as lanes da invocacao.", this);
            FinalizarExecucao(false);
            return;
        }

        _tempoRestante = Mathf.Max(0f, _tempoAvisoOnda);
        _etapa = EtapaAtaque.AvisandoOnda;
        _aoAvisarOnda?.Invoke();
    }

    private int SortearLaneSegura(int quantidadeLanes)
    {
        int laneSegura = Random.Range(0, quantidadeLanes);
        if (!_evitarRepetirLaneSegura || quantidadeLanes <= 1 || laneSegura != _ultimaLaneSegura)
            return laneSegura;

        int deslocamento = Random.Range(1, quantidadeLanes);
        return (laneSegura + deslocamento) % quantidadeLanes;
    }

    private void InvocarOnda()
    {
        bool ultimaOnda = _ondasExecutadas >= Mathf.Max(1, _quantidadeOndas) - 1;

        for (int i = 0; i < _lanesOcupadas.Count; i++)
        {
            bool forcarSombrio = _garantirZumbiSombrio && !_invocouZumbiSombrio
                && ultimaOnda && i == _lanesOcupadas.Count - 1;
            bool invocarSombrio = forcarSombrio || Random.value < _chanceZumbiSombrio;

            if (invocarSombrio)
            {
                if (_gerenciadorInvocados.TentarInvocarZumbiSombrio(_lanesOcupadas[i], out _))
                    _invocouZumbiSombrio = true;
            }
            else
            {
                _gerenciadorInvocados.TentarInvocarZumbi(_lanesOcupadas[i], out _);
            }
        }

        _aoInvocarOnda?.Invoke();
    }

    private static void Embaralhar(List<int> valores)
    {
        for (int i = valores.Count - 1; i > 0; i--)
        {
            int indiceTroca = Random.Range(0, i + 1);
            int temporario = valores[i];
            valores[i] = valores[indiceTroca];
            valores[indiceTroca] = temporario;
        }
    }

    private Transform ObterAlvo()
    {
        if (_alvoPadrao != null) return _alvoPadrao;

        GameObject jogador = GameObject.FindGameObjectWithTag("Player");
        return jogador != null ? jogador.transform : null;
    }

    private void CriarAvisoGeral()
    {
        RemoverAvisoGeral();
        if (_prefabAviso == null || _pontoAviso == null) return;

        _avisoAtual = Instantiate(_prefabAviso, _pontoAviso.position, Quaternion.identity);
        _avisoAtual.transform.SetParent(_pontoAviso, true);
    }

    private void RemoverAvisoGeral()
    {
        if (_avisoAtual != null)
            Destroy(_avisoAtual);
        _avisoAtual = null;
    }

    private void FinalizarExecucao(bool iniciarCooldown)
    {
        bool estavaEmExecucao = EmExecucao;
        _avisoLanes?.Ocultar();
        RemoverAvisoGeral();
        _lanesOcupadas.Clear();
        _alvoAtual = null;
        _tempoRestante = 0f;
        _etapa = EtapaAtaque.Inativo;

        if (iniciarCooldown)
            IniciarCooldown();
        if (estavaEmExecucao)
            _aoFinalizar?.Invoke();
    }

    public override void Cancelar()
    {
        FinalizarExecucao(false);
    }

    private void OnDisable()
    {
        Cancelar();
    }

    private void OnValidate()
    {
        _quantidadeOndas = Mathf.Max(1, _quantidadeOndas);
        _chanceZumbiSombrio = Mathf.Clamp01(_chanceZumbiSombrio);
        _tempoPreparacao = Mathf.Max(0f, _tempoPreparacao);
        _tempoAvisoOnda = Mathf.Max(0f, _tempoAvisoOnda);
        _intervaloEntreOndas = Mathf.Max(0f, _intervaloEntreOndas);
        _tempoRecuperacao = Mathf.Max(0f, _tempoRecuperacao);
    }
}
