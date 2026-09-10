using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class AtaqueTiroCarregadoNecromante : AtaqueBossBase
{
    private enum EtapaAtaque
    {
        Inativo,
        Carregando,
        Recuperando
    }

    [Header("Referencias")]
    [SerializeField] private ProjetilNecromante _prefabProjetil;
    [SerializeField] private Transform _projectileRoot;
    [SerializeField] private AvisoLanesBoss _avisoLanes;
    [SerializeField] private Transform _pontoAviso;
    [SerializeField] private GameObject _prefabAviso;
    [SerializeField] private Transform _alvoPadrao;
    [Header("Selecao das lanes")]
    [SerializeField, Range(0f, 1f)] private float _chanceMirarJogador = 0.85f;
    [Header("Tempos em segundos")]
    [SerializeField, Min(0f)] private float _tempoCarregamento = 1.5f;
    [SerializeField, Min(0f)] private float _tempoRecuperacao = 0.6f;
    [Header("Projeteis placeholder")]
    [SerializeField, Min(0.1f)] private float _velocidadeProjetil = 25f;
    [SerializeField, Min(1)] private int _danoProjetil = 1;
    [SerializeField, Min(0.1f)] private float _escalaVisual = 1.5f;
    [Header("Integracoes futuras")]
    [SerializeField] private UnityEvent _aoIniciarCarregamento = new UnityEvent();
    [SerializeField] private UnityEvent _aoDisparar = new UnityEvent();
    [SerializeField] private UnityEvent _aoFinalizar = new UnityEvent();

    private readonly List<ProjetilNecromante> _projeteisAtivos = new List<ProjetilNecromante>();
    private Transform _alvoAtual;
    private GameObject _avisoAtual;
    private ParLanes _parEscolhido;
    private float _xAlvoTravado;
    private float _tempoRestante;
    private EtapaAtaque _etapa;

    public override bool EmExecucao => _etapa != EtapaAtaque.Inativo;
    public override bool EmPreparacao => _etapa == EtapaAtaque.Carregando;

    public override bool Inicializar()
    {
        Cancelar();
        RestaurarDisponibilidade();

        if (_prefabProjetil == null || _projectileRoot == null || _avisoLanes == null
            || _pontoAviso == null || _projectileRoot.IsChildOf(transform) == false
            || _pontoAviso.IsChildOf(transform) == false || _avisoLanes.gameObject != gameObject)
        {
            Debug.LogError("Necromante: configure projeteis, lanes e avisos do tiro carregado.", this);
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
            Debug.LogWarning("Necromante: jogador nao encontrado para o tiro carregado.", this);
            return false;
        }

        _xAlvoTravado = _alvoAtual.position.x;
        int laneAlvo = _avisoLanes.ObterLaneMaisProxima(_alvoAtual.position);
        if (!SeletorParLanes.TentarSortear(
                _avisoLanes.QuantidadeLanes,
                laneAlvo,
                _chanceMirarJogador,
                out _parEscolhido)
            || !_avisoLanes.Exibir(_parEscolhido.Primeira, _parEscolhido.Segunda))
        {
            Debug.LogWarning("Necromante: nao foi possivel selecionar o par de lanes do tiro carregado.", this);
            return false;
        }

        CriarAviso();
        _tempoRestante = Mathf.Max(0f, _tempoCarregamento);
        _etapa = EtapaAtaque.Carregando;
        _aoIniciarCarregamento?.Invoke();
        return true;
    }

    public override void Atualizar(float deltaTime)
    {
        if (!EmExecucao || deltaTime <= 0f) return;

        if (_etapa == EtapaAtaque.Carregando)
            _avisoLanes.Atualizar(deltaTime);

        _tempoRestante -= deltaTime;
        if (_tempoRestante > 0f) return;

        if (_etapa == EtapaAtaque.Carregando)
        {
            Disparar();
            RemoverAvisos();
            _tempoRestante = Mathf.Max(0f, _tempoRecuperacao);
            _etapa = EtapaAtaque.Recuperando;

            if (_tempoRestante <= 0f)
                FinalizarExecucao(true);
            return;
        }

        FinalizarExecucao(true);
    }

    private Transform ObterAlvo()
    {
        if (_alvoPadrao != null) return _alvoPadrao;

        GameObject jogador = GameObject.FindGameObjectWithTag("Player");
        return jogador != null ? jogador.transform : null;
    }

    private void Disparar()
    {
        CriarProjetil(_parEscolhido.Primeira);
        CriarProjetil(_parEscolhido.Segunda);
        _aoDisparar?.Invoke();
    }

    private void CriarProjetil(int indiceLane)
    {
        Vector3 posicaoLane = _avisoLanes.ObterPosicao(indiceLane);
        Vector2 destino = new Vector2(_xAlvoTravado, posicaoLane.y);
        Vector2 direcao = destino - (Vector2)_projectileRoot.position;
        if (direcao.sqrMagnitude <= 0f)
            direcao = Vector2.left;

        ProjetilNecromante projetil = Instantiate(_prefabProjetil, _projectileRoot.position, Quaternion.identity);
        projetil.transform.localScale *= Mathf.Max(0.1f, _escalaVisual);
        projetil.Configurar(direcao, _velocidadeProjetil, _danoProjetil);

        _projeteisAtivos.RemoveAll(item => item == null);
        _projeteisAtivos.Add(projetil);
    }

    private void CriarAviso()
    {
        if (_prefabAviso == null || _pontoAviso == null) return;

        _avisoAtual = Instantiate(_prefabAviso, _pontoAviso.position, Quaternion.identity);
        _avisoAtual.transform.SetParent(_pontoAviso, true);
    }

    private void RemoverAvisos()
    {
        _avisoLanes?.Ocultar();
        if (_avisoAtual != null)
            Destroy(_avisoAtual);
        _avisoAtual = null;
    }

    private void FinalizarExecucao(bool iniciarCooldown)
    {
        bool estavaEmExecucao = EmExecucao;
        RemoverAvisos();
        _alvoAtual = null;
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
        _chanceMirarJogador = Mathf.Clamp01(_chanceMirarJogador);
        _tempoCarregamento = Mathf.Max(0f, _tempoCarregamento);
        _tempoRecuperacao = Mathf.Max(0f, _tempoRecuperacao);
        _velocidadeProjetil = Mathf.Max(0.1f, _velocidadeProjetil);
        _danoProjetil = Mathf.Max(1, _danoProjetil);
        _escalaVisual = Mathf.Max(0.1f, _escalaVisual);
    }
}
