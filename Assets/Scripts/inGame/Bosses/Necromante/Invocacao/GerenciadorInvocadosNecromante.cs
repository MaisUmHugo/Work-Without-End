using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GerenciadorInvocadosNecromante : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private VidaBoss _vidaBoss;
    [SerializeField] private Zumbi _prefabZumbi;
    [SerializeField] private Mao_Zumbi _prefabMaoZumbi;
    [SerializeField] private ZumbiSombrio _prefabZumbiSombrio;
    [SerializeField] private Transform _alvoPadrao;

    [Header("Spawn")]
    [SerializeField, Range(1f, 1.5f)] private float _viewportXSpawn = 1.15f;
    [SerializeField, Min(1)] private int _limiteSimultaneo = 6;
    [SerializeField, Min(1)] private int _limiteZumbisSombrios = 2;

    private readonly List<GameObject> _invocados = new List<GameObject>();
    private float _multiplicadorVelocidade = 1f;

    public int QuantidadeAtiva
    {
        get
        {
            RemoverReferenciasInvalidas();
            return _invocados.Count;
        }
    }

    public int EspacosDisponiveis => Mathf.Max(0, _limiteSimultaneo - QuantidadeAtiva);
    public int LimiteSimultaneo => _limiteSimultaneo;

    public void DefinirLimiteSimultaneo(int limiteSimultaneo)
    {
        _limiteSimultaneo = Mathf.Max(1, limiteSimultaneo);
    }

    public bool TentarInvocarZumbi(int indiceLane, out Zumbi invocado)
    {
        return TentarInvocarZumbi(indiceLane, 0f, out invocado);
    }

    public bool TentarInvocarZumbi(int indiceLane, float deslocamentoHorizontal, out Zumbi invocado)
    {
        invocado = null;
        if (!PodeInvocar(indiceLane) || _prefabZumbi == null)
            return false;

        Vector3 posicaoSpawn = ObterPosicaoSpawn(indiceLane, deslocamentoHorizontal);
        invocado = Instantiate(_prefabZumbi, posicaoSpawn, Quaternion.identity);
        if (!invocado.ConfigurarInvocacaoBoss(indiceLane))
        {
            Destroy(invocado.gameObject);
            invocado = null;
            return false;
        }

        invocado.ConfigurarVelocidadeEncontroBoss(_multiplicadorVelocidade);

        RegistrarInvocado(invocado);
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
        return TentarInvocarZumbiSombrio(indiceLane, 0f, out invocado);
    }

    public bool TentarInvocarZumbiSombrio(
        int indiceLane,
        float deslocamentoHorizontal,
        out ZumbiSombrio invocado)
    {
        invocado = null;
        RemoverReferenciasInvalidas();

        if (_prefabZumbiSombrio == null || !PodeInvocar(indiceLane)
            || ContarZumbisSombriosAtivos() >= _limiteZumbisSombrios)
            return false;

        Transform alvo = ObterAlvo();
        if (alvo == null)
        {
            Debug.LogWarning("Necromante: jogador nao encontrado para invocar Zumbi Sombrio.", this);
            return false;
        }

        Vector3 posicaoSpawn = ObterPosicaoSpawn(indiceLane, deslocamentoHorizontal);
        invocado = Instantiate(_prefabZumbiSombrio, posicaoSpawn, Quaternion.identity);

        if (!invocado.Inicializar(alvo, posicaoSpawn.y))
        {
            Destroy(invocado.gameObject);
            invocado = null;
            return false;
        }

        invocado.ConfigurarMultiplicadorVelocidade(_multiplicadorVelocidade);

        invocado.Finalizado += AoFinalizarInvocado;
        _invocados.Add(invocado.gameObject);
        return true;
    }

    public bool TentarInvocarMaoZumbi(
        int indiceLane,
        float deslocamentoHorizontal,
        out Mao_Zumbi invocado)
    {
        invocado = null;
        if (!PodeInvocar(indiceLane) || _prefabMaoZumbi == null)
            return false;

        Vector3 posicaoSpawn = ObterPosicaoSpawn(indiceLane, deslocamentoHorizontal);
        invocado = Instantiate(_prefabMaoZumbi, posicaoSpawn, Quaternion.identity);
        if (!invocado.ConfigurarInvocacaoBoss(indiceLane))
        {
            Destroy(invocado.gameObject);
            invocado = null;
            return false;
        }

        invocado.ConfigurarVelocidadeEncontroBoss(_multiplicadorVelocidade);

        RegistrarInvocado(invocado);
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
                Entregavel entregavel = invocado.GetComponent<Entregavel>();
                if (entregavel != null)
                    entregavel.EntregaResolvida -= AoResolverInvocado;
                Destroy(invocado);
            }
        }

        _invocados.Clear();
    }

    public void DefinirMultiplicadorVelocidade(float multiplicadorVelocidade)
    {
        _multiplicadorVelocidade = Mathf.Max(0.1f, multiplicadorVelocidade);
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

    private Vector3 ObterPosicaoSpawn(int indiceLane, float deslocamentoHorizontal)
    {
        float yLane = LanesController.instance.PosicaoY((LanesController.Linhas)indiceLane);
        float xSpawn = Camera.main.ViewportToWorldPoint(new Vector3(_viewportXSpawn, 0.5f, 0f)).x;
        return new Vector3(xSpawn + Mathf.Max(0f, deslocamentoHorizontal), yLane, 0f);
    }

    private void AoFinalizarInvocado(ZumbiSombrio invocado)
    {
        if (invocado != null)
            invocado.Finalizado -= AoFinalizarInvocado;
        _invocados.Remove(invocado != null ? invocado.gameObject : null);
    }

    private void RegistrarInvocado(Entregavel invocado)
    {
        if (invocado == null) return;

        invocado.EntregaResolvida += AoResolverInvocado;
        _invocados.Add(invocado.gameObject);
    }

    private void AoResolverInvocado(Entregavel invocado)
    {
        if (invocado != null)
            invocado.EntregaResolvida -= AoResolverInvocado;
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

    private int ContarZumbisSombriosAtivos()
    {
        RemoverReferenciasInvalidas();
        int quantidade = 0;

        for (int i = 0; i < _invocados.Count; i++)
        {
            GameObject invocado = _invocados[i];
            if (invocado != null && invocado.GetComponent<ZumbiSombrio>() != null)
                quantidade++;
        }

        return quantidade;
    }

    private void OnValidate()
    {
        _viewportXSpawn = Mathf.Clamp(_viewportXSpawn, 1f, 1.5f);
        _limiteSimultaneo = Mathf.Max(1, _limiteSimultaneo);
        _limiteZumbisSombrios = Mathf.Max(1, _limiteZumbisSombrios);
    }
}
