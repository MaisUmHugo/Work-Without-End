using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HordaManager : MonoBehaviour
{
    public List<string> todasAsTagsEntregaveis;
    [Header("Referência ao Spawner")]
    public SpawnerManager spawnerManager;
    public Bola bola;

    [Header("Barra de Progresso")]
    public Slider barraProgresso; 

    private int NumeroHorda, N_Entregas;
    [Header("Controle Hordas")]
    public int E_Necessarias;
    public TextMeshProUGUI TextoHorda;
    public TextMeshProUGUI TextoEntrega;
    public float aumentovelocidade;
    public float aumentodistancia;
    public float ReduzirIntervalo;

    //private bool aguardandoInicio = false;

    [Header("Delay de Início das Hordas")]
    [Tooltip("Tempo (em segundos) antes da primeira horda começar após carregar a cena.")]
    public float delayInicial;
    [Tooltip("Tempo de espera entre o fim de uma horda e o início da próxima.")]
    public float delayEntreHordas;

    [Header("Parallax")]
    public float multiplicadorParallax;
    public float aumentoParallaxPorHorda;
    public bool aumentarParallax = true;


    private bool HordaMudou, Objetivo;
    private bool Normal = true;
    public static HordaManager instance;
    [System.Serializable]
    public class TagsPorHorda
    {
        public int horda;
        public List<string> tagsPermitidas;
    }

    [Header("Configuração de Inimigos por Horda")]
    public List<TagsPorHorda> tagsPorHorda = new List<TagsPorHorda>();

    private void Start()
    {
        aumentarParallax = PlayerPrefs.GetInt("parallaxAumentar", 1) == 1;
        NumeroHorda = 1;
        spawnerManager.DesativarSpawn();
        StartCoroutine(IniciarHordaComDelay(delayInicial));
    }
    private void Awake()
    {
        instance = this;
    }
    private void Update()
    {
        verificarhorda();
        TextoHorda.text = "Horda: " + NumeroHorda;
        TextoEntrega.text = $"Entregas:{N_Entregas}/{E_Necessarias}" ;
        AtualizarBarraProgresso();
        if (Input.GetKeyDown(KeyCode.N))
        {
            AlterarHorda();
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            int i = 0;
            Normal = false;
            while (i <= 4)
            {
                AlterarHorda();
                trocandoHorda = false;
                Normal = true;
                i++;
            }
        }

    }

    private IEnumerator IniciarHordaComDelay(float delay)
    {
        //aguardandoInicio = true;
        spawnerManager.DesativarSpawn();

        yield return new WaitForSeconds(delay);

        AtualizarInimigosPermitidos(); 
        spawnerManager.AtivarSpawn();
        //aguardandoInicio = false;
    }

    private void AtualizarBarraProgresso()
    {
        if (barraProgresso != null)
        {
            // Calcula o valor do progresso como uma porcentagem
            float progresso = (float)N_Entregas / (float)E_Necessarias;
            barraProgresso.value = progresso;  // Atualiza a barra de progresso
        }
    }
    private void Mudarcondicao()
    {
        if (HordaMudou)
        {
            // velocidade dos inimigos da horda
            float novoMultiplicador = spawnerManager.multiplicadorVelocidade + aumentovelocidade;
            bola.multiplicadorVelocidade = novoMultiplicador;
            spawnerManager.DefinirVelocidade(novoMultiplicador);
            //velocidade da distância
            float multiplicadordedistancia = spawnerManager.multiplicadorDistancia + aumentodistancia;
            spawnerManager.DefinirDistancia(multiplicadordedistancia);
            // intervalo de spawn deles
            float novoIntervalo = spawnerManager.intervaloSpawn - ReduzirIntervalo;
            if (novoIntervalo <= 1.5f)
            {
                novoIntervalo = 1.5f;
            }
            spawnerManager.DefinirIntervalo(novoIntervalo);
            HordaMudou = false;

            // aumenta levemente a velocidade do fundo (efeito de intensidade)
            if (NumeroHorda <= 16)
            {
                if (aumentarParallax)
                    multiplicadorParallax += aumentoParallaxPorHorda;
                else
                    multiplicadorParallax = 1f; // reset para a velocidade normal
            }

            // notifica todos os parallax ativos
            foreach (Parallax p in FindObjectsByType<Parallax>(FindObjectsSortMode.None))
            {
                p.AtualizarVelocidadeParallax(multiplicadorParallax);
            }
        }
    }
    private void verificarhorda()
    {
        if (N_Entregas >= E_Necessarias)
        {
            Objetivo = true;
        }
        if (Objetivo)
        {
            AlterarHorda();
        }
    }

    private bool trocandoHorda = false;

    private void AlterarHorda()
    {
        if (trocandoHorda) return; // impede sobreposição
        trocandoHorda = true;

        NumeroHorda++;
        E_Necessarias++;
        HordaMudou = true;
        Objetivo = false;
        N_Entregas = 0;

        /*if (NumeroHorda >= 10)
        {
            Final();
            return;
        }
        */

        Mudarcondicao();
        if (Normal)
        {
            StartCoroutine(DelayProximaHorda());
            Debug.Log($"[HordaManager] Esperando {delayEntreHordas}s antes da próxima horda ({NumeroHorda})");
        }
    }

    private IEnumerator DelayProximaHorda()
    {
        spawnerManager.DesativarSpawn();
        yield return new WaitForSeconds(delayEntreHordas);
        AtualizarInimigosPermitidos();
        spawnerManager.AtivarSpawn();
        trocandoHorda = false;
    }

    public void AumentarEntrega()
    {
        N_Entregas++;
        Debug.Log(N_Entregas);
    }
    private void AtualizarInimigosPermitidos()
    {
        // tenta encontrar uma configuração para a horda atual
        TagsPorHorda config = tagsPorHorda.Find(t => t.horda == NumeroHorda);

        if (config != null && config.tagsPermitidas != null && config.tagsPermitidas.Count > 0)
        {
            // usa as tags configuradas normalmente
            spawnerManager.DefinirTagsPermitidas(config.tagsPermitidas);
            Debug.Log($"[HordaManager] Tags permitidas na horda {NumeroHorda}: {string.Join(", ", config.tagsPermitidas)}");
        }
        else
        {
            // nenhuma configuração -> escolher uma tag aleatória entre todas
            if (spawnerManager.todasAsTagsEntregaveis != null && spawnerManager.todasAsTagsEntregaveis.Count > 0)
            {
                string tagAleatoria = spawnerManager.todasAsTagsEntregaveis[Random.Range(0, spawnerManager.todasAsTagsEntregaveis.Count)];
                spawnerManager.DefinirTagsPermitidas(new List<string> { tagAleatoria });

                Debug.Log($"[HordaManager] Nenhuma tag configurada para a horda {NumeroHorda}. Escolhida aleatoriamente: {tagAleatoria}");
            }
            else
            {
                Debug.LogWarning("[HordaManager] Nenhuma tag configurada e nenhuma tag possível definida no SpawnerManager!");
            }
        }
    }

    public void DefinirTagsPermitidas(List<string> tags)
    {
        spawnerManager.tagsPermitidas = tags;
        Debug.Log($"[SpawnerManager] Tags permitidas atualizadas: {string.Join(", ", tags)}");
    }
    /* private void Final()
    {
        SceneManager.LoadScene("CenaFimDemo");
    }*/

    public void DefinirAumentoParallax(bool ativado)
    {
        //Debug.Log("Toggle mandou: " + ativado);

        aumentarParallax = ativado;
        PlayerPrefs.SetInt("parallaxAumentar", ativado ? 1 : 0);

        if (!ativado)
        {
            multiplicadorParallax = 1f;
            foreach (Parallax p in FindObjectsByType<Parallax>(FindObjectsSortMode.None))
                p.AtualizarVelocidadeParallax(1f);

           // Debug.Log("Parallax DESATIVADO");
        }
        else
        {
            multiplicadorParallax = 1f + (NumeroHorda - 1) * aumentoParallaxPorHorda;

            foreach (Parallax p in FindObjectsByType<Parallax>(FindObjectsSortMode.None))
                p.AtualizarVelocidadeParallax(multiplicadorParallax);

            //Debug.Log("Parallax ATIVADO — vel=" + multiplicadorParallax);
        }
    }
}
