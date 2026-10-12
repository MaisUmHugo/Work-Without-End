using System;
using UnityEngine;
using UnityEngine.UI;

public class AnimacaoMenu : MonoBehaviour
{
    [Serializable]
    public class Quadro
    {
        public Sprite Sprite;
        [Min(0f)] public float Tempo;
    }

    [Serializable]
    public class Clipe
    {
        public Quadro[] Quadros;
        [Min(0.01f)] public float Duracao = 1f;

        public Sprite ObterSprite(float tempo, bool repetir)
        {
            if (Quadros == null || Quadros.Length == 0) return null;
            float instante = repetir ? Mathf.Repeat(tempo, Mathf.Max(0.01f, Duracao)) : tempo;
            Sprite sprite = Quadros[0].Sprite;
            for (int i = 1; i < Quadros.Length && instante >= Quadros[i].Tempo; i++)
                sprite = Quadros[i].Sprite;
            return sprite;
        }

        public void Mostrar(Image imagem, float tempo, float escala, bool repetir)
        {
            if (imagem == null) return;
            Sprite sprite = ObterSprite(tempo, repetir);
            if (sprite == null) return;
            imagem.sprite = sprite;
            imagem.rectTransform.sizeDelta = sprite.rect.size * escala;
            imagem.rectTransform.pivot = sprite.pivot / sprite.rect.size;
        }
    }

    [Serializable]
    public class Destinatario
    {
        public Image Imagem;
        public Clipe Movimento;
        public Clipe Recebimento;
        [Min(0.1f)] public float Escala = 5f;
        public float AlturaLane;
        [Min(0f)] public float AmplitudeZigueZague;
        [Min(0.1f)] public float IntervaloZigueZague = 1.8f;
    }

    [Header("Personagens e arte")]
    [SerializeField] private Image _protagonista;
    [SerializeField] private Image _caixa;
    [SerializeField] private Clipe _andando;
    [SerializeField] private Clipe _arremessando;
    [SerializeField] private Destinatario[] _destinatarios;
    [SerializeField, Min(0.1f)] private float _escalaProtagonista = 6f;

    [Header("Cenario em movimento")]
    [SerializeField] private RectTransform _cenario;
    [SerializeField] private RectTransform _repeticaoCenario;
    [SerializeField, Min(0f)] private float _velocidadeCenario = 145f;

    [Header("Louco atravessando a rua")]
    [SerializeField] private Image _louco;
    [SerializeField] private Clipe _corridaLouco;
    [SerializeField] private Clipe _recebimentoLouco;
    [SerializeField, Min(0.1f)] private float _escalaLouco = 4f;
    [SerializeField] private Vector2 _inicioTravessia = new Vector2(1100f, -390f);
    [SerializeField] private Vector2 _fimTravessia = new Vector2(50f, -85f);
    [SerializeField, Min(0.1f)] private float _tempoTravessia = 4.2f;
    [SerializeField, Min(0.1f)] private float _tempoSaidaLouco = 6f;
    [SerializeField] private float _xSaidaLouco = -1350f;
    [SerializeField, Min(0f)] private float _intervaloLouco = 5f;

    [Header("Motorista assustado")]
    [SerializeField] private Image _motorista;
    [SerializeField] private Clipe _dirigindoMotorista;
    [SerializeField] private Clipe _recebimentoMotorista;
    [SerializeField, Min(0.1f)] private float _escalaMotorista = 6f;
    [SerializeField, Min(0.1f)] private float _tempoAceleracaoMotorista = 2.6f;
    [SerializeField] private float _xEntregaMotorista = -400f;
    [SerializeField, Min(0.1f)] private float _tempoSaidaMotorista = 1.3f;
    [SerializeField, Min(0f)] private float _intervaloMotorista = 1f;

    [Header("Trocas de lane do protagonista")]
    [SerializeField, Min(0f)] private float _tempoReacaoZumbi = 1.1f;
    [SerializeField, Min(0.01f)] private float _duracaoTrocaLane = 0.45f;
    [SerializeField, Min(0f)] private float _antecedenciaDesvioMotorista = 0.35f;

