using UnityEngine;

public class Parallax : MonoBehaviour
{
    [Header("Parallax")]
    [Range(0f, 0.5f)] public float speed = 0.2f;
    [Header("Materiais")]
    [SerializeField] private Material material1;
    [SerializeField] private Material material2;
    [Header("Repeticao dos materiais")]
    [SerializeField] private float repeticoesMaterial1 = 1f;
    [SerializeField] private float repeticoesMaterial2 = 1f;

    private Renderer render;
    private Material mat;
    private float distance;
    private float multiplicadorAtual = 1f;
    private int materialAtual = -1;
    private Material _materialOriginal;
    private readonly Material[] _materiaisCriados = new Material[2];
    private readonly Material[] _fontesMateriais = new Material[2];
    private ControladorTransicaoCenario _controladorTransicao;

    public int IndiceMaterialAtual => materialAtual;

    private void Awake()
    {
        ResolverRenderer();
    }

    private void OnEnable()
    {
        _controladorTransicao = GetComponentInParent<ControladorTransicaoCenario>();
        if (_controladorTransicao != null)
            _controladorTransicao.RegistrarCamada(this);
    }

    private void Start()
    {
        if (materialAtual < 0)
            AplicarIndiceMaterial(_controladorTransicao != null ? _controladorTransicao.CenarioAtual : 0);
    }

    private void Update()
    {
        if (BloqueioGameplay.Bloqueado) return;
        distance = Mathf.Repeat(distance + TempoGameplay.DeltaTime * speed * multiplicadorAtual, 1f);
        AtualizarOffset();
    }

    public void AtualizarVelocidadeParallax(float novoMultiplicador)
    {
        multiplicadorAtual = Mathf.Max(0f, novoMultiplicador);
    }

    public void AtualizarMaterial(int horda)
    {
        int indiceMaterial = ((Mathf.Max(1, horda) - 1) / 6) % 2;
        if (_controladorTransicao != null)
            _controladorTransicao.SolicitarCenario(indiceMaterial);
        else
            AplicarIndiceMaterial(indiceMaterial);
    }

    public void AplicarIndiceMaterial(int indiceMaterial)
    {
        if (!ResolverRenderer()) return;
        indiceMaterial = Mathf.Clamp(indiceMaterial, 0, 1);
        Material fonte = indiceMaterial == 0 ? material1 : material2;
        if (fonte == null) fonte = _materialOriginal;
        if (fonte == null) return;

        if (_materiaisCriados[indiceMaterial] == null || _fontesMateriais[indiceMaterial] != fonte)
        {
            DestruirMaterial(_materiaisCriados[indiceMaterial]);
            _materiaisCriados[indiceMaterial] = new Material(fonte);
            _fontesMateriais[indiceMaterial] = fonte;
        }
        mat = _materiaisCriados[indiceMaterial];
        materialAtual = indiceMaterial;
        render.sharedMaterial = mat;
        AtualizarOffset();
    }

    private bool ResolverRenderer()
    {
        if (render != null) return true;
        render = GetComponent<Renderer>();
        if (render == null)
        {
            Debug.LogError("Parallax: configure um Renderer na camada.", this);
            return false;
        }
        _materialOriginal = render.sharedMaterial;
        return true;
    }

    private void AtualizarOffset()
    {
        if (mat == null || !mat.HasProperty("_MainTex")) return;
        float repeticoes = materialAtual == 0 ? repeticoesMaterial1 : repeticoesMaterial2;
        mat.SetTextureOffset("_MainTex", Vector2.right * distance * repeticoes);
    }

    private void OnDisable()
    {
        if (_controladorTransicao != null)
            _controladorTransicao.DesregistrarCamada(this);
        LiberarMateriais();
    }

    private void OnDestroy()
    {
        LiberarMateriais();
    }

    private void LiberarMateriais()
    {
        if (render != null) render.sharedMaterial = _materialOriginal;
        for (int i = 0; i < _materiaisCriados.Length; i++)
        {
            DestruirMaterial(_materiaisCriados[i]);
            _materiaisCriados[i] = null;
            _fontesMateriais[i] = null;
        }
        mat = null;
        materialAtual = -1;
    }

    private void DestruirMaterial(Material materialCriado)
    {
        if (materialCriado == null) return;
        if (Application.isPlaying) Destroy(materialCriado);
        else DestroyImmediate(materialCriado);
    }
}
