using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class AvisoLanesBoss : MonoBehaviour
{
    [SerializeField] private Color _corAviso = new Color(1f, 0.15f, 0.15f, 0.38f);
    [SerializeField, Range(0f, 1f)] private float _variacaoAlpha = 0.14f;
    [SerializeField, Min(0f)] private float _pulsosPorSegundo = 4f;

    private LanesController _lanes;
    private SpriteRenderer[] _renderizadores;
    private Color[] _coresOriginais;
    private int[] _lanesAtivas = Array.Empty<int>();
    private float _tempoPulso;

    public int QuantidadeLanes => _lanes != null && _lanes.linhas != null ? _lanes.linhas.Length : 0;

    public bool Inicializar()
    {
        Ocultar();
        _lanes = LanesController.instance;
        if (_lanes == null || _lanes.linhas == null || _lanes.linhas.Length < 2)
        {
            Debug.LogError("Boss: configure pelo menos duas lanes no LanesController.", this);
            return false;
        }

        _renderizadores = new SpriteRenderer[_lanes.linhas.Length];
        _coresOriginais = new Color[_lanes.linhas.Length];

        for (int i = 0; i < _lanes.linhas.Length; i++)
        {
            Transform lane = _lanes.linhas[i];
            SpriteRenderer renderizador = lane != null ? lane.GetComponent<SpriteRenderer>() : null;
            if (renderizador == null)
            {
                Debug.LogError($"Boss: a lane {i} precisa de um SpriteRenderer para o aviso.", this);
                return false;
            }

            _renderizadores[i] = renderizador;
            _coresOriginais[i] = renderizador.color;
        }

        return true;
    }

    public int ObterLaneMaisProxima(Vector3 posicao)
    {
        if (QuantidadeLanes == 0) return -1;

        int melhorIndice = 0;
        float menorDistancia = Mathf.Abs(posicao.y - _lanes.linhas[0].position.y);
        for (int i = 1; i < _lanes.linhas.Length; i++)
        {
            float distancia = Mathf.Abs(posicao.y - _lanes.linhas[i].position.y);
            if (distancia >= menorDistancia) continue;

            menorDistancia = distancia;
            melhorIndice = i;
        }

        return melhorIndice;
    }

    public Vector3 ObterPosicao(int indiceLane)
    {
        return IndiceValido(indiceLane) ? _lanes.linhas[indiceLane].position : Vector3.zero;
    }

    public bool Exibir(params int[] indicesLanes)
    {
        if (indicesLanes == null || indicesLanes.Length == 0)
            return false;

        var lanesValidas = new List<int>();
        foreach (int indiceLane in indicesLanes)
        {
            if (!IndiceValido(indiceLane)) return false;
            if (!lanesValidas.Contains(indiceLane))
                lanesValidas.Add(indiceLane);
        }

        Ocultar();
        _lanesAtivas = lanesValidas.ToArray();
        _tempoPulso = 0f;
        AplicarCor(_corAviso.a);
        return true;
    }

    public void Atualizar(float deltaTime)
    {
        if (_lanesAtivas.Length == 0 || deltaTime <= 0f) return;

        _tempoPulso += deltaTime * Mathf.Max(0f, _pulsosPorSegundo) * Mathf.PI * 2f;
        float alpha = _corAviso.a + Mathf.Sin(_tempoPulso) * _variacaoAlpha;
        AplicarCor(Mathf.Clamp01(alpha));
    }

    public void Ocultar()
    {
        foreach (int indiceLane in _lanesAtivas)
            RestaurarCor(indiceLane);

        _lanesAtivas = Array.Empty<int>();
        _tempoPulso = 0f;
    }

    private void AplicarCor(float alpha)
    {
        foreach (int indiceLane in _lanesAtivas)
            AplicarCor(indiceLane, alpha);
    }

    private void AplicarCor(int indice, float alpha)
    {
        if (!IndiceValido(indice)) return;

        Color cor = _corAviso;
        cor.a = alpha;
        _renderizadores[indice].color = cor;
    }

    private void RestaurarCor(int indice)
    {
        if (!IndiceValido(indice) || _coresOriginais == null || indice >= _coresOriginais.Length) return;
        _renderizadores[indice].color = _coresOriginais[indice];
    }

    private bool IndiceValido(int indice)
    {
        return indice >= 0 && _renderizadores != null && indice < _renderizadores.Length
            && _renderizadores[indice] != null;
    }

    private void OnDisable()
    {
        Ocultar();
    }

    private void OnValidate()
    {
        _variacaoAlpha = Mathf.Clamp01(_variacaoAlpha);
        _pulsosPorSegundo = Mathf.Max(0f, _pulsosPorSegundo);
    }
}