    [Header("Posicoes no menu")]
    [SerializeField] private float _xProtagonista = -730f;
    [SerializeField] private float _alturaProtagonista = -325f;
    [SerializeField] private float _xEntrada = 1100f;
    [SerializeField] private float _xEntrega = -250f;
    [SerializeField] private float _xSaida = -1150f;
    [SerializeField] private Vector2 _origemCaixa = new Vector2(55f, 75f);

    [Header("Ritmo da sequencia")]
    [SerializeField, Min(0.1f), Tooltip("Acelera o cenario, os personagens e as entregas juntos, preservando a sincronizacao.")]
    private float _velocidadeAnimacao = 1.5f;
    [SerializeField, Min(0.1f)] private float _tempoAproximacao = 6f;
    [SerializeField, Min(0.1f)] private float _tempoArremesso = 0.65f;
    [SerializeField, Min(0.01f)] private float _atrasoCaixa = 0.12f;
    [SerializeField, Min(0.1f)] private float _tempoReacao = 1.2f;
    [SerializeField, Min(0.1f)] private float _tempoSaida = 2.8f;
    [SerializeField, Min(0f)] private float _intervalo = 0.7f;

    private int _indice;
    private float _tempoCiclo;
    private float _tempoAnimacao;
    private Vector2 _posicaoInicialCenario;
    private bool _cenarioInicializado;

    private void OnEnable()
    {
        if (_protagonista == null || _caixa == null || _destinatarios == null || _destinatarios.Length == 0)
        {
            Debug.LogWarning("[Menu] Configure os personagens da sequencia animada.", this);
            enabled = false;
            return;
        }
        _indice = 0;
        _tempoCiclo = 0f;
        _tempoAnimacao = 0f;
        _caixa.enabled = false;
        if (_louco != null) _louco.enabled = false;
        if (_motorista != null) _motorista.enabled = false;
        if (_cenario != null && _repeticaoCenario != null)
        {
            if (!_cenarioInicializado)
            {
                _posicaoInicialCenario = _cenario.anchoredPosition;
                _cenarioInicializado = true;
            }
            _repeticaoCenario.gameObject.SetActive(true);
        }
        foreach (Destinatario destinatario in _destinatarios)
            if (destinatario.Imagem != null) destinatario.Imagem.enabled = false;
        AtualizarSequencia(0f);
    }

    private void OnDisable()
    {
        if (_caixa != null) _caixa.enabled = false;
        if (_louco != null) _louco.enabled = false;
        if (_motorista != null) _motorista.enabled = false;
        if (_cenarioInicializado && _cenario != null)
            _cenario.anchoredPosition = _posicaoInicialCenario;
        if (_repeticaoCenario != null) _repeticaoCenario.gameObject.SetActive(false);
    }

    private void Update()
    {
        AtualizarSequencia(Time.unscaledDeltaTime * _velocidadeAnimacao);
    }

