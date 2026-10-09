using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(VidaBoss))]
public class ReceptorEntregaBoss : MonoBehaviour
{
    [SerializeField] private VidaBoss _vida;
    [SerializeField, Min(1)] private int _danoPorEntrega = 1;
    public bool PodeReceberEntrega => isActiveAndEnabled && _vida != null && _vida.Vulneravel && !_vida.Esgotada;

    private void Awake()
    {
        if (_vida == null)
            _vida = GetComponent<VidaBoss>();
    }

    private void OnTriggerEnter2D(Collider2D colisao)
    {
        if (!colisao.CompareTag("Caixa")) return;

        TentarReceberCaixa(colisao.GetComponentInParent<Caixa>());
    }

    public bool TentarReceberCaixa(Caixa caixa)
    {
        if (!isActiveAndEnabled || _vida == null || caixa == null || !caixa.TentarConsumir()) return false;

        bool recebeuDano = _vida.TentarReceberDano(_danoPorEntrega);
        if (recebeuDano)
            VidaManager.instance?.RegistrarEntregaBemSucedida();
        return recebeuDano;
    }

    private void OnValidate()
    {
        _danoPorEntrega = Mathf.Max(1, _danoPorEntrega);
    }
}
