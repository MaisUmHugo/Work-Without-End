using UnityEngine;

public class Caixa : MonoBehaviour
{
    [Header("Configuração")]
    public float tempoMaximo = 5f; // some depois de 5s se não colidir

    [HideInInspector] public string layerEntregavel = "Entregavel";
    private bool _consumida;
    private void Start()
    {
        Destroy(gameObject, tempoMaximo);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer(layerEntregavel))
        {
            // só some a caixa, quem recebeu decide se ganha ponto
            TentarConsumir();
        }
    }

    public bool TentarConsumir()
    {
        if (_consumida) return false;

        _consumida = true;
        Destroy(gameObject);
        return true;
    }
}