    private void AtualizarSequencia(float deltaTime)
    {
        _tempoCiclo += deltaTime;
        _tempoAnimacao += deltaTime;
        AtualizarCenario();
        float inicioReacao = _tempoAproximacao + _tempoArremesso;
        float inicioSaida = inicioReacao + _tempoReacao;
        float fimSaida = inicioSaida + _tempoSaida;
        float duracao = DuracaoCicloAtual(fimSaida);
        while (_tempoCiclo >= duracao)
        {
            if (_indice < _destinatarios.Length)
            {
                _destinatarios[_indice].Imagem.enabled = false;
            }
            else if (_indice == IndiceLouco)
                _louco.enabled = false;
            else if (_motorista != null)
                _motorista.enabled = false;
            int quantidade = _destinatarios.Length + (_louco != null ? 1 : 0) + (_motorista != null ? 1 : 0);
            _indice = (_indice + 1) % quantidade;
            _tempoCiclo -= duracao;
            duracao = DuracaoCicloAtual(fimSaida);
        }
        if (_indice == IndiceLouco)
        {
            AtualizarLouco();
            return;
        }
        if (_indice == IndiceMotorista)
        {
            AtualizarMotorista();
            return;
        }
        Destinatario atual = _destinatarios[_indice];
        AtualizarProtagonista(_tempoAproximacao);

        float velocidadeAproximacao = (_xEntrada - _xEntrega) / _tempoAproximacao;
        float xInicioSaida = _xEntrega - velocidadeAproximacao * (_tempoArremesso + _tempoReacao);
        float x = _tempoCiclo < inicioSaida
            ? _xEntrada - velocidadeAproximacao * _tempoCiclo
            : Mathf.Lerp(xInicioSaida, _xSaida, Mathf.Clamp01((_tempoCiclo - inicioSaida) / _tempoSaida));
        atual.Imagem.enabled = _tempoCiclo < fimSaida;
        atual.Imagem.rectTransform.anchoredPosition = new Vector2(x,
            AlturaDestinatario(atual, Mathf.Min(_tempoCiclo, inicioReacao)));
        if (_tempoCiclo < inicioReacao)
            atual.Movimento.Mostrar(atual.Imagem, _tempoCiclo, atual.Escala, true);
        else
            atual.Recebimento.Mostrar(atual.Imagem, _tempoCiclo - inicioReacao, atual.Escala, false);

        // Antecipar o centro no momento do impacto evita corrigir a direcao durante o voo.
        Vector2 centro = CentroSprite(atual.Movimento.ObterSprite(inicioReacao, true),
            atual.Escala, atual.Imagem.rectTransform.localScale);
        Vector2 destino = new Vector2(_xEntrega - velocidadeAproximacao * _tempoArremesso,
            AlturaDestinatario(atual, inicioReacao)) + centro;
        AtualizarCaixa(_tempoAproximacao, destino);
    }

    private int IndiceLouco => _louco != null ? _destinatarios.Length : -1;
    private int IndiceMotorista => _motorista != null ? _destinatarios.Length + (_louco != null ? 1 : 0) : -1;

    private void AtualizarProtagonista(float inicioArremesso)
    {
        float altura = _alturaProtagonista;
        float alturaSuperior = _destinatarios[0].AlturaLane;
        if (_indice == 0)
        {
            float progresso = Mathf.Clamp01((_tempoCiclo - _tempoReacaoZumbi) / _duracaoTrocaLane);
            altura = Mathf.Lerp(alturaSuperior, _alturaProtagonista, Mathf.SmoothStep(0f, 1f, progresso));
        }
        else if (_indice == IndiceMotorista)
        {
            float inicioDesvio = inicioArremesso - _duracaoTrocaLane - _antecedenciaDesvioMotorista;
            float progresso = Mathf.Clamp01((_tempoCiclo - inicioDesvio) / _duracaoTrocaLane);
            altura = Mathf.Lerp(_alturaProtagonista, alturaSuperior, Mathf.SmoothStep(0f, 1f, progresso));
        }
        _protagonista.rectTransform.anchoredPosition = new Vector2(_xProtagonista, altura);
        bool arremessando = _tempoCiclo >= inicioArremesso && _tempoCiclo < inicioArremesso + _tempoArremesso;
        if (arremessando)
            _arremessando.Mostrar(_protagonista, _tempoCiclo - inicioArremesso, _escalaProtagonista, false);
        else
            _andando.Mostrar(_protagonista, _tempoAnimacao, _escalaProtagonista, true);
    }

    private void AtualizarMotorista()
    {
        float inicioArremesso = Mathf.Max(0f, _tempoAceleracaoMotorista - _tempoArremesso);
        AtualizarProtagonista(inicioArremesso);
        float progresso = Mathf.Clamp01(_tempoCiclo / _tempoAceleracaoMotorista);
        float x = _tempoCiclo < _tempoAceleracaoMotorista
            ? Mathf.Lerp(_xEntrada, _xEntregaMotorista, progresso * progresso)
            : Mathf.Lerp(_xEntregaMotorista, _xSaida,
                Mathf.Clamp01((_tempoCiclo - _tempoAceleracaoMotorista) / _tempoSaidaMotorista));
        _motorista.enabled = _tempoCiclo < _tempoAceleracaoMotorista + _tempoSaidaMotorista;
        _motorista.rectTransform.anchoredPosition = new Vector2(x, _alturaProtagonista);
        if (_tempoCiclo < _tempoAceleracaoMotorista)
            _dirigindoMotorista.Mostrar(_motorista, _tempoCiclo, _escalaMotorista, true);
        else
            _recebimentoMotorista.Mostrar(_motorista, _tempoCiclo - _tempoAceleracaoMotorista, _escalaMotorista, false);
        Vector2 centro = CentroSprite(_dirigindoMotorista.ObterSprite(_tempoAceleracaoMotorista, true),
            _escalaMotorista, _motorista.rectTransform.localScale);
        AtualizarCaixa(inicioArremesso, new Vector2(_xEntregaMotorista, _alturaProtagonista) + centro);
    }

