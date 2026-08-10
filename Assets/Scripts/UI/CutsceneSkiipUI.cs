using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CutsceneSkipUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject skipGroup;
    public Image fillImage;
    public TextMeshProUGUI skipText;

    [Header("Configuração")]
    public float tempoSegurando;

    private float tempoAtual = 0f;
    private bool segurando = false;

    private void Start()
    {
        fillImage.fillAmount = 0f;
        skipGroup.SetActive(false);
    }

    private void Update()
    {
        bool pressionando = Input.GetKey(KeyCode.Space);

        if (pressionando)
        {
            if (!segurando)
            {
                segurando = true;
                skipGroup.SetActive(true);
            }

            tempoAtual += Time.deltaTime;
            fillImage.fillAmount = tempoAtual / tempoSegurando;

            if (tempoAtual >= tempoSegurando)
            {
                // dispara evento para o CutsceneManager
                if (CutsceneManager.instance != null)
                {
                    CutsceneManager.instance.PularCutscene();
                }

            }
        }
        else
        {
            segurando = false;
            tempoAtual = 0f;
            fillImage.fillAmount = 0f;

            if (skipGroup.activeSelf)
                skipGroup.SetActive(false);
        }
    }
}
