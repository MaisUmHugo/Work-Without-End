using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ZumbiSombrio : Entregavel
{
    private enum EstadoZumbiSombrio
    {
        Caminhando,
        PreparandoPerseguicao,
        Perseguindo,
        Atordoado,
        AposImpacto,
        Finalizado
    }

    private static readonly int Andar = Animator.StringToHash("Andar");
    private static readonly int Correr = Animator.StringToHash("Correr");
    private static readonly int Caiu = Animator.StringToHash("Caiu");
    private static readonly int RecebeuEntrega = Animator.StringToHash("RecebeuEntrega");
    private static readonly int Transparente = Animator.StringToHash("Transparente");

    [Header("Referencias")]
    [SerializeField] private Rigidbody2D _corpo;
    [SerializeField] private Collider2D _colisor;
    [SerializeField] private Animator _animator;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private EntregavelPisca _entregavelPisca;
    [SerializeField] private PontuacaoPopup _popupPontuacao;
    [SerializeField] private Transform _pontoExclamacao;
    [SerializeField] private GameObject _prefabExclamacao;
    [SerializeField, Min(0f)] private float _duracaoExclamacao = 2f;

    [Header("Movimento")]
    [SerializeField, Min(0.1f)] private float _velocidadeCaminhada = 12.5f;
    [SerializeField, Min(0.1f)] private float _velocidadeCorrida = 18f;
    [SerializeField, Min(0.1f)] private float _velocidadeTrocaLane = 22f;
    [SerializeField, Min(0f)] private float _distanciaInicioPerseguicao = 35f;
    [SerializeField, Min(0)] private int _alcanceTrocaLane = 1;
    [SerializeField, Min(0f)] private float _tempoAvisoTrocaLane = 0.3f;
    [SerializeField] private Color _corAvisoTrocaLane = new Color(0.95f, 0.25f, 1f, 1f);

    [Header("Dano")]
    [SerializeField, Min(1)] private int _danoAoJogador = 1;
    [Tooltip("Limite de limpeza apos o contato ou entrega, caso nao saia da camera antes.")]
    [SerializeField, Min(1f)] private float _tempoAposImpacto = 12f;

    [Header("Visual apos a entrega completa")]
    [SerializeField, Min(0f)] private float _tempoParaTransparencia = 1.5f;
    [SerializeField, Range(0f, 1f)] private float _opacidadeAposEntrega = 0.5f;

    [Header("Entregas")]
    [SerializeField, Min(2)] private int _entregasNecessarias = 2;
    [Tooltip("Distancia para liberar a entrega caso nao exista camera. Com camera, libera ao entrar na tela.")]
    [SerializeField, Min(0f)] private float _distanciaLiberarEntrega = 45f;
    [Tooltip("Duracao da desaceleracao breve apos a primeira entrega.")]
    [SerializeField, Min(0f)] private float _tempoAtordoamento = 0.18f;
    [SerializeField, Range(0.1f, 1f)] private float _fatorVelocidadeAposEntrega = 0.65f;
    [SerializeField, Min(1f)] private float _escalaPorEntrega = 1.2f;
    [SerializeField, Min(0f)] private float _distanciaEmpurraoEntrega = 0.55f;
    [SerializeField, Min(0f)] private float _duracaoEmpurraoEntrega = 0.1f;
    [SerializeField] private Color _corSombria = Color.white;

    [Header("Limpeza")]
    [SerializeField, Min(0f)] private float _margemSaidaCamera = 0.15f;
    [SerializeField, Min(1f)] private float _distanciaMaximaAtrasJogador = 30f;

    private Transform _alvo;
    private Mov _movimentoJogador;
    private Camera _camera;
    private EstadoZumbiSombrio _estado;
    private EstadoZumbiSombrio _estadoAntesAtordoamento;
    private Vector3 _escalaOriginalVisual;
    private int _entregasRecebidas;
    private float _tempoAtordoamentoRestante;
    private Vector2 _origemEmpurraoEntrega;
    private Vector2 _destinoEmpurraoEntrega;
    private float _tempoEmpurraoRestante;
    private float _duracaoEmpurraoAtual;
    private GameObject _exclamacaoAtual;
    private float _tempoExclamacaoRestante;
    private int _indiceLaneAtual;
    private float _laneInicialY;
    private float _laneDestinoY;
    private float _tempoAvisoRestante;
    private float _tempoImpactoRestante;
    private float _tempoTransparenciaRestante;
    private bool _transparente;
    private bool _inicializado;
    private bool _finalizacaoNotificada;
    private float _velocidadeCaminhadaBase;
    private float _velocidadeCorridaBase;
    private float _velocidadeTrocaLaneBase;

    public event Action<ZumbiSombrio> Finalizado;

    public bool EmExecucao => _estado == EstadoZumbiSombrio.Caminhando
        || _estado == EstadoZumbiSombrio.PreparandoPerseguicao
        || _estado == EstadoZumbiSombrio.Perseguindo
        || _estado == EstadoZumbiSombrio.Atordoado;

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
        if (_entregavelPisca == null)
            _entregavelPisca = GetComponent<EntregavelPisca>();
        if (_popupPontuacao == null)
            _popupPontuacao = GetComponentInChildren<PontuacaoPopup>();
        if (_spriteRenderer != null)
            _escalaOriginalVisual = _spriteRenderer.transform.localScale;

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
        _indiceLaneAtual = ObterIndiceLaneMaisProxima(posicaoLaneY);
        _laneInicialY = LanesController.instance != null && LanesController.instance.linhas != null
            && LanesController.instance.linhas.Length > 0
            ? LanesController.instance.PosicaoY((LanesController.Linhas)_indiceLaneAtual) : posicaoLaneY;
        _laneDestinoY = _laneInicialY;
        _tempoAvisoRestante = 0f;
        _tempoImpactoRestante = 0f;
        _tempoTransparenciaRestante = 0f;
        _transparente = false;
        if (_animator != null) _animator.SetBool(Transparente, false);
        _tempoAtordoamentoRestante = 0f;
        _tempoEmpurraoRestante = 0f;
        _entregasRecebidas = 0;
        ativoParaEntrega = false;
        _entregavelPisca?.PararPiscar();
        RemoverExclamacao();
        _estado = EstadoZumbiSombrio.Caminhando;
        _inicializado = true;
        _finalizacaoNotificada = false;
        _colisor.enabled = true;

        Vector2 posicao = _corpo.position;
        posicao.y = _laneInicialY;
        _corpo.position = posicao;
        AtualizarAnimator();
        AplicarCor(_corSombria);
        RestaurarEscala();
        return true;
    }

    private void LateUpdate()
    {
        if (!_inicializado || _spriteRenderer == null || BloqueioGameplay.Bloqueado) return;

        float fator = EntregaPendente ? Mathf.Pow(_escalaPorEntrega, _entregasRecebidas) : 1f;
        _spriteRenderer.transform.localScale = Vector3.Lerp(
            _spriteRenderer.transform.localScale, _escalaOriginalVisual * fator,
            Mathf.Clamp01(TempoGameplay.DeltaTime * 20f));

        if (_transparente)
        {
            Color cor = _spriteRenderer.color;
            cor.a = _opacidadeAposEntrega;
            _spriteRenderer.color = cor;
        }
    }

    public void IniciarCorridaParaTeste()
    {
        if (!_inicializado || _estado != EstadoZumbiSombrio.Caminhando) return;

        DefinirLaneDestino();
        IniciarPerseguicao();
        // O teste pula a caminhada; o controller antigo sai do Idle apenas pelo estado Andando.
        int estadoCorrendo = Animator.StringToHash("Base Layer.Correndo");
        if (_animator != null && _animator.HasState(0, estadoCorrendo))
            _animator.Play(estadoCorrendo, 0, 0f);
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
            return;
        }

        if (_alvo == null)
        {
            Remover();
            return;
        }

        float deltaTime = TempoGameplay.FixedDeltaTime;
        AtualizarDisponibilidadeEntrega();
        AtualizarExclamacao(deltaTime);
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

            case EstadoZumbiSombrio.Atordoado:
                _corpo.linearVelocity = Vector2.zero;
                if (_tempoEmpurraoRestante > 0f)
                    AtualizarEmpurraoEntrega(deltaTime);
                else
                    MoverDuranteReacaoEntrega(deltaTime);
                _tempoAtordoamentoRestante -= deltaTime;
                if (_tempoAtordoamentoRestante <= 0f)
                    RetomarMovimento();
                break;

            case EstadoZumbiSombrio.AposImpacto:
                MoverCaminhando(deltaTime);
                if (!_transparente && _entregasRecebidas >= _entregasNecessarias)
                {
                    _tempoTransparenciaRestante -= deltaTime;
                    if (_tempoTransparenciaRestante <= 0f)
                    {
                        _entregavelPisca?.PararPiscar();
                        _transparente = true;
                        if (_animator != null) _animator.SetBool(Transparente, true);
                    }
                }
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

    private void AtualizarDisponibilidadeEntrega()
    {
        if (ativoParaEntrega || !EntregaPendente || !EmExecucao) return;
        if (_alvo == null) return;
        if (_camera == null) _camera = Camera.main;

        if (_camera != null)
        {
            Vector3 viewport = _camera.WorldToViewportPoint(_corpo.position);
            if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f
                || viewport.y < 0f || viewport.y > 1f) return;
        }
        else if (Mathf.Abs(_corpo.position.x - _alvo.position.x) > _distanciaLiberarEntrega)
            return;
        LiberarEntrega();
    }

    private void LiberarEntrega()
    {
        if (ativoParaEntrega) return;
        ativoParaEntrega = true;
        _entregavelPisca?.PiscarAtivo(true);
        ExibirExclamacao();
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
        AtualizarDisponibilidadeEntrega();
    }

    private void ExibirExclamacao()
    {
        RemoverExclamacao();
        if (_prefabExclamacao == null || _pontoExclamacao == null || _duracaoExclamacao <= 0f)
            return;

        _exclamacaoAtual = Instantiate(_prefabExclamacao, _pontoExclamacao.position, Quaternion.identity);
        _exclamacaoAtual.transform.SetParent(_pontoExclamacao, true);
        _tempoExclamacaoRestante = _duracaoExclamacao;
    }

    private void AtualizarExclamacao(float deltaTime)
    {
        if (_exclamacaoAtual == null) return;

        _tempoExclamacaoRestante -= deltaTime;
        if (_tempoExclamacaoRestante <= 0f)
            RemoverExclamacao();
    }

    private void RemoverExclamacao()
    {
        if (_exclamacaoAtual != null)
            Destroy(_exclamacaoAtual);
        _exclamacaoAtual = null;
        _tempoExclamacaoRestante = 0f;
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
        if (_estado == EstadoZumbiSombrio.Finalizado
            || _estado == EstadoZumbiSombrio.AposImpacto
            || colisao == null || BloqueioGameplay.Bloqueado || Time.timeScale <= 0f) return;

        Caixa caixa = colisao.GetComponentInParent<Caixa>();
        if (caixa != null)
        {
            TentarReceberCaixa(caixa);
            return;
        }

        if (colisao.CompareTag("Player"))
            AtingirJogador();
    }

    public bool TentarReceberCaixa(Caixa caixa)
    {
        if (caixa == null || BloqueioGameplay.Bloqueado || !EmExecucao || !EntregaPendente)
            return false;

        // O contato pode acontecer entre dois FixedUpdates, logo apos entrar na area de entrega.
        AtualizarDisponibilidadeEntrega();
        if (!PodeReceberEntrega || !caixa.TentarConsumir()) return false;
        ReceberEntrega();
        return true;
    }

    public override void ReceberEntrega()
    {
        if (BloqueioGameplay.Bloqueado || !ativoParaEntrega || !EntregaPendente
            || _estado == EstadoZumbiSombrio.AposImpacto
            || _estado == EstadoZumbiSombrio.Finalizado) return;

        _entregavelPisca?.PararPiscar();
        AplicarCor(_corSombria);
        _entregasRecebidas++;
        VidaManager.instance?.RegistrarEntregaBemSucedida();
        if (_entregasRecebidas >= _entregasNecessarias)
        {
            int pontosRecebidos = ProcessarEntrega(false);
            _popupPontuacao?.MostrarPontuacao(pontosRecebidos);
            ConcluirEncontro();
            return;
        }

        if (_estado != EstadoZumbiSombrio.Atordoado)
            _estadoAntesAtordoamento = _estado;
        _estado = EstadoZumbiSombrio.Atordoado;
        _tempoAtordoamentoRestante = _tempoAtordoamento;
        if (_corpo != null)
        {
            _corpo.linearVelocity = Vector2.zero;
            _origemEmpurraoEntrega = _corpo.position;
            // Recua no eixo do cenario sem empurrar o Sombrio para fora da lane.
            _destinoEmpurraoEntrega = _origemEmpurraoEntrega + Vector2.right * _distanciaEmpurraoEntrega;
            _duracaoEmpurraoAtual = Mathf.Min(_duracaoEmpurraoEntrega, _tempoAtordoamento);
            _tempoEmpurraoRestante = _duracaoEmpurraoAtual;
            if (_duracaoEmpurraoAtual <= 0f)
                _corpo.position = _destinoEmpurraoEntrega;
        }
        AplicarCor(_corSombria);
        AtualizarAnimator();
    }

    private void RetomarMovimento()
    {
        _estado = _estadoAntesAtordoamento;
        AplicarCor(_estado == EstadoZumbiSombrio.PreparandoPerseguicao
            ? _corAvisoTrocaLane : _corSombria);
        AtualizarAnimator();
        _entregavelPisca?.PiscarAtivo(true);
        ExibirExclamacao();
    }

    private void MoverDuranteReacaoEntrega(float deltaTime)
    {
        float progresso = 1f - Mathf.Clamp01(
            _tempoAtordoamentoRestante / Mathf.Max(0.001f, _tempoAtordoamento));
        float fatorVelocidade = Mathf.Lerp(_fatorVelocidadeAposEntrega, 1f, progresso);
        if (_estadoAntesAtordoamento == EstadoZumbiSombrio.Perseguindo)
            MoverPerseguindo(deltaTime * fatorVelocidade);
        else
            MoverCaminhando(deltaTime * fatorVelocidade);
    }

    private void AtualizarEmpurraoEntrega(float deltaTime)
    {
        if (_tempoEmpurraoRestante <= 0f) return;

        _tempoEmpurraoRestante = Mathf.Max(0f, _tempoEmpurraoRestante - deltaTime);
        float progresso = 1f - _tempoEmpurraoRestante / _duracaoEmpurraoAtual;
        float progressoSuave = 1f - (1f - progresso) * (1f - progresso);
        _corpo.MovePosition(Vector2.Lerp(_origemEmpurraoEntrega, _destinoEmpurraoEntrega, progressoSuave));
    }

    private void ConcluirEncontro()
    {
        _entregavelPisca?.PararPiscar();
        RemoverExclamacao();
        // O joinha segue da posicao atual, sem voltar a lane onde o invocado nasceu.
        _laneInicialY = _corpo.position.y;
        _laneDestinoY = _laneInicialY;
        _estado = EstadoZumbiSombrio.AposImpacto;
        _tempoImpactoRestante = Mathf.Max(0f, _tempoAposImpacto);
        _tempoTransparenciaRestante = _tempoParaTransparencia;
        _colisor.enabled = false;
        RestaurarEscala();
        AplicarCor(_corSombria);
        NotificarFinalizacao();
        AtualizarAnimator();

        if (_entregasRecebidas >= _entregasNecessarias)
            _entregavelPisca?.PiscarRecebendo();

        if (_tempoImpactoRestante <= 0f)
            Remover();
    }

    private void RestaurarEscala()
    {
        if (_spriteRenderer != null)
            _spriteRenderer.transform.localScale = _escalaOriginalVisual;
    }

    private void AtingirJogador()
    {
        if (_estado == EstadoZumbiSombrio.AposImpacto) return;

        RegistrarFalhaEntrega(false);
        VidaManager.instance?.PerderVidas(_danoAoJogador, false);
        ConcluirEncontro();
    }

    private void VerificarSaidaDaArea()
    {
        if (_alvo != null && _corpo.position.x < _alvo.position.x - _distanciaMaximaAtrasJogador)
        {
            RegistrarFalhaEntrega();
            Remover();
            return;
        }

        if (_camera == null)
            _camera = Camera.main;
        if (_camera == null) return;

        Vector3 viewport = _camera.WorldToViewportPoint(_corpo.position);
        if (viewport.x < -_margemSaidaCamera)
        {
            RegistrarFalhaEntrega();
            Remover();
        }
    }

    private void AtualizarAnimator()
    {
        if (_animator == null) return;

        EstadoZumbiSombrio estadoVisual = _estado == EstadoZumbiSombrio.Atordoado
            ? _estadoAntesAtordoamento : _estado;
        _animator.SetBool(
            Andar,
            estadoVisual == EstadoZumbiSombrio.Caminhando
                || estadoVisual == EstadoZumbiSombrio.PreparandoPerseguicao);
        _animator.SetBool(Correr, estadoVisual == EstadoZumbiSombrio.Perseguindo);
        bool recebeuEntrega = _estado == EstadoZumbiSombrio.AposImpacto
            && _entregasRecebidas >= _entregasNecessarias;
        _animator.SetBool(RecebeuEntrega, recebeuEntrega);
        _animator.SetBool(Caiu, _estado == EstadoZumbiSombrio.AposImpacto && !recebeuEntrega);
    }

    private void AplicarCor(Color cor)
    {
        cor.a = 1f;
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
        _entregavelPisca?.PararPiscar();
        RemoverExclamacao();
        AplicarCor(_corSombria);
        RestaurarEscala();
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
        _tempoAposImpacto = Mathf.Max(1f, _tempoAposImpacto);
        _tempoParaTransparencia = Mathf.Max(0f, _tempoParaTransparencia);
        _opacidadeAposEntrega = Mathf.Clamp01(_opacidadeAposEntrega);
        _entregasNecessarias = Mathf.Max(2, _entregasNecessarias);
        _distanciaLiberarEntrega = Mathf.Max(0f, _distanciaLiberarEntrega);
        _tempoAtordoamento = Mathf.Max(0f, _tempoAtordoamento);
        _fatorVelocidadeAposEntrega = Mathf.Clamp(_fatorVelocidadeAposEntrega, 0.1f, 1f);
        _duracaoExclamacao = Mathf.Max(0f, _duracaoExclamacao);
        _escalaPorEntrega = Mathf.Max(1f, _escalaPorEntrega);
        _distanciaEmpurraoEntrega = Mathf.Max(0f, _distanciaEmpurraoEntrega);
        _duracaoEmpurraoEntrega = Mathf.Max(0f, _duracaoEmpurraoEntrega);
        _margemSaidaCamera = Mathf.Max(0f, _margemSaidaCamera);
        _distanciaMaximaAtrasJogador = Mathf.Max(1f, _distanciaMaximaAtrasJogador);
    }
}
