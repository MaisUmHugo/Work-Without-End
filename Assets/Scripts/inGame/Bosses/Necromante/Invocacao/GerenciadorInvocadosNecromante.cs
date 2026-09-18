using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GerenciadorInvocadosNecromante : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private VidaBoss _vidaBoss;
    [SerializeField] private Zumbi _prefabZumbi;
    [SerializeField] private ZumbiSombrio _prefabZumbiSombrio;
    [SerializeField] private Transform _alvoPadrao;

    [Header("Spawn")]
    [SerializeField, Range(1f, 1.5f)] private float _viewportXSpawn = 1.15f;
    [SerializeField, Min(1)] private int _limiteSimultaneo = 6;

    private readonly List<GameObject> _invocados = new List<GameObject>();

    public int QuantidadeAtiva
    {
        get
        {
            RemoverReferenciasInvalidas();
            return _invocados.Count;
        }
    }

    public int EspacosDisponiveis => Mathf.Max(0, _limiteSimultaneo - QuantidadeAtiva);

    public bool TentarInvocarZumbi(int indiceLane, out Zumbi invocado)
    {
        invocado = null;
        if (!PodeInvocar(indiceLane) || _prefabZumbi == null)
            return false;

        Vector3 posicaoSpawn = ObterPosicaoSpawn(indiceLane);
        invocado = Instantiate(_prefabZumbi, posicaoSpawn, Quaternion.identity);
        if (!invocado.ConfigurarInvocacaoBoss(indiceLane))
        {
            Destroy(invocado.gameObject);
            invocado = null;
            return false;
        }

        _invocados.Add(invocado.gameObject);
        return true;
    }

    private void OnEnable()
    {
        if (_vidaBoss != null)
            _vidaBoss.VidaEsgotada += RemoverTodos;
    }

    private void OnDisable()
    {
        if (_vidaBoss != null)
            _vidaBoss.VidaEsgotada -= RemoverTodos;

        RemoverTodos();
    }

    public bool TentarInvocarZumbiSombrio(int indiceLane, out ZumbiSombrio invocado)
    {
        invocado = null;
        RemoverReferenciasInvalidas();

        if (_prefabZumbiSombrio == null || !PodeInvocar(indiceLane))
            return false;

        Transform alvo = ObterAlvo();
        if (alvo == null)
        {
            Debug.LogWarning("Necromante: jogador nao encontrado para invocar Zumbi Sombrio.", this);
            return false;
        }

        Vector3 posicaoSpawn = ObterPosicaoSpawn(indiceLane);
        invocado = Instantiate(_prefabZumbiSombrio, posicaoSpawn, Quaternion.identity);

        if (!invocado.Inicializar(alvo, posicaoSpawn.y))
        {
            Destroy(invocado.gameObject);
            invocado = null;
            return false;
        }

        invocado.Finalizado += AoFinalizarInvocado;
        _invocados.Add(invocado.gameObject);
        return true;
    }

    public bool TentarInvocarZumbiSombrioAleatorio()
    {
        if (LanesController.instance == null || LanesController.instance.linhas.Length == 0)
            return false;

        int lane = Random.Range(0, LanesController.instance.linhas.Length);
        return TentarInvocarZumbiSombrio(lane, out _);
    }

    public void RemoverTodos()
    {
        for (int i = _invocados.Count - 1; i >= 0; i--)
        {
            GameObject invocado = _invocados[i];
            if (invocado == null) continue;

            ZumbiSombrio zumbiSombrio = invocado.GetComponent<ZumbiSombrio>();
            if (zumbiSombrio != null)
            {
                zumbiSombrio.Finalizado -= AoFinalizarInvocado;
                zumbiSombrio.Remover();
            }
            else
            {
                Destroy(invocado);
            }
        }

        _invocados.Clear();
    }

    private Transform ObterAlvo()
    {
        if (_alvoPadrao != null) return _alvoPadrao;

        GameObject jogador = GameObject.FindGameObjectWithTag("Player");
        return jogador != null ? jogador.transform : null;
    }

    private bool PodeInvocar(int indiceLane)
    {
        RemoverReferenciasInvalidas();
        return LanesController.instance != null
            && LanesController.instance.linhas != null
            && Camera.main != null
            && indiceLane >= 0
            && indiceLane < LanesController.instance.linhas.Length
            && _invocados.Count < _limiteSimultaneo;
    }

    private Vector3 ObterPosicaoSpawn(int indiceLane)
    {
        float yLane = LanesController.instance.PosicaoY((LanesController.Linhas)indiceLane);
        float xSpawn = Camera.main.ViewportToWorldPoint(new Vector3(_viewportXSpawn, 0.5f, 0f)).x;
        return new Vector3(xSpawn, yLane, 0f);
    }

    private void AoFinalizarInvocado(ZumbiSombrio invocado)
    {
        if (invocado != null)
            invocado.Finalizado -= AoFinalizarInvocado;
        _invocados.Remove(invocado != null ? invocado.gameObject : null);
    }

    private void RemoverReferenciasInvalidas()
    {
        for (int i = _invocados.Count - 1; i >= 0; i--)
        {
            if (_invocados[i] == null)
                _invocados.RemoveAt(i);
        }
    }

    private void OnValidate()
    {
        _viewportXSpawn = Mathf.Clamp(_viewportXSpawn, 1f, 1.5f);
        _limiteSimultaneo = Mathf.Max(1, _limiteSimultaneo);
    }
}
