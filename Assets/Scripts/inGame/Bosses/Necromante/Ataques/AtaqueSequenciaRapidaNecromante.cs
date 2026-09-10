using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class AtaqueSequenciaRapidaNecromante : AtaqueBossBase
{
    private enum EtapaAtaque
    {
        Inativo,
        Preparando,
        AvisandoDisparo,
        Intervalo,
        Recuperando
    }

    [Header("Referencias")]
    [SerializeField] private ProjetilNecromante _prefabProjetil;
    [SerializeField] private Transform _projectileRoot;
    [SerializeField] private AvisoLanesBoss _avisoLanes;
    [SerializeField] private Transform _pontoAviso;
    [SerializeField] private GameObject _prefabAviso;
    [SerializeField] private Transform _alvoPadrao;
    [Header("Sequencia")]
    [SerializeField, Min(1)] private int _quantidadeDisparos = 4;
    [SerializeField] private bool _evitarRepeticaoLane = true;
    [Header("Tempos em segundos")]
    [SerializeField, Min(0f)] private float _tempoPreparacao = 0.35f;
    [SerializeField, Min(0f)] private float _tempoAvisoPorDisparo = 0.15f;
    [SerializeField, Min(0f)] private float _intervaloEntreDisparos = 0.02f;
    [SerializeField, Min(0f)] private float _tempoRecuperacao = 0.3f;
    [Header("Projetil")]
    [SerializeField, Min(0.1f)] private float _velocidadeProjetil = 40f;
    [SerializeField, Min(1)] private int _danoProjetil = 1;
    [SerializeField, Min(0.1f)] private float _escalaVisual = 1f;
    [Header("Integracoes futuras")]
    [SerializeField] private UnityEvent _aoIniciarSequencia = new UnityEvent();
    [SerializeField] private UnityEvent _aoAvisarDisparo = new UnityEvent();
    [SerializeField] private UnityEvent _aoDisparar = new UnityEvent();
    [SerializeField] private UnityEvent _aoFinalizar = new UnityEvent();

    private readonly List<int> _ordemLanes = new List<int>();
    private readonly List<ProjetilNecromante> _projeteisAtivos = new List<ProjetilNecromante>();
    private Transform _alvoAtual;
    private GameObject _avisoAtual;
    private int _laneEscolhida = -1;
    private int _ultimaLane = -1;
    private int _disparosRealizados;
    private float _xAlvoTravado;
    private float _tempoRestante;
    private EtapaAtaque _etapa;

    public override bool EmExecucao => _etapa != EtapaAtaque.Inativo;
    public override bool EmPreparacao => _etapa == EtapaAtaque.Preparando
        || _etapa == EtapaAtaque.AvisandoDisparo;

    public override bool Inicializar()
    {
        Cancelar();
        RestaurarDisponibilidade();

        if (_prefabProjetil == null || _projectileRoot == null || _avisoLanes == null
            || _pontoAviso == null || !_projectileRoot.IsChildOf(transform)
            || !_pontoAviso.IsChildOf(transform) || _avisoLanes.gameObject != gameObject)
        {
            Debug.LogError("Necromante: configure projetil, ProjectileRoot, lanes e aviso da sequencia rapida.", this);
            return false;
        }

        return _avisoLanes.Inicializar();
    }

    public override bool TentarIniciar(Transform alvo)
    {
        if (!Disponivel) return false;

        _alvoAtual = alvo != null ? alvo : ObterAlvo();
        if (_alvoAtual == null)
        {
            Debug.LogWarning("Necromante: jogador nao encontrado para a sequencia rapida.", this);
            return false;
        }

        _ordemLanes.Clear();
        _laneEscolhida = -1;
        _ultimaLane = -1;
        _disparosRealizados = 0;
        CriarAvisoGeral();
        _tempoRestante = Mathf.Max(0f, _tempoPreparacao);
        _etapa = EtapaAtaque.Preparando;
        _aoIniciarSequencia?.Invoke();
        return true;
    }

    public override void Atualizar(float deltaTime)
    {
        if (!EmExecucao || deltaTime <= 0f) return;

        if (_etapa == EtapaAtaque.AvisandoDisparo)
            _avisoLanes.Atualizar(deltaTime);

        _tempoRestante -= deltaTime;
        if (_tempoRestante > 0f) return;

        switch (_etapa)
        {
            case EtapaAtaque.Preparando:
                RemoverAvisoGeral();
                PrepararProximoDisparo();
                break;

            case EtapaAtaque.AvisandoDisparo:
                Disparar();
                _avisoLanes.Ocultar();
                _disparosRealizados++;

                if (_disparosRealizados >= Mathf.Max(1, _quantidadeDisparos))
                {
                    _tempoRestante = Mathf.Max(0f, _tempoRecuperacao);
                    _etapa = EtapaAtaque.Recuperando;
                    if (_tempoRestante <= 0f)
                        FinalizarExecucao(true);
                }
                else
                {
                    _tempoRestante = Mathf.Max(0f, _intervaloEntreDisparos);
                    _etapa = EtapaAtaque.Intervalo;
                }
                break;

            case EtapaAtaque.Intervalo:
                PrepararProximoDisparo();
                break;

            case EtapaAtaque.Recuperando:
                FinalizarExecucao(true);
                break;
        }
    }

    private bool PrepararProximoDisparo()
    {
        if (_alvoAtual == null)
            _alvoAtual = ObterAlvo();
        if (_alvoAtual == null)
        {
            FinalizarExecucao(false);
            return false;
        }

        _laneEscolhida = EscolherProximaLane();
        if (_laneEscolhida < 0 || !_avisoLanes.Exibir(_laneEscolhida))
        {
            Debug.LogWarning("Necromante: nao foi possivel escolher a lane da sequencia rapida.", this);
            FinalizarExecucao(false);
            return false;
        }

        _ultimaLane = _laneEscolhida;
        _xAlvoTravado = _alvoAtual.position.x;
        _tempoRestante = Mathf.Max(0f, _tempoAvisoPorDisparo);
        _etapa = EtapaAtaque.AvisandoDisparo;
        _aoAvisarDisparo?.Invoke();
        return true;
    }

    private int EscolherProximaLane()
    {
        int quantidadeLanes = _avisoLanes.QuantidadeLanes;
        if (quantidadeLanes <= 0) return -1;

        if (!_evitarRepeticaoLane)
            return Random.Range(0, quantidadeLanes);

        if (_ordemLanes.Count == 0)
            PreencherOrdemLanes(quantidadeLanes);

        int ultimoIndice = _ordemLanes.Count - 1;
        if (_ordemLanes.Count > 1 && _ordemLanes[ultimoIndice] == _ultimaLane)
        {
            int indiceTroca = Random.Range(0, ultimoIndice);
            int temporario = _ordemLanes[indiceTroca];
            _ordemLanes[indiceTroca] = _ordemLanes[ultimoIndice];
            _ordemLanes[ultimoIndice] = temporario;
        }

        int lane = _ordemLanes[ultimoIndice];
        _ordemLanes.RemoveAt(ultimoIndice);
        return lane;
    }

    private void PreencherOrdemLanes(int quantidadeLanes)
    {
        _ordemLanes.Clear();
        for (int i = 0; i < quantidadeLanes; i++)
            _ordemLanes.Add(i);

        for (int i = _ordemLanes.Count - 1; i > 0; i--)
        {
            int indiceTroca = Random.Range(0, i + 1);
            int temporario = _ordemLanes[i];
            _ordemLanes[i] = _ordemLanes[indiceTroca];
            _ordemLanes[indiceTroca] = temporario;
        }
    }

    private Transform ObterAlvo()
    {
        if (_alvoPadrao != null) return _alvoPadrao;

        GameObject jogador = GameObject.FindGameObjectWithTag("Player");
        return jogador != null ? jogador.transform : null;
    }

    private void Disparar()
    {
        Vector3 posicaoLane = _avisoLanes.ObterPosicao(_laneEscolhida);
        Vector2 destino = new Vector2(_xAlvoTravado, posicaoLane.y);
        Vector2 direcao = destino - (Vector2)_projectileRoot.position;
        if (direcao.sqrMagnitude <= 0f)
            direcao = Vector2.left;

        ProjetilNecromante projetil = Instantiate(_prefabProjetil, _projectileRoot.position, Quaternion.identity);
        projetil.transform.localScale *= Mathf.Max(0.1f, _escalaVisual);
        projetil.Configurar(direcao, _velocidadeProjetil, _danoProjetil);

        _projeteisAtivos.RemoveAll(item => item == null);
        _projeteisAtivos.Add(projetil);
        _aoDisparar?.Invoke();
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
        _ordemLanes.Clear();
        _alvoAtual = null;
        _laneEscolhida = -1;
        _ultimaLane = -1;
        _disparosRealizados = 0;
        _xAlvoTravado = 0f;
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
        foreach (ProjetilNecromante projetil in _projeteisAtivos)
        {
            if (projetil != null)
                Destroy(projetil.gameObject);
        }
        _projeteisAtivos.Clear();
    }

    private void OnDisable()
    {
        Cancelar();
    }

    private void OnValidate()
    {
        _quantidadeDisparos = Mathf.Max(1, _quantidadeDisparos);
        _tempoPreparacao = Mathf.Max(0f, _tempoPreparacao);
        _tempoAvisoPorDisparo = Mathf.Max(0f, _tempoAvisoPorDisparo);
        _intervaloEntreDisparos = Mathf.Max(0f, _intervaloEntreDisparos);
        _tempoRecuperacao = Mathf.Max(0f, _tempoRecuperacao);
        _velocidadeProjetil = Mathf.Max(0.1f, _velocidadeProjetil);
        _danoProjetil = Mathf.Max(1, _danoProjetil);
        _escalaVisual = Mathf.Max(0.1f, _escalaVisual);
    }
}
