using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SeletorAtaquesBoss : MonoBehaviour
{
    [SerializeField] private AtaqueBossBase[] _ataques;
    [SerializeField] private bool _usarOrdemSequencial = true;
    [SerializeField] private bool _evitarRepeticao = true;

    private int _ultimoIndice = -1;
    private readonly HashSet<AtaqueBossBase> _ataquesPermitidos = new HashSet<AtaqueBossBase>();
    private bool _usarFiltroAtaques;

    public bool Inicializar()
    {
        if (_ataques == null || _ataques.Length == 0)
        {
            Debug.LogError("Boss: configure pelo menos um ataque no seletor.", this);
            return false;
        }

        for (int i = 0; i < _ataques.Length; i++)
        {
            AtaqueBossBase ataque = _ataques[i];
            if (ataque == null || ataque.gameObject != gameObject || !ataque.Inicializar())
            {
                Debug.LogError($"Boss: ataque invalido no indice {i} do seletor.", this);
                return false;
            }
        }

        _ultimoIndice = -1;
        return true;
    }

    public void AtualizarCooldowns(float deltaTime)
    {
        if (_ataques == null) return;

        foreach (AtaqueBossBase ataque in _ataques)
            ataque?.AtualizarCooldown(deltaTime);
    }

    public bool TentarIniciar(Transform alvo, out AtaqueBossBase ataqueIniciado)
    {
        ataqueIniciado = null;
        List<int> candidatos = ObterCandidatos();

        while (candidatos.Count > 0)
        {
            int posicao = _usarOrdemSequencial
                ? ObterProximaPosicaoSequencial(candidatos)
                : Random.Range(0, candidatos.Count);
            int indice = candidatos[posicao];
            AtaqueBossBase ataque = _ataques[indice];

            if (ataque.TentarIniciar(alvo))
            {
                _ultimoIndice = indice;
                ataqueIniciado = ataque;
                return true;
            }

            candidatos.RemoveAt(posicao);
        }

        return false;
    }

    private List<int> ObterCandidatos()
    {
        var candidatos = new List<int>();
        if (_ataques == null) return candidatos;

        for (int i = 0; i < _ataques.Length; i++)
        {
            AtaqueBossBase ataque = _ataques[i];
            if (ataque != null && ataque.isActiveAndEnabled && ataque.Disponivel
                && (!_usarFiltroAtaques || _ataquesPermitidos.Contains(ataque)))
                candidatos.Add(i);
        }

        if (_evitarRepeticao && candidatos.Count > 1)
            candidatos.Remove(_ultimoIndice);

        return candidatos;
    }

    private int ObterProximaPosicaoSequencial(List<int> candidatos)
    {
        for (int deslocamento = 1; deslocamento <= _ataques.Length; deslocamento++)
        {
            int indiceDesejado = (_ultimoIndice + deslocamento + _ataques.Length) % _ataques.Length;
            int posicao = candidatos.IndexOf(indiceDesejado);
            if (posicao >= 0) return posicao;
        }

        return 0;
    }

    public void Reiniciar()
    {
        if (_ataques != null)
        {
            foreach (AtaqueBossBase ataque in _ataques)
            {
                if (ataque == null) continue;
                ataque.Cancelar();
                ataque.RestaurarDisponibilidade();
            }
        }

        _ultimoIndice = -1;
    }

    public bool DefinirAtaquesPermitidos(IReadOnlyList<AtaqueBossBase> ataquesPermitidos)
    {
        _ataquesPermitidos.Clear();

        if (ataquesPermitidos == null || ataquesPermitidos.Count == 0)
        {
            Debug.LogError("Boss: configure pelo menos um ataque permitido para a fase.", this);
            _usarFiltroAtaques = false;
            return false;
        }

        for (int i = 0; i < ataquesPermitidos.Count; i++)
        {
            AtaqueBossBase ataque = ataquesPermitidos[i];
            if (ataque == null || !ContemAtaque(ataque))
            {
                Debug.LogError($"Boss: ataque permitido invalido no indice {i}.", this);
                _ataquesPermitidos.Clear();
                _usarFiltroAtaques = false;
                return false;
            }

            _ataquesPermitidos.Add(ataque);
        }

        _usarFiltroAtaques = true;
        if (_ultimoIndice >= 0 && !_ataquesPermitidos.Contains(_ataques[_ultimoIndice]))
            _ultimoIndice = -1;
        return true;
    }

    public void LiberarTodosAtaques()
    {
        _ataquesPermitidos.Clear();
        _usarFiltroAtaques = false;
    }

    public bool AtaquePermitido(AtaqueBossBase ataque)
    {
        return ataque != null && (!_usarFiltroAtaques || _ataquesPermitidos.Contains(ataque));
    }

    private bool ContemAtaque(AtaqueBossBase ataque)
    {
        if (_ataques == null) return false;

        for (int i = 0; i < _ataques.Length; i++)
        {
            if (_ataques[i] == ataque)
                return true;
        }

        return false;
    }

    public void CancelarTodos()
    {
        if (_ataques == null) return;

        foreach (AtaqueBossBase ataque in _ataques)
            ataque?.Cancelar();
    }
}
