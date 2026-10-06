using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ControladorTransicaoCenario : MonoBehaviour
{
    private class EstadoHud
    {
        public CanvasGroup Grupo;
        public float Alpha;
        public bool Interagivel;
        public bool BloqueiaRaycasts;
    }

    [Header("Fade")]
    [SerializeField] private SpriteRenderer _fadeRenderer;
    [SerializeField, Min(0f)] private float _tempoFadeIn = 0.5f;
    [SerializeField, Min(0f)] private float _tempoVisivel = 1f;
    [SerializeField, Min(0f)] private float _tempoFadeOut = 0.5f;
    [SerializeField] private Color _corCidade = new Color(0.314f, 0.247f, 0.345f, 1f);
    [SerializeField] private Color _corFloresta = new Color(0.094f, 0.137f, 0.118f, 1f);

    private readonly List<Parallax> _camadas = new List<Parallax>();
    private readonly List<EstadoHud> _huds = new List<EstadoHud>();
    private Coroutine _rotina;
    private int _cenarioAtual;
    private int _cenarioSolicitado;
    private bool _estadoGameplaySalvo;

    public bool EmTransicao => _estadoGameplaySalvo;
    public int CenarioAtual => _cenarioAtual;
    public int CenarioSolicitado => _cenarioSolicitado;

    private void Awake()
    {
        OcultarFade();
    }

    private void Start()
    {
        foreach (Parallax camada in GetComponentsInChildren<Parallax>(true))
        {
            if (camada.isActiveAndEnabled)
                RegistrarCamada(camada);
        }
        AplicarCenario(_cenarioAtual);
    }

    private void Update()
    {
        if (BloqueioGameplay.EstaAtivo(MotivoBloqueioGameplay.GameOver))
        {
            if (EmTransicao)
                CancelarTransicao();
            return;
        }
        if (_rotina == null && _cenarioSolicitado != _cenarioAtual
            && !BloqueioGameplay.BloqueadoSemTransicao && Time.timeScale > 0f)
        {
            if (_fadeRenderer == null)
                AplicarCenario(_cenarioSolicitado);
            else
                _rotina = StartCoroutine(ExecutarTransicao());
        }
    }

    public void RegistrarCamada(Parallax camada)
    {
        if (camada == null || _camadas.Contains(camada)) return;
        _camadas.Add(camada);
        camada.AplicarIndiceMaterial(_cenarioAtual);
    }

    public void DesregistrarCamada(Parallax camada)
    {
        _camadas.Remove(camada);
    }

    public void SolicitarCenario(int indiceCenario)
    {
        _cenarioSolicitado = Mathf.Clamp(indiceCenario, 0, 1);
    }

    [ContextMenu("Cenario/Cidade (Play)")]
    private void SolicitarCidade()
    {
        if (Application.isPlaying)
            SolicitarCenario(0);
    }

    [ContextMenu("Cenario/Floresta Morta (Play)")]
    private void SolicitarFloresta()
    {
        if (Application.isPlaying)
            SolicitarCenario(1);
    }

    private IEnumerator ExecutarTransicao()
    {
        _estadoGameplaySalvo = true;
        BloqueioGameplay.Definir(MotivoBloqueioGameplay.TransicaoCenario, true);
        OcultarHuds();
        try
        {
            // Reune os pedidos das diferentes camadas antes de iniciar o fade.
            yield return null;
            Color cor = _cenarioSolicitado == 0 ? _corCidade : _corFloresta;
            yield return ExecutarFade(cor, 0f, 1f, _tempoFadeIn);
            AplicarCenario(_cenarioSolicitado);
            yield return EsperarTempo(_tempoVisivel);
            if (_cenarioSolicitado != _cenarioAtual)
                AplicarCenario(_cenarioSolicitado);
            yield return ExecutarFade(cor, 1f, 0f, _tempoFadeOut);
        }
        finally
        {
            RestaurarEstado();
            _rotina = null;
        }
    }

    private IEnumerator ExecutarFade(Color cor, float inicio, float fim, float duracao)
    {
        float tempo = 0f;
        while (tempo < duracao)
        {
            if (!BloqueioGameplay.BloqueadoSemTransicao)
                tempo += Time.unscaledDeltaTime;
            cor.a = Mathf.Lerp(inicio, fim, duracao > 0f ? tempo / duracao : 1f);
            if (_fadeRenderer != null)
                _fadeRenderer.color = cor;
            yield return null;
        }
        cor.a = fim;
        if (_fadeRenderer != null)
            _fadeRenderer.color = cor;
    }

    private IEnumerator EsperarTempo(float duracao)
    {
        float tempo = 0f;
        while (tempo < duracao)
        {
            if (!BloqueioGameplay.BloqueadoSemTransicao)
                tempo += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void AplicarCenario(int indice)
    {
        _cenarioAtual = indice;
        _camadas.RemoveAll(camada => camada == null);
        foreach (Parallax camada in _camadas)
            camada.AplicarIndiceMaterial(indice);
    }

    private void OcultarHuds()
    {
        _huds.Clear();
        HashSet<GameObject> gruposEncontrados = new HashSet<GameObject>();
        foreach (MonoBehaviour componente in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!(componente is IHudTransicaoCenario fonteHud)) continue;
            GameObject objeto = fonteHud.GrupoHudTransicao;
            if (objeto == null || !gruposEncontrados.Add(objeto)) continue;
            CanvasGroup grupo = objeto.GetComponent<CanvasGroup>();
            if (grupo == null)
            {
                Debug.LogWarning("Transicao: configure um CanvasGroup no prefab da HUD.", objeto);
                continue;
            }
            _huds.Add(new EstadoHud
            {
                Grupo = grupo,
                Alpha = grupo.alpha,
                Interagivel = grupo.interactable,
                BloqueiaRaycasts = grupo.blocksRaycasts
            });
            grupo.alpha = 0f;
            grupo.interactable = false;
            grupo.blocksRaycasts = false;
        }
    }

    private void RestaurarEstado()
    {
        OcultarFade();
        foreach (EstadoHud estado in _huds)
        {
            if (estado.Grupo == null) continue;
            estado.Grupo.alpha = estado.Alpha;
            estado.Grupo.interactable = estado.Interagivel;
            estado.Grupo.blocksRaycasts = estado.BloqueiaRaycasts;
        }
        _huds.Clear();
        if (!_estadoGameplaySalvo) return;
        _estadoGameplaySalvo = false;
        BloqueioGameplay.Definir(MotivoBloqueioGameplay.TransicaoCenario, false);
    }

    private void OcultarFade()
    {
        if (_fadeRenderer == null) return;
        Color cor = _fadeRenderer.color;
        cor.a = 0f;
        _fadeRenderer.color = cor;
    }

    public void CancelarTransicao()
    {
        if (_rotina != null)
            StopCoroutine(_rotina);
        _rotina = null;
        _cenarioSolicitado = _cenarioAtual;
        RestaurarEstado();
    }

    private void OnDisable()
    {
        CancelarTransicao();
    }

    private void OnValidate()
    {
        _tempoFadeIn = Mathf.Max(0f, _tempoFadeIn);
        _tempoVisivel = Mathf.Max(0f, _tempoVisivel);
        _tempoFadeOut = Mathf.Max(0f, _tempoFadeOut);
    }
}
