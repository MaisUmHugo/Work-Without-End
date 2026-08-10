using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    private INPUTS inputs;

    [Header("Painéis")]
    [SerializeField] private GameObject FundoCinza;
    [SerializeField] private GameObject painelPause;
    [SerializeField] private GameObject painelConfirmacao;
    //[SerializeField] private GameObject painelMenuInicial;

    public static bool JogoPausado { get; private set; }
    private System.Action acaoConfirmada; 

    private void Awake()
    {
        inputs = new INPUTS();

        inputs.Gameplay.Pause.performed += ctx =>
        {
            if (JogoPausado)
                FecharPause();
            else
                AbrirPause();
        };

        inputs.UI.Cancel.performed += ctx =>
        {
            if (painelConfirmacao.activeSelf)
                BotaoConfirmarNao();
        };
    }

    private void OnEnable()
    {
        inputs.Enable();
    }

    private void OnDisable()
    {
        inputs.Disable();
    }

    public void AbrirPause()
    {
        JogoPausado = true;
        Time.timeScale = 0f;

        FundoCinza.SetActive(true);
        painelPause.SetActive(true);
        painelConfirmacao.SetActive(false);

        Debug.Log("Pause aberto");
    }

    public void FecharPause()
    {
        JogoPausado = false;
        Time.timeScale = 1f;

        painelPause.SetActive(false);
        FundoCinza.SetActive(false);
        painelConfirmacao.SetActive(false);
        //painelMenuInicial.SetActive(true);

        Debug.Log("Pause fechado");
    }

    // --- Botões ---
    public void BotaoContinuar()
    {
        FecharPause();
    }

    public void BotaoMenuPrincipal()
    {
        MostrarConfirmacao(() =>
        {
            JogoPausado = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene("MenuPrincipal");
        });
    }

    public void BotaoReiniciar()
    {
        MostrarConfirmacao(() =>
        {
            JogoPausado = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        });
    }

    private void MostrarConfirmacao(System.Action acao)
    {
        painelPause.SetActive(false);
        painelConfirmacao.SetActive(true);

        acaoConfirmada = acao;
    }

    public void BotaoConfirmarSim()
    {
        acaoConfirmada?.Invoke();
        acaoConfirmada = null;
    }

    public void BotaoConfirmarNao()
    {
        painelConfirmacao.SetActive(false);
        painelPause.SetActive(true);

        acaoConfirmada = null;
    }

    private void OnDestroy()
    {
        if (JogoPausado)
        {
            Time.timeScale = 1f;
            JogoPausado = false;
        }
    }
}
