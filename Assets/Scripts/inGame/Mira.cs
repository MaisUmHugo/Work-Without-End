using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Mira : MonoBehaviour
{
    private Camera cam;
    private INPUTS inputs;
    private SpriteRenderer sr;
    private Color corNormal;

    [Header("Cooldown Visual")]
    public Image cooldownUI;
    [HideInInspector] public float cooldownProgresso;
    [HideInInspector] public bool emCooldown;

    private void Awake()
    {
        cam = Camera.main;
        inputs = new INPUTS();
        sr = GetComponent<SpriteRenderer>();

        if (sr != null)
            corNormal = sr.color;
    }

    private void OnEnable() => inputs.Gameplay.Enable();
    private void OnDisable() => inputs.Gameplay.Disable();

    private void Update()
    {
        if (!BloqueioGameplay.Bloqueado)
        {
            Vector2 pointerPos = inputs.Gameplay.Aim.ReadValue<Vector2>();
            Vector3 worldPos = cam.ScreenToWorldPoint(pointerPos);
            worldPos.z = -5f;
            transform.position = worldPos;
        }

        if (cooldownUI != null)
        {
            cooldownUI.fillAmount = 1f - cooldownProgresso;
            cooldownUI.enabled = emCooldown;
        }

        if (sr == null) return;

        Color corCooldown = new Color(0.55f, 0.55f, 0.55f, corNormal.a);
        sr.color = emCooldown
            ? Color.Lerp(corCooldown, corNormal, cooldownProgresso)
            : corNormal;
    }
}