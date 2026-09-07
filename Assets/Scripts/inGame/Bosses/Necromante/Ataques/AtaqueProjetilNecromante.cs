using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class AtaqueProjetilNecromante : AtaqueBossBase
{
    private enum EtapaAtaque
    {
        Inativo,
        Preparando,
        Recuperando
    }

    [Header("Referencias")]
    [SerializeField] private ProjetilNecromante _prefabProjetil;
    [SerializeField] private Transform _projectileRoot;
    [SerializeField] private Transform _pontoAviso;
    [SerializeField] private GameObject _prefabAviso;
    [SerializeField] private Transform _alvoPadrao;
    [Header("Tempos em segundos")]
    [SerializeField, Min(0f)] private float _tempoPreparacao = 0.8f;
    [SerializeField, Min(0f)] private float _tempoRecuperacao = 0.35f;
    [Header("Projetil")]
    [SerializeField, Min(0.1f)] private float _velocidadeProjetil = 15f;

    private readonly List<ProjetilNecromante> _projeteisAtivos = new List<ProjetilNecromante>();
    private Transform _alvoAtual;
    private GameObject _avisoAtual;
    private float _tempoRestante;
    private EtapaAtaque _etapa;

    public override bool EmExecucao => _etapa != EtapaAtaque.Inativo;
    public override bool EmPreparacao => _etapa == EtapaAtaque.Preparando;

    public override bool Inicializar()
    {
        RemoverAviso();
        _etapa = EtapaAtaque.Inativo;

        if (_prefabProjetil == null || _projectileRoot == null || _pontoAviso == null
            || _projectileRoot.IsChildOf(transform) == false || _pontoAviso.IsChildOf(transform) == false)
        {
            Debug.LogError("Necromante: configure o prefab, ProjectileRoot e ponto de aviso do ataque.", this);
            return false;
        }

        return true;
    }

    public override bool TentarIniciar(Transform alvo)
    {
        if (EmExecucao) return false;

        _alvoAtual = alvo != null ? alvo : ObterAlvo();
        if (_alvoAtual == null)
        {
            Debug.LogWarning("Necromante: jogador nao encontrado para o ataque de projetil.", this);
            return false;
        }

        CriarAviso();
        _tempoRestante = Mathf.Max(0f, _tempoPreparacao);
        _etapa = EtapaAtaque.Preparando;
        return true;
    }

    public override void Atualizar(float deltaTime)
    {
        if (!EmExecucao || deltaTime <= 0f) return;

        _tempoRestante -= deltaTime;
        if (_tempoRestante > 0f) return;

        if (_etapa == EtapaAtaque.Preparando)
        {
            Disparar();
            RemoverAviso();
            _tempoRestante = Mathf.Max(0f, _tempoRecuperacao);
            _etapa = EtapaAtaque.Recuperando;

            if (_tempoRestante <= 0f)
                FinalizarExecucao();
            return;
        }

        FinalizarExecucao();
    }

    private Transform ObterAlvo()
    {
        if (_alvoPadrao != null) return _alvoPadrao;

        GameObject jogador = GameObject.FindGameObjectWithTag("Player");
        return jogador != null ? jogador.transform : null;
    }

    private void Disparar()
    {
        if (_alvoAtual == null || _prefabProjetil == null || _projectileRoot == null) return;

        Vector2 direcao = ((Vector2)_alvoAtual.position - (Vector2)_projectileRoot.position).normalized;
        if (direcao.sqrMagnitude <= 0f)
            direcao = Vector2.left;

        ProjetilNecromante projetil = Instantiate(
            _prefabProjetil,
            _projectileRoot.position,
            Quaternion.identity);
        projetil.Configurar(direcao, _velocidadeProjetil);
        _projeteisAtivos.RemoveAll(item => item == null);
        _projeteisAtivos.Add(projetil);
    }

    private void CriarAviso()
    {
        RemoverAviso();
        if (_prefabAviso == null || _pontoAviso == null) return;

        _avisoAtual = Instantiate(_prefabAviso, _pontoAviso.position, Quaternion.identity);
        _avisoAtual.transform.SetParent(_pontoAviso, true);
    }

    private void RemoverAviso()
    {
        if (_avisoAtual != null)
            Destroy(_avisoAtual);
        _avisoAtual = null;
    }

    private void FinalizarExecucao()
    {
        RemoverAviso();
        _alvoAtual = null;
        _tempoRestante = 0f;
        _etapa = EtapaAtaque.Inativo;
    }

    public override void Cancelar()
    {
        FinalizarExecucao();
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
        _tempoPreparacao = Mathf.Max(0f, _tempoPreparacao);
        _tempoRecuperacao = Mathf.Max(0f, _tempoRecuperacao);
        _velocidadeProjetil = Mathf.Max(0.1f, _velocidadeProjetil);
    }
}
