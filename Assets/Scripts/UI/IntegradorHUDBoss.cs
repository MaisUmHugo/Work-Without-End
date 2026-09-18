using UnityEngine;

[DisallowMultipleComponent]
public class IntegradorHUDBoss : MonoBehaviour
{
    [Header("Identificacao")]
    [SerializeField] private string _nomeBoss = "NECROMANTE";
    [SerializeField] private HUDBoss _prefabHud;

    [Header("Fontes do boss")]
    [SerializeField] private VidaBoss _vida;
    [SerializeField] private ControladorFasesBossBase _controladorFases;
    [SerializeField, Tooltip("Componente que implementa IControladorEncontroBoss.")]
    private MonoBehaviour _controladorEncontro;

    private HUDBoss _instanciaHud;

    public HUDBoss HudAtual => _instanciaHud;

    private void Awake()
    {
        CriarHud();
    }

    private void OnEnable()
    {
        if (_instanciaHud != null)
            _instanciaHud.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (_instanciaHud != null)
            _instanciaHud.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_instanciaHud != null)
            Destroy(_instanciaHud.gameObject);
    }

    private void CriarHud()
    {
        if (_instanciaHud != null || !ReferenciasValidas()) return;

        _instanciaHud = Instantiate(_prefabHud);
        _instanciaHud.name = $"HUD {_nomeBoss}";
        _instanciaHud.ConfigurarBoss(
            _nomeBoss,
            _vida,
            _controladorFases,
            _controladorEncontro);
    }

    private bool ReferenciasValidas()
    {
        if (_prefabHud == null)
        {
            Debug.LogError("Boss: configure o prefab da HUD.", this);
            return false;
        }

        if (_vida == null || _vida.gameObject != gameObject
            || _controladorFases == null || _controladorFases.gameObject != gameObject)
        {
            Debug.LogError("Boss: configure vida e fases da mesma raiz na integracao da HUD.", this);
            return false;
        }

        if (!(_controladorEncontro is IControladorEncontroBoss)
            || _controladorEncontro.gameObject != gameObject)
        {
            Debug.LogError(
                "Boss: configure um controlador de encontro compativel da mesma raiz.",
                this);
            return false;
        }

        return true;
    }

    private void OnValidate()
    {
        if (_vida == null)
            _vida = GetComponent<VidaBoss>();
        if (_controladorFases == null)
            _controladorFases = GetComponent<ControladorFasesBossBase>();

        if (_controladorEncontro == null)
        {
            MonoBehaviour[] componentes = GetComponents<MonoBehaviour>();
            for (int i = 0; i < componentes.Length; i++)
            {
                if (componentes[i] is IControladorEncontroBoss)
                {
                    _controladorEncontro = componentes[i];
                    break;
                }
            }
        }
    }
}
