using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PausaGameplay : MonoBehaviour
{
    private static PausaGameplay _instancia;
    private static readonly HashSet<GameObject> _pendentes = new HashSet<GameObject>();
    private readonly HashSet<GameObject> _objetos = new HashSet<GameObject>();
    private readonly Dictionary<Rigidbody2D, bool> _corpos = new Dictionary<Rigidbody2D, bool>();
    private readonly Dictionary<Animator, float> _animadores = new Dictionary<Animator, float>();
    private readonly Dictionary<ParticleSystem, bool> _particulas = new Dictionary<ParticleSystem, bool>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarRegistro()
    {
        _instancia = null;
        _pendentes.Clear();
    }

    private void Awake()
    {
        if (_instancia != null && _instancia != this)
        {
            Debug.LogError("PausaGameplay: mantenha apenas um componente no manager da partida.", this);
            enabled = false;
            return;
        }
        _instancia = this;
        foreach (GameObject objeto in _pendentes)
        {
            if (objeto != null)
                _objetos.Add(objeto);
        }
        _pendentes.Clear();
    }

    private void OnEnable()
    {
        BloqueioGameplay.BloqueioAlterado += AplicarBloqueio;
        AplicarBloqueio(BloqueioGameplay.Bloqueado);
    }

    public static void Registrar(GameObject objeto)
    {
        if (objeto == null) return;
        if (_instancia == null)
        {
            _pendentes.RemoveWhere(pendente => pendente == null);
            _pendentes.Add(objeto);
            return;
        }
        _instancia._objetos.RemoveWhere(registrado => registrado == null);
        _instancia._objetos.Add(objeto);
        if (BloqueioGameplay.Bloqueado)
            _instancia.CongelarObjeto(objeto);
    }

    private void AplicarBloqueio(bool bloqueado)
    {
        if (!bloqueado)
        {
            RestaurarObjetos();
            return;
        }
        if (!BloqueioGameplay.VisuaisBloqueados)
            RestaurarVisuais();
        _objetos.RemoveWhere(objeto => objeto == null);
        foreach (GameObject objeto in _objetos)
            CongelarObjeto(objeto);
    }

    private void CongelarObjeto(GameObject objeto)
    {
        foreach (Rigidbody2D corpo in objeto.GetComponentsInChildren<Rigidbody2D>(true))
        {
            if (_corpos.ContainsKey(corpo)) continue;
            _corpos.Add(corpo, corpo.simulated);
            corpo.simulated = false;
        }
        if (!BloqueioGameplay.VisuaisBloqueados) return;
        foreach (Animator animador in objeto.GetComponentsInChildren<Animator>(true))
        {
            if (_animadores.ContainsKey(animador)) continue;
            _animadores.Add(animador, animador.speed);
            animador.speed = 0f;
        }
        foreach (ParticleSystem particula in objeto.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (_particulas.ContainsKey(particula)) continue;
            _particulas.Add(particula, particula.isPlaying);
            if (particula.isPlaying)
                particula.Pause(false);
        }
    }

    private void RestaurarObjetos()
    {
        foreach (var estado in _corpos)
        {
            if (estado.Key != null)
                estado.Key.simulated = estado.Value;
        }
        _corpos.Clear();
        RestaurarVisuais();
    }

    private void RestaurarVisuais()
    {
        foreach (var estado in _animadores)
        {
            if (estado.Key != null)
                estado.Key.speed = estado.Value;
        }
        foreach (var estado in _particulas)
        {
            if (estado.Key != null && estado.Value)
                estado.Key.Play(false);
        }
        _animadores.Clear();
        _particulas.Clear();
    }

    private void OnDisable()
    {
        BloqueioGameplay.BloqueioAlterado -= AplicarBloqueio;
        RestaurarObjetos();
    }

    private void OnDestroy()
    {
        if (_instancia == this)
            _instancia = null;
    }
}
