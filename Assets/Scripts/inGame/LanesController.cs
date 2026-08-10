using UnityEngine;

public class LanesController : MonoBehaviour
{
    public enum Linhas
    {
        L1, L2, L3, L4
    }

    [Header("Lanes")]
    public Transform[] linhas; // array de linhas no inspector

    public static LanesController instance { get; private set; }

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public float PosicaoX(Linhas linha)
    {
        return linhas[(int)linha].position.x;
    }

    public float PosicaoY(Linhas linha)
    {
        return linhas[(int)linha].position.y;
    }

    // Atalho para pegar direto a posição completa da linha
    public Vector3 Posicao(Linhas linha)
    {
        return linhas[(int)linha].position;
    }
}
