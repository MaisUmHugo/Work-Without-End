using System;
using UnityEngine;

[DisallowMultipleComponent]
public class VidaBoss : MonoBehaviour
{
    [SerializeField, Min(1)] private int _vidaMaxima = 10;

    [SerializeField, Tooltip("Valor exibido para depuracao durante o Play Mode.")] private int _vidaAtual;
    [SerializeField, Tooltip("Estado exibido para depuracao durante o Play Mode.")] private bool _vulneravel;
    private bool _esgotamentoNotificado;

    public int VidaMaxima => _vidaMaxima;
    public int VidaAtual => _vidaAtual;
    public bool Vulneravel => _vulneravel;
    public bool Esgotada => _vidaAtual <= 0;

    public event Action<int, int> VidaAlterada;
    public event Action<bool> VulnerabilidadeAlterada;
    public event Action<int> DanoRecebido;
    public event Action<int> DanoBloqueado;
    public event Action VidaEsgotada;

    private void Awake()
    {
        RestaurarVida();
    }

    public void RestaurarVida()
    {
        _vidaMaxima = Mathf.Max(1, _vidaMaxima);
        _vidaAtual = _vidaMaxima;
        _esgotamentoNotificado = false;
        DefinirVulneravel(false);
        VidaAlterada?.Invoke(_vidaAtual, _vidaMaxima);
    }

    public void DefinirVulneravel(bool vulneravel)
    {
        bool novoValor = vulneravel && !Esgotada;
        if (_vulneravel == novoValor) return;

        _vulneravel = novoValor;
        VulnerabilidadeAlterada?.Invoke(_vulneravel);
    }

    public bool TentarReceberDano(int dano)
    {
        if (dano <= 0 || Esgotada) return false;

        if (!_vulneravel)
        {
            DanoBloqueado?.Invoke(dano);
            return false;
        }

        int vidaAnterior = _vidaAtual;
        _vidaAtual = Mathf.Max(0, _vidaAtual - dano);
        int danoAplicado = vidaAnterior - _vidaAtual;

        DanoRecebido?.Invoke(danoAplicado);
        VidaAlterada?.Invoke(_vidaAtual, _vidaMaxima);

        if (Esgotada && !_esgotamentoNotificado)
        {
            _esgotamentoNotificado = true;
            DefinirVulneravel(false);
            VidaEsgotada?.Invoke();
        }

        return true;
    }

    private void OnValidate()
    {
        _vidaMaxima = Mathf.Max(1, _vidaMaxima);
    }
}
