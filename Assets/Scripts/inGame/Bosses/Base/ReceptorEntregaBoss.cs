using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(VidaBoss))]
public class ReceptorEntregaBoss : MonoBehaviour
{
    [SerializeField] private VidaBoss _vida;
    [SerializeField, Min(1)] private int _danoPorEntrega = 1;

    private void Awake()
    {
        if (_vida == null)
            _vida = GetComponent<VidaBoss>();
    }

    private void OnTriggerEnter2D(Collider2D colisao)
    {
        if (!colisao.CompareTag("Caixa")) return;

        Caixa caixa = colisao.GetComponentInParent<Caixa>();
        if (caixa == null || !caixa.TentarConsumir()) return;

        _vida.TentarReceberDano(_danoPorEntrega);
    }

    private void OnValidate()
    {
        _danoPorEntrega = Mathf.Max(1, _danoPorEntrega);
    }
}
