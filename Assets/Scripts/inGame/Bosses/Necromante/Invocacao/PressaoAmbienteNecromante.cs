using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PressaoAmbienteNecromante : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private ComportamentoBossNecromante _comportamento;
    [SerializeField] private ControladorEncontroNecromante _controladorEncontro;
    [SerializeField] private SpawnerManager _spawnerManager;

    [Header("Composicao")]
    [SerializeField] private string _tagZumbi = "Zumbi";
    [SerializeField] private string _tagMaoZumbi = "MaoZumbi";
    [SerializeField, Range(0f, 1f)] private float _chanceMaoZumbi = 0.4f;
    [SerializeField, Min(1)] private int _limiteSimultaneo = 2;

    [Header("Ritmo")]
    [SerializeField] private Vector2 _intervaloSpawn = new Vector2(3.5f, 5.5f);
    [SerializeField, Min(0f)] private float _antecedenciaRetomarNaVulnerabilidade = 0.6f;

    private readonly List<GameObject> _invocadosAtivos = new List<GameObject>();
    private float _tempoAteProximoSpawn;
    private bool _pressaoEstavaLiberada;
    private bool _controlandoSpawnerConvencional;
    private float _multiplicadorVelocidadeInvocados = 1f;

    public int QuantidadeAtiva
    {
        get
        {
            RemoverReferenciasInvalidas();
            return _invocadosAtivos.Count;
        }
    }

    private void Awake()
    {
        ResolverReferencias();
        AgendarProximoSpawn();
    }

    private void Update()
    {
        if (!ResolverReferencias())
            return;

        AtualizarControleSpawnerConvencional();

        bool pressaoLiberada = _controlandoSpawnerConvencional
            && !BloqueioGameplay.Bloqueado
            && Time.timeScale > 0f
            && _comportamento.PodeGerarPressaoAmbiente(
                _antecedenciaRetomarNaVulnerabilidade);

        if (!pressaoLiberada)
        {
            _pressaoEstavaLiberada = false;
            return;
        }

        if (!_pressaoEstavaLiberada)
        {
            _pressaoEstavaLiberada = true;
            _tempoAteProximoSpawn = 0f;
        }

        RemoverReferenciasInvalidas();
        if (_invocadosAtivos.Count >= Mathf.Max(1, _limiteSimultaneo))
            return;

        _tempoAteProximoSpawn -= Time.deltaTime;
        if (_tempoAteProximoSpawn > 0f)
            return;

        InvocarPressao();
        AgendarProximoSpawn();
    }

    private bool ResolverReferencias()
    {
        if (_comportamento == null)
            _comportamento = GetComponent<ComportamentoBossNecromante>();
        if (_controladorEncontro == null)
            _controladorEncontro = GetComponent<ControladorEncontroNecromante>();
        if (_spawnerManager == null)
            _spawnerManager = SpawnerManager.instance != null
                ? SpawnerManager.instance
                : FindFirstObjectByType<SpawnerManager>();

        return _comportamento != null && _controladorEncontro != null
            && _spawnerManager != null;
    }

    private void AtualizarControleSpawnerConvencional()
    {
        EstadoBoss estado = _comportamento.EstadoAtual;
        bool encontroEmExecucao = estado != EstadoBoss.Inativo;

        if (encontroEmExecucao && !_controlandoSpawnerConvencional)
        {
            _spawnerManager.DefinirSuspensoPorEncontroBoss(true);
            _controlandoSpawnerConvencional = true;
            _pressaoEstavaLiberada = false;
            return;
        }

        if (!_controlandoSpawnerConvencional)
            return;

        bool encontroConcluido = _controladorEncontro.Concluido;
        bool encontroCancelado = estado == EstadoBoss.Inativo
            && !_controladorEncontro.Encerrado;
        if (encontroConcluido || encontroCancelado)
            RestaurarSpawnerConvencional();
    }

    private void InvocarPressao()
    {
        string tagEscolhida = Random.value < _chanceMaoZumbi
            ? _tagMaoZumbi
            : _tagZumbi;
        GameObject invocado = _spawnerManager.SpawnParaEncontroBoss(tagEscolhida);
        if (invocado != null)
        {
            AplicarVelocidadeDaFase(invocado);
            _invocadosAtivos.Add(invocado);
            Entregavel entregavel = invocado.GetComponent<Entregavel>();
            if (entregavel != null)
                entregavel.EntregaResolvida += AoResolverInvocado;
        }
    }

    public void ConfigurarFase(
        int limiteSimultaneo,
        Vector2 intervaloSpawn,
        float multiplicadorVelocidadeInvocados)
    {
        _limiteSimultaneo = Mathf.Max(1, limiteSimultaneo);
        _intervaloSpawn = intervaloSpawn;
        _multiplicadorVelocidadeInvocados = Mathf.Max(0.1f, multiplicadorVelocidadeInvocados);
        AgendarProximoSpawn();
    }

    private void AplicarVelocidadeDaFase(GameObject invocado)
    {
        Zumbi zumbi = invocado.GetComponent<Zumbi>();
        if (zumbi != null)
            zumbi.ConfigurarVelocidadeEncontroBoss(_multiplicadorVelocidadeInvocados);

        Mao_Zumbi maoZumbi = invocado.GetComponent<Mao_Zumbi>();
        if (maoZumbi != null)
            maoZumbi.ConfigurarVelocidadeEncontroBoss(_multiplicadorVelocidadeInvocados);
    }

    private void AgendarProximoSpawn()
    {
        float minimo = Mathf.Max(0f, Mathf.Min(_intervaloSpawn.x, _intervaloSpawn.y));
        float maximo = Mathf.Max(minimo, Mathf.Max(_intervaloSpawn.x, _intervaloSpawn.y));
        _tempoAteProximoSpawn = Random.Range(minimo, maximo);
    }

    private void RemoverReferenciasInvalidas()
    {
        for (int i = _invocadosAtivos.Count - 1; i >= 0; i--)
        {
            if (_invocadosAtivos[i] == null)
                _invocadosAtivos.RemoveAt(i);
        }
    }

    private void AoResolverInvocado(Entregavel invocado)
    {
        if (invocado != null)
            invocado.EntregaResolvida -= AoResolverInvocado;
        _invocadosAtivos.Remove(invocado != null ? invocado.gameObject : null);
    }

    private void DesinscreverInvocados()
    {
        for (int i = _invocadosAtivos.Count - 1; i >= 0; i--)
        {
            GameObject invocado = _invocadosAtivos[i];
            if (invocado == null) continue;

            Entregavel entregavel = invocado.GetComponent<Entregavel>();
            if (entregavel != null)
                entregavel.EntregaResolvida -= AoResolverInvocado;
        }

        _invocadosAtivos.Clear();
    }

    private void RestaurarSpawnerConvencional()
    {
        if (_spawnerManager != null)
            _spawnerManager.DefinirSuspensoPorEncontroBoss(false);

        _controlandoSpawnerConvencional = false;
        _pressaoEstavaLiberada = false;
        AgendarProximoSpawn();
    }

    private void OnDisable()
    {
        DesinscreverInvocados();
        RestaurarSpawnerConvencional();
    }

    private void OnValidate()
    {
        _chanceMaoZumbi = Mathf.Clamp01(_chanceMaoZumbi);
        _limiteSimultaneo = Mathf.Max(1, _limiteSimultaneo);
        _intervaloSpawn.x = Mathf.Max(0f, _intervaloSpawn.x);
        _intervaloSpawn.y = Mathf.Max(0f, _intervaloSpawn.y);
        _antecedenciaRetomarNaVulnerabilidade = Mathf.Max(
            0f,
            _antecedenciaRetomarNaVulnerabilidade);
    }
}
