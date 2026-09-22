using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ZumbiSombrio : MonoBehaviour
{
    private enum EstadoZumbiSombrio
    {
        Caminhando,
        PreparandoPerseguicao,
        Perseguindo,
        AposImpacto,
        Finalizado
    }

    private static readonly int Andar = Animator.StringToHash("Andar");
    private static readonly int Correr = Animator.StringToHash("Correr");
    private static readonly int Caiu = Animator.StringToHash("Caiu");

    [Header("Referencias")]
    [SerializeField] private Rigidbody2D _corpo;
    [SerializeField] private Collider2D _colisor;
    [SerializeField] private Animator _animator;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Header("Movimento")]
    [SerializeField, Min(0.1f)] private float _velocidadeCaminhada = 6f;
    [SerializeField, Min(0.1f)] private float _velocidadeCorrida = 8.5f;
    [SerializeField, Min(0.1f)] private float _velocidadeTrocaLane = 12f;
    [SerializeField, Min(0f)] private float _distanciaInicioPerseguicao = 35f;
    [SerializeField, Min(0)] private int _alcanceTrocaLane = 2;
    [SerializeField, Min(0f)] private float _tempoAvisoTrocaLane = 0.3f;
    [SerializeField] private Color _corAvisoTrocaLane = new Color(0.95f, 0.25f, 1f, 1f);

    [Header("Dano")]
    [SerializeField, Min(1)] private int _danoAoJogador = 1;
    [SerializeField, Min(0f)] private float _tempoAposImpacto = 1.25f;

    [Header("Imunidade a entrega")]
    [SerializeField] private Color _corSombria = new Color(0.22f, 0.08f, 0.3f, 1f);
    [SerializeField] private Color _corBloqueio = new Color(0.75f, 0.3f, 1f, 1f);
    [SerializeField, Min(0.01f)] private float _duracaoFeedbackBloqueio = 0.15f;
    [SerializeField, Min(1f)] private float _forcaRebatidaCaixa = 1.35f;
    [SerializeField, Min(0f)] private float _velocidadeMinimaRebatida = 12f;
    [SerializeField, Min(0f)] private float _velocidadeAngularRebatida = 720f;

    [Header("Limpeza")]
    [SerializeField, Min(0f)] private float _margemSaidaCamera = 0.15f;
    [SerializeField, Min(1f)] private float _distanciaMaximaAtrasJogador = 30f;

    private Transform _alvo;
    private Mov _movimentoJogador;
    private Camera _camera;
    private Coroutine _feedbackBloqueio;
    private EstadoZumbiSombrio _estado;
    private int _indiceLaneAtual;
    private float _laneInicialY;
    private float _laneDestinoY;
    private float _tempoAvisoRestante;
    private float _tempoImpactoRestante;
    private bool _inicializado;
    private bool _finalizacaoNotificada;
    private float _velocidadeCaminhadaBase;
    private float _velocidadeCorridaBase;
    private float _velocidadeTrocaLaneBase;

    public event Action<ZumbiSombrio> Finalizado;

    public bool EmExecucao => _estado == EstadoZumbiSombrio.Caminhando
        || _estado == EstadoZumbiSombrio.PreparandoPerseguicao
        || _estado == EstadoZumbiSombrio.Perseguindo;

    private void Awake()
    {
        if (_corpo == null)
            _corpo = GetComponent<Rigidbody2D>();
        if (_colisor == null)
            _colisor = GetComponent<Collider2D>();
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        _velocidadeCaminhadaBase = _velocidadeCaminhada;
        _velocidadeCorridaBase = _velocidadeCorrida;
        _velocidadeTrocaLaneBase = _velocidadeTrocaLane;

        _camera = Camera.main;
        AplicarCor(_corSombria);
    }

    private void Start()
    {
        if (_inicializado) return;

        GameObject jogador = GameObject.FindGameObjectWithTag("Player");
        if (jogador == null)
        {
            Debug.LogWarning("Zumbi Sombrio: jogador nao encontrado.", this);
            Remover();
            return;
        }

        Inicializar(jogador.transform, transform.position.y);
    }

    public bool Inicializar(Transform alvo, float posicaoLaneY)
    {
        if (alvo == null || _corpo == null || _colisor == null)
        {
            Debug.LogError("Zumbi Sombrio: configure corpo, colisor e alvo.", this);
            return false;
        }

        _alvo = alvo;
        _movimentoJogador = alvo.GetComponent<Mov>();
        _laneInicialY = posicaoLaneY;
        _indiceLaneAtual = ObterIndiceLaneMaisProxima(posicaoLaneY);
        _laneDestinoY = posicaoLaneY;
        _tempoAvisoRestante = 0f;
        _tempoImpactoRestante = 0f;
        _estado = EstadoZumbiSombrio.Caminhando;
        _inicializado = true;
        _finalizacaoNotificada = false;
        _colisor.enabled = true;

        Vector2 posicao = _corpo.position;
        posicao.y = _laneInicialY;
        _corpo.position = posicao;
        AtualizarAnimator();
        AplicarCor(_corSombria);
        return true;
    }

    public void ConfigurarMultiplicadorVelocidade(float multiplicador)
    {
        multiplicador = Mathf.Max(0.1f, multiplicador);
        _velocidadeCaminhada = _velocidadeCaminhadaBase * multiplicador;
        _velocidadeCorrida = _velocidadeCorridaBase * multiplicador;
        _velocidadeTrocaLane = _velocidadeTrocaLaneBase * multiplicador;
    }

    private void FixedUpdate()
    {
        if (!_inicializado || _estado == EstadoZumbiSombrio.Finalizado) return;

        if (BloqueioGameplay.Bloqueado || Time.timeScale <= 0f)
        {
            if (_corpo != null)
                _corpo.linearVelocity = Vector2.zero;
            return;
        }

        if (_alvo == null)
        {
            Remover();
            return;
        }

        float deltaTime = Time.fixedDeltaTime;
        switch (_estado)
        {
            case EstadoZumbiSombrio.Caminhando:
                MoverCaminhando(deltaTime);
                if (Mathf.Abs(_corpo.position.x - _alvo.position.x) <= _distanciaInicioPerseguicao)
                    PrepararPerseguicao();
                break;

            case EstadoZumbiSombrio.PreparandoPerseguicao:
                MoverCaminhando(deltaTime);
                _tempoAvisoRestante -= deltaTime;
                if (_tempoAvisoRestante <= 0f)
                    IniciarPerseguicao();
                break;

            case EstadoZumbiSombrio.Perseguindo:
                MoverPerseguindo(deltaTime);
                break;

            case EstadoZumbiSombrio.AposImpacto:
                MoverCaminhando(deltaTime);
                _tempoImpactoRestante -= deltaTime;
                if (_tempoImpactoRestante <= 0f)
                {
                    Remover();
                    return;
                }
                break;
        }

        VerificarSaidaDaArea();
    }

    private void MoverCaminhando(float deltaTime)
    {
        Vector2 posicao = _corpo.position;
        posicao.x -= _velocidadeCaminhada * deltaTime;
        posicao.y = _laneInicialY;
        _corpo.MovePosition(posicao);
    }

    private void MoverPerseguindo(float deltaTime)
    {
        Vector2 posicao = _corpo.position;
        posicao.x -= _velocidadeCorrida * deltaTime;
        posicao.y = Mathf.MoveTowards(posicao.y, _laneDestinoY, _velocidadeTrocaLane * deltaTime);
        _corpo.MovePosition(posicao);
    }

    private void PrepararPerseguicao()
    {
        bool mudaraLane = DefinirLaneDestino();
        if (!mudaraLane || _tempoAvisoTrocaLane <= 0f)
        {
            IniciarPerseguicao();
            return;
        }

        _estado = EstadoZumbiSombrio.PreparandoPerseguicao;
        _tempoAvisoRestante = _tempoAvisoTrocaLane;
        AplicarCor(_corAvisoTrocaLane);
        AtualizarAnimator();
    }

    private bool DefinirLaneDestino()
    {
        if (_movimentoJogador == null || LanesController.instance == null
            || LanesController.instance.linhas == null
            || LanesController.instance.linhas.Length == 0)
        {
            _laneDestinoY = _laneInicialY;
            return false;
        }

        int quantidadeLanes = LanesController.instance.linhas.Length;
        int laneJogador = Mathf.Clamp((int)_movimentoJogador.linhaAtual, 0, quantidadeLanes - 1);
        int distanciaLanes = Mathf.Abs(laneJogador - _indiceLaneAtual);

        if (distanciaLanes > Mathf.Max(0, _alcanceTrocaLane))
        {
            _laneDestinoY = _laneInicialY;
            return false;
        }

        bool mudaraLane = laneJogador != _indiceLaneAtual;
        _indiceLaneAtual = laneJogador;
        _laneDestinoY = LanesController.instance.PosicaoY(
            (LanesController.Linhas)_indiceLaneAtual);
        return mudaraLane;
    }

    private int ObterIndiceLaneMaisProxima(float posicaoY)
    {
        if (LanesController.instance == null || LanesController.instance.linhas == null
            || LanesController.instance.linhas.Length == 0)
            return 0;

        int indiceMaisProximo = 0;
        float menorDistancia = float.MaxValue;
        for (int i = 0; i < LanesController.instance.linhas.Length; i++)
        {
            float distancia = Mathf.Abs(
                LanesController.instance.PosicaoY((LanesController.Linhas)i) - posicaoY);
            if (distancia >= menorDistancia) continue;

            menorDistancia = distancia;
            indiceMaisProximo = i;
        }

        return indiceMaisProximo;
    }

    private void IniciarPerseguicao()
    {
        _estado = EstadoZumbiSombrio.Perseguindo;
        _tempoAvisoRestante = 0f;
        AplicarCor(_corSombria);
        AtualizarAnimator();
    }

    private void OnTriggerEnter2D(Collider2D colisao)
    {
        ProcessarColisao(colisao);
    }

    private void OnTriggerStay2D(Collider2D colisao)
    {
        ProcessarColisao(colisao);
    }

    private void ProcessarColisao(Collider2D colisao)
    {
        if (_estado == EstadoZumbiSombrio.Finalizado || colisao == null) return;

        if (colisao.CompareTag("Player"))
        {
            AtingirJogador();
            return;
        }

        Caixa caixa = colisao.GetComponentInParent<Caixa>();
        if (caixa != null)
            BloquearEntrega(colisao);
    }

    private void AtingirJogador()
    {
        if (_estado == EstadoZumbiSombrio.AposImpacto) return;

        VidaManager.instance?.PerderVidas(_danoAoJogador);
        _estado = EstadoZumbiSombrio.AposImpacto;
        _tempoImpactoRestante = Mathf.Max(0f, _tempoAposImpacto);
        _colisor.enabled = false;
        NotificarFinalizacao();
        AtualizarAnimator();

        if (_tempoImpactoRestante <= 0f)
            Remover();
    }

    private void BloquearEntrega(Collider2D colisaoCaixa)
    {
        if (_estado == EstadoZumbiSombrio.AposImpacto) return;

        Physics2D.IgnoreCollision(_colisor, colisaoCaixa);
        Rigidbody2D corpoCaixa = colisaoCaixa.attachedRigidbody;
        if (corpoCaixa != null)
        {
            Vector2 velocidade = corpoCaixa.linearVelocity;
            Vector2 direcaoRebatida = velocidade.sqrMagnitude > 0f
                ? -velocidade.normalized
                : ((Vector2)corpoCaixa.position - _corpo.position).normalized;
            direcaoRebatida = (direcaoRebatida
                + Vector2.up * UnityEngine.Random.Range(-0.45f, 0.45f)).normalized;
            float velocidadeRebatida = Mathf.Max(
                _velocidadeMinimaRebatida,
                velocidade.magnitude * _forcaRebatidaCaixa);
            corpoCaixa.linearVelocity = direcaoRebatida * velocidadeRebatida;
            float sentidoGiro = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            corpoCaixa.angularVelocity = sentidoGiro * _velocidadeAngularRebatida;
        }

        if (_feedbackBloqueio != null)
            StopCoroutine(_feedbackBloqueio);
        _feedbackBloqueio = StartCoroutine(ExibirBloqueio());
    }

    private IEnumerator ExibirBloqueio()
    {
        AplicarCor(_corBloqueio);
        yield return new WaitForSeconds(_duracaoFeedbackBloqueio);
        _feedbackBloqueio = null;
        AplicarCor(_corSombria);
    }

    private void VerificarSaidaDaArea()
    {
        if (_alvo != null && _corpo.position.x < _alvo.position.x - _distanciaMaximaAtrasJogador)
        {
            Remover();
            return;
        }

        if (_camera == null)
            _camera = Camera.main;
        if (_camera == null) return;

        Vector3 viewport = _camera.WorldToViewportPoint(_corpo.position);
        if (viewport.x < -_margemSaidaCamera)
            Remover();
    }

    private void AtualizarAnimator()
    {
        if (_animator == null) return;

        _animator.SetBool(
            Andar,
            _estado == EstadoZumbiSombrio.Caminhando
                || _estado == EstadoZumbiSombrio.PreparandoPerseguicao);
        _animator.SetBool(Correr, _estado == EstadoZumbiSombrio.Perseguindo);
        _animator.SetBool(Caiu, _estado == EstadoZumbiSombrio.AposImpacto);
    }

    private void AplicarCor(Color cor)
    {
        if (_spriteRenderer != null)
            _spriteRenderer.color = cor;
    }

    public void Remover()
    {
        if (_estado == EstadoZumbiSombrio.Finalizado) return;

        _estado = EstadoZumbiSombrio.Finalizado;
        if (_corpo != null)
            _corpo.linearVelocity = Vector2.zero;
        NotificarFinalizacao();
        Destroy(gameObject);
    }

    private void NotificarFinalizacao()
    {
        if (_finalizacaoNotificada) return;

        _finalizacaoNotificada = true;
        Finalizado?.Invoke(this);
    }

    private void OnDisable()
    {
        if (_feedbackBloqueio != null)
            StopCoroutine(_feedbackBloqueio);
        _feedbackBloqueio = null;
        AplicarCor(_corSombria);
    }

    private void OnDestroy()
    {
        NotificarFinalizacao();
    }

    private void OnValidate()
    {
        _velocidadeCaminhada = Mathf.Max(0.1f, _velocidadeCaminhada);
        _velocidadeCorrida = Mathf.Max(0.1f, _velocidadeCorrida);
        _velocidadeTrocaLane = Mathf.Max(0.1f, _velocidadeTrocaLane);
        _distanciaInicioPerseguicao = Mathf.Max(0f, _distanciaInicioPerseguicao);
        _alcanceTrocaLane = Mathf.Max(0, _alcanceTrocaLane);
        _tempoAvisoTrocaLane = Mathf.Max(0f, _tempoAvisoTrocaLane);
        _danoAoJogador = Mathf.Max(1, _danoAoJogador);
        _tempoAposImpacto = Mathf.Max(0f, _tempoAposImpacto);
        _duracaoFeedbackBloqueio = Mathf.Max(0.01f, _duracaoFeedbackBloqueio);
        _forcaRebatidaCaixa = Mathf.Max(1f, _forcaRebatidaCaixa);
        _velocidadeMinimaRebatida = Mathf.Max(0f, _velocidadeMinimaRebatida);
        _velocidadeAngularRebatida = Mathf.Max(0f, _velocidadeAngularRebatida);
        _margemSaidaCamera = Mathf.Max(0f, _margemSaidaCamera);
        _distanciaMaximaAtrasJogador = Mathf.Max(1f, _distanciaMaximaAtrasJogador);
    }
}
