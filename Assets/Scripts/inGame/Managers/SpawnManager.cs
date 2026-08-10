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
    public List<string> todasAsTagsEntregaveis = new List<string>();

    [Header("Configurações de Spawn")]
    public List<ConfiguracaoSpawn> configuracoes = new List<ConfiguracaoSpawn>();
    private float posicaoForaCameraX; // agora só a variável

    [Header("Controle Dinâmico")]
    public float intervaloSpawn;
    public float multiplicadorVelocidade;
    public float multiplicadorDistancia;
    public bool spawnAtivo = true;
    public float intervaloaleatorio;

    private Dictionary<string, ConfiguracaoSpawn> dicionarioConfig = new Dictionary<string, ConfiguracaoSpawn>();
    private float proximoSpawn;

    private float intervaloPadrao;
    private float velocidadePadrao;
    private bool velocidadeflutuante;

    public List<string> tagsPermitidas = new List<string>();

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        posicaoForaCameraX = Camera.main.ViewportToWorldPoint(new Vector3(1.2f, 0, 0)).x;

        foreach (var c in configuracoes)
        {
            if (!dicionarioConfig.ContainsKey(c.tagAssociada))
                dicionarioConfig.Add(c.tagAssociada, c);
        }
        intervaloPadrao = intervaloSpawn;
        velocidadePadrao = multiplicadorVelocidade;
        intervaloaleatorio = Random.Range(-1.5f, 1.5f);
    }

    void Update()
    {
        if (intervaloSpawn <= 2f)
        {
            intervaloSpawn = 2f;
        }
        if (multiplicadorVelocidade >= 13f)
        {
            velocidadeflutuante = true;
        }
        if (velocidadeflutuante)
        {
            multiplicadorVelocidade = Random.Range(11f, 13f);
        }
        if (!spawnAtivo) return;

        if (Time.time >= proximoSpawn)
        {
            List<ConfiguracaoSpawn> candidatos = new List<ConfiguracaoSpawn>();

            foreach (var config in configuracoes)
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

            proximoSpawn = Time.time + intervaloSpawn + intervaloaleatorio;
        }
    }


    public void AtivarSpawn() => spawnAtivo = true;
    public void DesativarSpawn() => spawnAtivo = false;

    public void DefinirIntervalo(float intervalo) => intervaloSpawn = intervalo;
    public void DefinirVelocidade(float mult) => multiplicadorVelocidade = mult;
    
    public void DefinirDistancia(float dist) => multiplicadorDistancia = dist;

    public void ResetarConfig()
    {
        intervaloSpawn = intervaloPadrao;
        multiplicadorVelocidade = velocidadePadrao;
        spawnAtivo = true;
        tagsPermitidas.Clear();
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
        AjustarVelocidade(go);
        intervaloaleatorio = Random.Range(-1.5f, 1.5f);
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

    private void AjustarVelocidade(GameObject inimigo)
    {
        var componentes = inimigo.GetComponents<MonoBehaviour>();

        foreach (var c in componentes)
        {
            var campos = c.GetType().GetFields(System.Reflection.BindingFlags.Public |
                                               System.Reflection.BindingFlags.NonPublic |
                                               System.Reflection.BindingFlags.Instance);

            foreach (var campo in campos)
            {
                // verifica se o nome do campo contém "velocidade"
                if (campo.Name.ToLower().Contains("velocidade") && campo.FieldType == typeof(float))
                {
                    float vOriginal = (float)campo.GetValue(c);
                    campo.SetValue(c, vOriginal * multiplicadorVelocidade);
                    // Debug.Log($"[{c.GetType().Name}] {campo.Name} alterado para {vOriginal * multiplicadorVelocidade}");
                }
                if (campo.Name.ToLower().Contains("distancia") && campo.FieldType == typeof(float))
                {
                    float vOriginal = (float)campo.GetValue(c);
                    campo.SetValue(c, vOriginal + multiplicadorDistancia);
                    // Debug.Log($"[{c.GetType().Name}] {campo.Name} alterado para {vOriginal * multiplicadorVelocidade}");
                }
            }
        }
    }


    //private void AjustarVelocidade(GameObject inimigo)
    //{
    //    var componentes = inimigo.GetComponents<MonoBehaviour>();
    //    foreach (var c in componentes)
    //    {
    //        var campo = c.GetType().GetField("velocidade");
    //        if (campo != null && campo.FieldType == typeof(float))
    //        {
    //            float vOriginal = (float)campo.GetValue(c);
    //            campo.SetValue(c, vOriginal * multiplicadorVelocidade);
    //        }
    //    }
    //}

}