    private float AlturaDestinatario(Destinatario destinatario, float tempo)
    {
        float progresso = Mathf.PingPong(tempo / destinatario.IntervaloZigueZague, 1f);
        float desvio = Mathf.SmoothStep(-destinatario.AmplitudeZigueZague,
            destinatario.AmplitudeZigueZague, progresso);
        return destinatario.AlturaLane + desvio;
    }

    private Vector2 CentroSprite(Sprite sprite, float escala, Vector2 escalaTransform)
    {
        if (sprite == null) return Vector2.zero;
        return Vector2.Scale((sprite.rect.size * 0.5f - sprite.pivot) * escala, escalaTransform);
    }

    private void AtualizarCaixa(float inicioArremesso, Vector2 destino)
    {
        float tempoCaixa = _tempoCiclo - inicioArremesso - _atrasoCaixa;
        _caixa.enabled = tempoCaixa >= 0f && _tempoCiclo < inicioArremesso + _tempoArremesso;
        if (!_caixa.enabled) return;
        Vector2 origem = _protagonista.rectTransform.anchoredPosition + _origemCaixa;
        float progresso = Mathf.Clamp01(tempoCaixa / Mathf.Max(0.01f, _tempoArremesso - _atrasoCaixa));
        _caixa.rectTransform.anchoredPosition = Vector2.Lerp(origem, destino, progresso);
        _caixa.rectTransform.localRotation = Quaternion.identity;
    }

    private float DuracaoCicloAtual(float fimSaida)
    {
        if (_indice < _destinatarios.Length) return fimSaida + _intervalo;
        if (_indice == IndiceLouco) return _tempoTravessia + _tempoSaidaLouco + _intervaloLouco;
        return _tempoAceleracaoMotorista + _tempoSaidaMotorista + _intervaloMotorista;
    }

    private void AtualizarCenario()
    {
        if (_cenario == null || _repeticaoCenario == null) return;
        float largura = _cenario.rect.width;
        if (largura <= 0f) return;
        float deslocamento = Mathf.Repeat(_tempoAnimacao * _velocidadeCenario, largura);
        _cenario.anchoredPosition = _posicaoInicialCenario + Vector2.left * deslocamento;
        _repeticaoCenario.anchoredPosition = _cenario.anchoredPosition + Vector2.right * largura;
    }

    private void AtualizarLouco()
    {
        if (_louco == null) return;
        float inicioArremesso = Mathf.Max(0f, _tempoTravessia - _tempoArremesso);
        AtualizarProtagonista(inicioArremesso);

        _louco.enabled = _tempoCiclo < _tempoTravessia + _tempoSaidaLouco;
        if (_louco.enabled)
        {
            if (_tempoCiclo < _tempoTravessia)
            {
                _louco.rectTransform.anchoredPosition = Vector2.Lerp(_inicioTravessia, _fimTravessia,
                    _tempoCiclo / _tempoTravessia);
                _corridaLouco.Mostrar(_louco, _tempoCiclo, _escalaLouco, true);
            }
            else
            {
                float progresso = (_tempoCiclo - _tempoTravessia) / _tempoSaidaLouco;
                _louco.rectTransform.anchoredPosition = Vector2.Lerp(_fimTravessia,
                    new Vector2(_xSaidaLouco, _fimTravessia.y), progresso);
                _recebimentoLouco.Mostrar(_louco, _tempoCiclo - _tempoTravessia, _escalaLouco, false);
            }
        }
        Vector2 centro = CentroSprite(_corridaLouco.ObterSprite(_tempoTravessia, true),
            _escalaLouco, _louco.rectTransform.localScale);
        AtualizarCaixa(inicioArremesso, _fimTravessia + centro);
    }
}
