using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerManager : MonoBehaviour
{
    public static SpawnerManager instance;
    public enum TipoSpawn
    {
        EmLane,
        Livre,
        Fixo
    }

    [System.Serializable]
    public class ConfiguracaoSpawn
    {
        public string nome;
        public GameObject prefab;
        public string tagAssociada;
        public TipoSpawn tipoSpawn = TipoSpawn.EmLane;
        public Transform[] pontosFixos; // usado se tipo = Fixo
    }
    [System.Serializable]
    private class FaixaQuantidadePorHorda
    {
        [SerializeField, Min(1)] private int hordaInicial = 1;
        [SerializeField, Min(0)] private int hordaFinal = 10;
        [SerializeField, Min(1)] private int quantidadeMinima = 1;
        [SerializeField, Min(1)] private int quantidadeMaxima = 1;

        public FaixaQuantidadePorHorda()
        {
        }

        public FaixaQuantidadePorHorda(int inicio, int fim, int minimo, int maximo)
        {
            hordaInicial = inicio;
            hordaFinal = fim;
            quantidadeMinima = minimo;
            quantidadeMaxima = maximo;
        }

        public bool Contem(int horda)
        {
            return horda >= hordaInicial && (hordaFinal <= 0 || horda <= hordaFinal);
        }

        public int SortearQuantidade()
        {
            int minimo = Mathf.Max(1, quantidadeMinima);
            int maximo = Mathf.Max(minimo, quantidadeMaxima);
            return Random.Range(minimo, maximo + 1);
        }
    }


    [Header("Tags disponíveis de entregáveis")]
    [HideInInspector]
    public List<string> todasAsTagsEntregaveis = new List<string>();

    [Header("Configurações de Spawn")]
    public List<ConfiguracaoSpawn> configuracoes = new List<ConfiguracaoSpawn>();
    private float posicaoForaCameraX; // agora só a variável

    [Header("Controle Dinâmico")]
    public bool spawnAtivo = true;
    [SerializeField, Min(0f)] private float variacaoIntervaloSpawn = 0.3f;
    [SerializeField, Min(0.05f)] private float intervaloMinimoSpawn = 1f;
    private float intervaloSpawnAtual = 5f;
    private float multiplicadorDificuldadeAtual = 1f;
    private bool stressTestMaximoAtivo;
    [Header("Batch normal")]
    [SerializeField] private List<FaixaQuantidadePorHorda> faixasQuantidadePorHorda = CriarFaixasQuantidadePadrao();
    [SerializeField, Min(0f)] private float intervaloInternoBatchNormal = 0.1f;
    private FaixaQuantidadePorHorda faixaQuantidadeAtual;
    private int numeroHordaAtual = 1;
    private Coroutine rotinaBatchSpawn;
    private bool batchEmAndamento;
    private int versaoBatch;
    private int ultimaLaneBatch = -1;
    private readonly List<ConfiguracaoSpawn> candidatosDoCiclo = new List<ConfiguracaoSpawn>();
    private readonly HashSet<string> tagsUsadasNoBatch = new HashSet<string>();


    [Header("Stress Test MAX")]
    [SerializeField, Min(0.05f)] private float intervaloSpawnStressMaximo = 1f;
    [SerializeField, Min(1)] private int quantidadeMinimaBatchStress = 6;
    [SerializeField, Min(1)] private int quantidadeMaximaBatchStress = 8;
    [SerializeField, Min(0f)] private float intervaloInternoBatchStress = 0.08f;
    [SerializeField, Min(0f)] private float intervaloLoteStressMaximo = 0.15f;
    [SerializeField] private List<string> tagsLoteStressMaximo = new List<string>
    {
        "Bebado",
        "MaoZumbi",
        "Zumbi",
        "Louco",
        "PCasa",
        "Malabarista",
        "MAssustado"
    };
    private Coroutine rotinaLoteStressMaximo;
    private bool loteStressPendente;

    [Header("Garantia do Malabarista")]
    [SerializeField, Min(1)] private int hordaInicioGarantiaMalabarista = 11;
    [SerializeField, Min(1)] private int quantidadePrimeirosSpawnsGarantiaMalabarista = 3;
    private const string TagMalabarista = "Malabarista";
    private bool garantiaMalabaristaAtiva;
    private bool malabaristaJaSpawnou;
    private int contadorSpawnsNormaisDaHorda;
    private int slotGarantidoMalabarista;

    public float IntervaloMinimoSpawn => Mathf.Max(0.05f, intervaloMinimoSpawn);
    public float IntervaloStressMaximo => Mathf.Max(0.05f, intervaloSpawnStressMaximo);


    private Dictionary<string, ConfiguracaoSpawn> dicionarioConfig = new Dictionary<string, ConfiguracaoSpawn>();
    private readonly List<GameObject> inimigosAtivos = new List<GameObject>();
    private float proximoSpawn;


    public List<string> tagsPermitidas = new List<string>();

    private static List<FaixaQuantidadePorHorda> CriarFaixasQuantidadePadrao()
    {
        return new List<FaixaQuantidadePorHorda>
        {
            new FaixaQuantidadePorHorda(1, 10, 1, 1),
            new FaixaQuantidadePorHorda(11, 14, 1, 3),
            new FaixaQuantidadePorHorda(15, 19, 2, 3),
            new FaixaQuantidadePorHorda(20, 24, 2, 4),
            new FaixaQuantidadePorHorda(25, 29, 3, 4),
            new FaixaQuantidadePorHorda(30, 34, 3, 5),
            new FaixaQuantidadePorHorda(35, 39, 4, 5),
            new FaixaQuantidadePorHorda(40, 44, 4, 6),
            new FaixaQuantidadePorHorda(45, 49, 5, 6),
            new FaixaQuantidadePorHorda(50, 0, 5, 7)
        };
    }

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        posicaoForaCameraX = Camera.main.ViewportToWorldPoint(new Vector3(1.2f, 0, 0)).x;

        todasAsTagsEntregaveis.Clear();
        dicionarioConfig.Clear();

        foreach (var configuracao in configuracoes)
        {
            if (configuracao == null)
            {
                Debug.LogWarning("SpawnerManager: configura\u00e7\u00e3o de spawn nula ignorada.", this);
                continue;
            }

            string tagAssociada = configuracao.tagAssociada?.Trim();

            if (string.IsNullOrEmpty(tagAssociada))
            {
                Debug.LogWarning("SpawnerManager: configura\u00e7\u00e3o com tag vazia ignorada.", this);
                continue;
            }

            if (configuracao.prefab == null)
            {
                Debug.LogWarning($"SpawnerManager: configura\u00e7\u00e3o da tag '{tagAssociada}' sem prefab foi ignorada.", this);
                continue;
            }

            if (dicionarioConfig.ContainsKey(tagAssociada))
            {
                Debug.LogWarning($"SpawnerManager: tag duplicada '{tagAssociada}' ignorada. A primeira configura\u00e7\u00e3o v\u00e1lida ser\u00e1 usada.", this);
                continue;
            }

            configuracao.tagAssociada = tagAssociada;
            dicionarioConfig.Add(tagAssociada, configuracao);
            todasAsTagsEntregaveis.Add(tagAssociada);
        }
        AtualizarFaixaQuantidade(numeroHordaAtual);
    }

    void Update()
    {
        if (!spawnAtivo || BloqueioGameplay.Bloqueado) return;

        if (Time.time >= proximoSpawn && !batchEmAndamento && rotinaLoteStressMaximo == null)
        {
            candidatosDoCiclo.Clear();

            foreach (var config in dicionarioConfig.Values)
            {
                if (PodeSpawnar(config.tagAssociada))
                    candidatosDoCiclo.Add(config);
            }

            if (candidatosDoCiclo.Count > 0)
            {
                IniciarBatchSpawn(SortearQuantidadeCiclo());
            }
            else
            {
                Debug.LogWarning("[SpawnerManager] Nenhum inimigo elegível para spawn nesta horda!");
            }

            AgendarProximoSpawn();
        }
    }


    private void AgendarProximoSpawn()
    {
        float intervaloFinal;

        if (stressTestMaximoAtivo)
        {
            intervaloFinal = IntervaloStressMaximo;
        }
        else
        {
            float variacao = Random.Range(-variacaoIntervaloSpawn, variacaoIntervaloSpawn);
            intervaloFinal = Mathf.Max(IntervaloMinimoSpawn, intervaloSpawnAtual + variacao);
        }

        proximoSpawn = Time.time + intervaloFinal;
    }

    public void AtivarSpawn()
    {
        spawnAtivo = true;

        if (stressTestMaximoAtivo)
        {
            IniciarLoteStressSePendente();
            AgendarProximoSpawn();
        }
    }

    public void DesativarSpawn()
    {
        spawnAtivo = false;
        CancelarBatchAtivo();
        CancelarLoteStressMaximo();
    }

    public void DefinirDificuldade(float multiplicador, float intervaloSpawn)
    {
        multiplicadorDificuldadeAtual = Mathf.Max(1f, multiplicador);
        intervaloSpawnAtual = Mathf.Max(IntervaloMinimoSpawn, intervaloSpawn);
        AtualizarDificuldadeInimigosAtivos();
    }

    public void DefinirStressTestMaximo(bool ativo)
    {
        CancelarBatchAtivo();
        CancelarLoteStressMaximo();

        stressTestMaximoAtivo = ativo;
        loteStressPendente = ativo;

        if (stressTestMaximoAtivo)
            IniciarLoteStressSePendente();

        if (spawnAtivo)
            AgendarProximoSpawn();
    }

    public void DefinirTagsPermitidas(List<string> tags)
    {
        tagsPermitidas = tags;
    }

    public void IniciarHorda(int numeroHorda)
    {
        CancelarBatchAtivo();
        numeroHordaAtual = Mathf.Max(1, numeroHorda);
        AtualizarFaixaQuantidade(numeroHordaAtual);

        garantiaMalabaristaAtiva = numeroHordaAtual >= hordaInicioGarantiaMalabarista
            && dicionarioConfig.ContainsKey(TagMalabarista);
        malabaristaJaSpawnou = false;
        contadorSpawnsNormaisDaHorda = 0;

        int quantidadeSlots = Mathf.Max(1, quantidadePrimeirosSpawnsGarantiaMalabarista);
        slotGarantidoMalabarista = garantiaMalabaristaAtiva
            ? Random.Range(1, quantidadeSlots + 1)
            : 0;

        if (numeroHordaAtual >= hordaInicioGarantiaMalabarista && !garantiaMalabaristaAtiva)
            Debug.LogWarning($"[SpawnerManager] Não foi possível garantir {TagMalabarista}: configuração de spawn não encontrada.", this);
    }

    private void IniciarBatchSpawn(int quantidade)
    {
        if (batchEmAndamento || quantidade <= 0)
            return;

        batchEmAndamento = true;
        tagsUsadasNoBatch.Clear();
        ultimaLaneBatch = -1;

        int versaoAtual = ++versaoBatch;
        float intervaloInterno = stressTestMaximoAtivo
            ? intervaloInternoBatchStress
            : intervaloInternoBatchNormal;

        rotinaBatchSpawn = StartCoroutine(ExecutarBatchSpawn(
            quantidade,
            intervaloInterno,
            stressTestMaximoAtivo,
            numeroHordaAtual,
            versaoAtual));
    }

    private IEnumerator ExecutarBatchSpawn(
        int quantidade,
        float intervaloInterno,
        bool stressDoBatch,
        int hordaDoBatch,
        int versaoDoBatch)
    {
        for (int i = 0; i < quantidade; i++)
        {
            while (BloqueioGameplay.Bloqueado
                && BatchContinuaValido(stressDoBatch, hordaDoBatch, versaoDoBatch))
            {
                yield return null;
            }

            if (!BatchContinuaValido(stressDoBatch, hordaDoBatch, versaoDoBatch))
                yield break;

            SpawnNormal();

            if (i < quantidade - 1 && intervaloInterno > 0f)
                yield return new WaitForSeconds(intervaloInterno);
        }

        yield return null;

        if (versaoDoBatch == versaoBatch)
        {
            rotinaBatchSpawn = null;
            batchEmAndamento = false;
        }
    }

    private bool BatchContinuaValido(bool stressDoBatch, int hordaDoBatch, int versaoDoBatch)
    {
        return spawnAtivo
            && stressTestMaximoAtivo == stressDoBatch
            && numeroHordaAtual == hordaDoBatch
            && versaoBatch == versaoDoBatch;
    }

    private int SortearQuantidadeCiclo()
    {
        if (stressTestMaximoAtivo)
        {
            int minimo = Mathf.Max(1, quantidadeMinimaBatchStress);
            int maximo = Mathf.Max(minimo, quantidadeMaximaBatchStress);
            return Random.Range(minimo, maximo + 1);
        }

        return faixaQuantidadeAtual != null
            ? faixaQuantidadeAtual.SortearQuantidade()
            : 1;
    }

    private void AtualizarFaixaQuantidade(int numeroHorda)
    {
        faixaQuantidadeAtual = null;

        if (faixasQuantidadePorHorda == null)
            return;

        for (int i = 0; i < faixasQuantidadePorHorda.Count; i++)
        {
            FaixaQuantidadePorHorda faixa = faixasQuantidadePorHorda[i];
            if (faixa != null && faixa.Contem(numeroHorda))
            {
                faixaQuantidadeAtual = faixa;
                return;
            }
        }
    }

    private ConfiguracaoSpawn SortearCandidatoDiverso()
    {
        int quantidadeDisponivel = 0;

        for (int i = 0; i < candidatosDoCiclo.Count; i++)
        {
            if (!tagsUsadasNoBatch.Contains(candidatosDoCiclo[i].tagAssociada))
                quantidadeDisponivel++;
        }

        if (quantidadeDisponivel == 0)
        {
            tagsUsadasNoBatch.Clear();
            quantidadeDisponivel = candidatosDoCiclo.Count;
        }

        int indiceSorteado = Random.Range(0, quantidadeDisponivel);

        for (int i = 0; i < candidatosDoCiclo.Count; i++)
        {
            ConfiguracaoSpawn candidato = candidatosDoCiclo[i];

            if (tagsUsadasNoBatch.Contains(candidato.tagAssociada))
                continue;

            if (indiceSorteado == 0)
                return candidato;

            indiceSorteado--;
        }

        return candidatosDoCiclo[0];
    }

    private void SpawnNormal()
    {
        contadorSpawnsNormaisDaHorda++;

        bool usarGarantia = garantiaMalabaristaAtiva
            && !malabaristaJaSpawnou
            && contadorSpawnsNormaisDaHorda >= slotGarantidoMalabarista;

        string tagEscolhida;
        GameObject instancia;
        int laneUtilizada;

        if (usarGarantia)
        {
            tagEscolhida = TagMalabarista;
            instancia = SpawnPorTag(tagEscolhida, true, ultimaLaneBatch, out laneUtilizada);
        }
        else
        {
            ConfiguracaoSpawn candidato = SortearCandidatoDiverso();
            tagEscolhida = candidato.tagAssociada;
            instancia = SpawnPorTag(tagEscolhida, false, ultimaLaneBatch, out laneUtilizada);
        }

        if (instancia == null)
            return;

        tagsUsadasNoBatch.Add(tagEscolhida);
        if (laneUtilizada >= 0)
            ultimaLaneBatch = laneUtilizada;

        if (tagEscolhida == TagMalabarista)
            malabaristaJaSpawnou = true;
    }

    public bool PodeSpawnar(string tag)
    {
        return tagsPermitidas.Count == 0 || tagsPermitidas.Contains(tag);
    }
    public GameObject SpawnPorTag(string tag)
    {
        int laneIgnorada;
        return SpawnPorTag(tag, false, -1, out laneIgnorada);
    }

    private GameObject SpawnForcado(string tag)
    {
        int laneIgnorada;
        return SpawnPorTag(tag, true, -1, out laneIgnorada);
    }

    private GameObject SpawnPorTag(
        string tag,
        bool ignorarRestricaoHorda,
        int laneEvitar,
        out int laneUtilizada)
    {
        laneUtilizada = -1;
        if (!dicionarioConfig.ContainsKey(tag))
        {
            Debug.LogWarning($"[SpawnerManager] Nenhum prefab configurado para tag: {tag}");
            return null;
        }

        if (!ignorarRestricaoHorda && !PodeSpawnar(tag))
        {
            Debug.Log($"[SpawnerManager] Spawn de {tag} desabilitado nesta horda.");
            return null;
        }

        ConfiguracaoSpawn config = dicionarioConfig[tag];
        Vector3 posicaoSpawn = Vector3.zero;

        switch (config.tipoSpawn)
        {
            case TipoSpawn.EmLane:
                if (LanesController.instance == null || LanesController.instance.linhas.Length == 0)
                {
                    Debug.LogWarning($"[SpawnerManager] Nenhuma lane configurada para {tag}.");
                    return null;
                }

                int idx = SortearLaneDiferente(LanesController.instance.linhas.Length, laneEvitar);
                float y = LanesController.instance.PosicaoY((LanesController.Linhas)idx);
                posicaoSpawn = new Vector3(posicaoForaCameraX, y, 0f);
                laneUtilizada = idx;
                break;

            case TipoSpawn.Livre:
                posicaoSpawn = new Vector3(
                    posicaoForaCameraX,
                    Random.Range(-3f, 3f),
                    0f);  
                    break;

            case TipoSpawn.Fixo:
                if (config.pontosFixos != null && config.pontosFixos.Length > 0)
                {
                    int idxFixo = Random.Range(0, config.pontosFixos.Length);
                    posicaoSpawn = config.pontosFixos[idxFixo].position;
                }
                else
                {
                    Debug.LogWarning($"Nenhum ponto fixo configurado para {tag}");
                    return null;
                }
                break;
        }

        GameObject go = Instantiate(config.prefab, posicaoSpawn, Quaternion.identity);
        inimigosAtivos.Add(go);
        AplicarDificuldade(go);
        return go;
    }

    private int SortearLaneDiferente(int quantidadeLanes, int laneEvitar)
    {
        if (quantidadeLanes <= 1 || laneEvitar < 0 || laneEvitar >= quantidadeLanes)
            return Random.Range(0, quantidadeLanes);

        int indice = Random.Range(0, quantidadeLanes - 1);
        return indice >= laneEvitar ? indice + 1 : indice;
    }

    private void IniciarLoteStressSePendente()
    {
        if (!loteStressPendente
            || !stressTestMaximoAtivo
            || !spawnAtivo
            || rotinaLoteStressMaximo != null)
        {
            return;
        }

        loteStressPendente = false;
        if (tagsLoteStressMaximo == null || tagsLoteStressMaximo.Count == 0)
        {
            return;
        }

        rotinaLoteStressMaximo = StartCoroutine(SpawnLoteStressMaximo());
    }

    private IEnumerator SpawnLoteStressMaximo()
    {
        if (tagsLoteStressMaximo == null || tagsLoteStressMaximo.Count == 0)
        {
            rotinaLoteStressMaximo = null;
            yield break;
        }

        for (int i = 0; i < tagsLoteStressMaximo.Count; i++)
        {
            while (BloqueioGameplay.Bloqueado && stressTestMaximoAtivo && spawnAtivo)
            {
                yield return null;
            }

            if (!stressTestMaximoAtivo || !spawnAtivo)
                break;

            string tag = tagsLoteStressMaximo[i]?.Trim();
            if (!string.IsNullOrEmpty(tag))
                SpawnForcado(tag);

            if (i < tagsLoteStressMaximo.Count - 1)
                yield return new WaitForSeconds(intervaloLoteStressMaximo);
        }

        rotinaLoteStressMaximo = null;
    }

    private void CancelarBatchAtivo()
    {
        versaoBatch++;

        if (rotinaBatchSpawn != null)
            StopCoroutine(rotinaBatchSpawn);

        rotinaBatchSpawn = null;
        batchEmAndamento = false;
        tagsUsadasNoBatch.Clear();
        ultimaLaneBatch = -1;
    }

    private void CancelarLoteStressMaximo()
    {
        if (rotinaLoteStressMaximo != null)
            StopCoroutine(rotinaLoteStressMaximo);

        rotinaLoteStressMaximo = null;
    }

    private void OnDisable()
    {
        CancelarBatchAtivo();
        CancelarLoteStressMaximo();
    }

    public IEnumerator SpawnMultiplo(string tag, int quantidade, float intervalo)
    {
        for (int i = 0; i < quantidade; i++)
        {
            SpawnPorTag(tag);
            yield return new WaitForSeconds(intervalo);
        }
    }

    private void AtualizarDificuldadeInimigosAtivos()
    {
        for (int i = inimigosAtivos.Count - 1; i >= 0; i--)
        {
            GameObject inimigo = inimigosAtivos[i];
            if (inimigo == null)
            {
                inimigosAtivos.RemoveAt(i);
                continue;
            }

            AplicarDificuldade(inimigo);
        }
    }
    private void AplicarDificuldade(GameObject inimigo)
    {
        MonoBehaviour[] componentes = inimigo.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour componente in componentes)
        {
            if (componente is IAjustavelDificuldade ajustavel)
            {
                ajustavel.AplicarDificuldade(multiplicadorDificuldadeAtual);
            }
        }
    }



}
