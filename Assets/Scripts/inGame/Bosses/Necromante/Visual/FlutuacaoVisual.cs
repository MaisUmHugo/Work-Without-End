using UnityEngine;

[DisallowMultipleComponent]
public class FlutuacaoVisual : MonoBehaviour
{
    [SerializeField] private ControladorBoss _controlador;
    [Tooltip("Amplitude em unidades de mundo, independente da escala do prefab.")]
    [SerializeField, Min(0f)] private float _amplitude = 0.4f;
    [SerializeField, Min(0.01f)] private float _periodo = 3f;

    private Vector3 _posicaoOriginal;
    private float _tempo;

    private void OnEnable()
    {
        _posicaoOriginal = transform.localPosition;
        _tempo = 0f;
    }

    private void Update()
    {
        if (_controlador == null || !_controlador.PodeAtualizar) return;

        _tempo = Mathf.Repeat(_tempo + Time.deltaTime, Mathf.Max(0.01f, _periodo));
        float deslocamento = Mathf.Sin(_tempo * 2f * Mathf.PI / Mathf.Max(0.01f, _periodo)) * Mathf.Max(0f, _amplitude);
        Vector3 offsetMundo = Vector3.up * deslocamento;
        Vector3 offsetLocal = transform.parent != null
            ? transform.parent.InverseTransformVector(offsetMundo) : offsetMundo;
        transform.localPosition = _posicaoOriginal + offsetLocal;
    }

    private void OnDisable()
    {
        transform.localPosition = _posicaoOriginal;
        _tempo = 0f;
    }
}
