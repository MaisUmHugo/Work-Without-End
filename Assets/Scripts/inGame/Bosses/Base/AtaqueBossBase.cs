using UnityEngine;

public abstract class AtaqueBossBase : MonoBehaviour
{
    [Header("Disponibilidade")]
    [SerializeField, Min(0f)] private float _cooldown;

    private float _cooldownRestante;

    public abstract bool EmExecucao { get; }
    public abstract bool EmPreparacao { get; }
    public bool Disponivel => !EmExecucao && _cooldownRestante <= 0f;
    public float CooldownRestante => _cooldownRestante;

    public abstract bool Inicializar();
    public abstract bool TentarIniciar(Transform alvo);
    public abstract void Atualizar(float deltaTime);
    public abstract void Cancelar();

    public void AtualizarCooldown(float deltaTime)
    {
        if (deltaTime <= 0f || _cooldownRestante <= 0f) return;
        _cooldownRestante = Mathf.Max(0f, _cooldownRestante - deltaTime);
    }

    public void RestaurarDisponibilidade()
    {
        _cooldownRestante = 0f;
    }

    protected void IniciarCooldown()
    {
        _cooldownRestante = Mathf.Max(0f, _cooldown);
    }
}
