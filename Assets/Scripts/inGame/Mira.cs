using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Mira : MonoBehaviour
{
    private Camera cam;
    private INPUTS inputs;
    private SpriteRenderer sr;

    [Header("Cooldown Visual")]
    public Image cooldownUI;
    [HideInInspector] public float cooldownProgresso;
    [HideInInspector] public bool emCooldown;

    void Awake()
    {
        cam = Camera.main;
        inputs = new INPUTS();
        sr = GetComponent<SpriteRenderer>();
    }

    void OnEnable() => inputs.Gameplay.Enable();
    void OnDisable() => inputs.Gameplay.Disable();

    void Update()
    {
        // posiciona a mira
        Vector2 pointerPos = inputs.Gameplay.Aim.ReadValue<Vector2>();
        Vector3 worldPos = cam.ScreenToWorldPoint(pointerPos);
        worldPos.z = -5f;
        transform.position = worldPos;

        // UI do cooldown
        if (cooldownUI != null)
        {
            cooldownUI.fillAmount = 1f - cooldownProgresso;
            cooldownUI.enabled = emCooldown;
        }

        // troca de cor quando estiver em cooldown
        Color corNormal = Color.white;
        Color corCooldown = new Color(0.55f, 0.55f, 0.55f, 1f);

        sr.color = Color.Lerp(corNormal, corCooldown, cooldownProgresso);
    }
}
