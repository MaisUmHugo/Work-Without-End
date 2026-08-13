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

    [Header("Tags disponíveis de entregáveis")]
    [HideInInspector]
    public List<string> todasAsTagsEntregaveis = new List<string>();

    [Header("Configurações de Spawn")]
    public List<ConfiguracaoSpawn> configuracoes = new List<ConfiguracaoSpawn>();
    private float posicaoForaCameraX; // agora só a variável

    [Header("Controle Dinâmico")]
    public bool spawnAtivo = true;
    [SerializeField] private float variacaoIntervaloSpawn = 0.3f;
    [SerializeField] private float intervaloMinimoSpawn = 1.5f;
    private float intervaloSpawnAtual = 5f;
    private float multiplicadorDificuldadeAtual = 1f;
    private bool stressTestMaximoAtivo;

    public float IntervaloMinimoSpawn => intervaloMinimoSpawn;


    private Dictionary<string, ConfiguracaoSpawn> dicionarioConfig = new Dictionary<string, ConfiguracaoSpawn>();
    private readonly List<GameObject> inimigosAtivos = new List<GameObject>();
    private float proximoSpawn;


    public List<string> tagsPermitidas = new List<string>();

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
    }

    void Update()
    {
        if (!spawnAtivo) return;

        if (Time.time >= proximoSpawn)
        {
            List<ConfiguracaoSpawn> candidatos = new List<ConfiguracaoSpawn>();

            foreach (var config in dicionarioConfig.Values)
            {
                if (PodeSpawnar(config.tagAssociada))
                    candidatos.Add(config);
            }

            if (candidatos.Count > 0)
            {
                int idx = Random.Range(0, candidatos.Count); 
                SpawnPorTag(candidatos[idx].tagAssociada);
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
            intervaloFinal = intervaloMinimoSpawn;
        }
        else
        {
            float variacao = Random.Range(-variacaoIntervaloSpawn, variacaoIntervaloSpawn);
            intervaloFinal = Mathf.Max(intervaloMinimoSpawn, intervaloSpawnAtual + variacao);
        }

        proximoSpawn = Time.time + intervaloFinal;
    }

    public void AtivarSpawn()
    {
        spawnAtivo = true;

        if (stressTestMaximoAtivo)
            AgendarProximoSpawn();
    }

    public void DesativarSpawn() => spawnAtivo = false;

    public void DefinirDificuldade(float multiplicador, float intervaloSpawn)
    {
        multiplicadorDificuldadeAtual = Mathf.Max(1f, multiplicador);
        intervaloSpawnAtual = Mathf.Max(intervaloMinimoSpawn, intervaloSpawn);
        AtualizarDificuldadeInimigosAtivos();
    }

    public void DefinirStressTestMaximo(bool ativo)
    {
        stressTestMaximoAtivo = ativo;

        if (spawnAtivo)
            AgendarProximoSpawn();
    }

    public void DefinirTagsPermitidas(List<string> tags)
    {
        tagsPermitidas = tags;
    }

    public bool PodeSpawnar(string tag)
    {
        return tagsPermitidas.Count == 0 || tagsPermitidas.Contains(tag);
    }
    public GameObject SpawnPorTag(string tag)
    {
        if (!dicionarioConfig.ContainsKey(tag))
        {
            Debug.LogWarning($"[SpawnerManager] Nenhum prefab configurado para tag: {tag}");
            return null;
        }

        if (!PodeSpawnar(tag))
        {
            Debug.Log($"[SpawnerManager] Spawn de {tag} desabilitado nesta horda.");
            return null;
        }

        ConfiguracaoSpawn config = dicionarioConfig[tag];
        Vector3 posicaoSpawn = Vector3.zero;

        switch (config.tipoSpawn)
        {
            case TipoSpawn.EmLane:
                int idx = Random.Range(0, LanesController.instance.linhas.Length);
                float y = LanesController.instance.PosicaoY((LanesController.Linhas)idx);
                posicaoSpawn = new Vector3(posicaoForaCameraX, y, 0f);
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
