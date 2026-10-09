# UI — preparação para o Filipe

Os controles são apenas uma tela de consulta por enquanto. Não há botões para remapear teclas. A sensibilidade continua ajustável nas opções e no pause.

## Cenas para trabalhar e conferir

- `Assets/Scenes/MenuPrincipal.unity`: menu, opções, ranking e painel de controles.
- `Assets/Scenes/CenaJogo.unity`: cena da partida usada na build, com hordas, encontros do Necromante e transição Cidade → Floresta. Os prefabs compartilhados já contêm os ajustes da UI e do Sombrio.
- `Assets/Scenes/Cenas Testes/NecromanteIntegracao.unity`: teste da integração entre hordas, boss, HUD e transição. Possui `ProgressaoNecromante` e os atalhos de fase 3, fuga e morte.
- `Assets/Scenes/Cenas Testes/Necromante.unity`: teste do boss isolado, com hordas convencionais suspensas e atalhos de ataques/Sombrio. Para testar os dois encontros e a troca de cenário, use `NecromanteIntegracao`.

As cenas de teste são versionadas para compartilhar o trabalho, mas não entram na lista de cenas da build final. Para conferir a partida completa, inicie pelo MenuPrincipal. Faça as alterações comuns nos prefabs para atualizar também a CenaJogo, evitando deixá-las somente como overrides de uma cena de teste.

## Painel de opções compartilhado

Edite `Assets/Prefabs/UI/PainelOpcoes.prefab`. O menu e o pause usam instâncias desse mesmo prefab, com o fundo novo `Pause.png` e as medidas do painel preparado pelo Filipe.

- `Fundo`: arte e dimensões do painel.
- `PauseScroll View/Viewport/Content`: volumes, sensibilidade, parallax e botão Controles. Os itens mantêm o intervalo de 128 unidades entre centros; a área de rolagem permite alcançar todos.
- `BotaoControles`: 270 × 96 unidades, com a arte `ControlesButton.png` preparada pelo Filipe. O texto separado fica oculto porque a imagem já contém a legenda.
- `ModoMiraControle`: esquerda/direita no controle ou clique nas metades do seletor alternam entre Direções Fixas, Livre e Cursor. Direções Fixas mantém as direções anteriores e acrescenta diagonais de 15° e 30° à frente. Livre orienta a mira pela direção do analógico direito; Cursor move a mira pela tela como um mouse. A descrição fica logo abaixo do seletor, com um tooltip próximo. O mouse continua com mira livre em todos os modos.
- `AssistenciaMiraToggle`: permite ligar ou desligar a assistência nos modos Livre e Cursor. Ela aproxima a direção da entrega de um alvo válido, com correção limitada; não dispara automaticamente nem modifica as direções fixas. As opções são salvas e compartilhadas entre menu, pause e partidas. Intensidade, ângulo de busca e limite de correção ficam no componente `Mira` de `Assets/Prefabs/CanvasMira.prefab`.
- `PauseScroll View/Botao_Voltar`: usa `BackButton.png`, com seta e palavra Voltar na própria arte, em 200 × 80 unidades. Fica centralizado nas opções do menu. No pause, retorno e saída formam um conjunto centralizado, nas posições horizontais −60 e 120, com espaço entre eles. O botão de sair fica apenas na instância do `PauseController`.

As conexões com `MenuController` e `PauseController` ficam nos respectivos prefabs pais. Preserve os componentes Button, Slider, Toggle e FeedbackSelecaoUI ao alterar a arte. Não é preciso executar um setup para usar o painel nas cenas existentes.

`FocoUI` mantém a ordem vertical dos itens independentemente da rolagem. Depois de Controles, descer seleciona Voltar; subir retorna a Controles. No rodapé do pause, esquerda/direita alternam entre Voltar e Sair. Um cursor parado não troca a seleção quando os itens rolam sob ele; mover o mouse continua selecionando normalmente.

O pause começa fechado, incluindo quando a cena é reiniciada. Abrir o pause oculta a HUD comum, a HUD do boss e a mira da partida. Fechar restaura a visibilidade anterior, inclusive o preenchimento de um fade interrompido; acessar Controles ou a confirmação de saída mantém a HUD oculta. No menu, o controle mantém a navegação normal por seleção. No pause, o analógico direito também move o ponteiro livremente; RT/R2 clica no ponteiro e A/X confirma a seleção. O cursor do sistema fica oculto durante a partida e retorna ao abrir os painéis.

## Painel de controles

Edite `Assets/Prefabs/UI/PainelControles.prefab`. Ele é compartilhado pelo menu e pelo pause: altere o prefab para atualizar ambos.

- `Fundo`: imagem do painel; pode receber a arte final.
- `Titulo`: título da tela.
- `TecladoMouse` e `Controle`: cada grupo contém quatro linhas de ação e indicação do botão/tecla.
- Em cada linha, `Acao` identifica a função e `Binding` mostra o controle correspondente.
- `Dica`: informação sobre sensibilidade.
- `BotaoVoltar`: botão funcional com navegação e destaque da seleção.

O botão Controles usa a arte `ControlesButton.png`. Os checkboxes das opções usam `CheckBoxUI.png` e `CheckBoxCheckUI.png`. Voltar usa a arte `BackButton.png`, proporcional e com destaque de seleção no padrão do menu. O texto separado de Voltar fica desativado porque a própria imagem já contém a legenda. Preserve o Button, o FeedbackSelecaoUI e os eventos ao trocar a imagem.

O acesso fica em **Opções → Controles** e **Pause → Controles**. `MenuController` e `PauseController` possuem a referência `_painelControles`; os eventos de abrir/voltar já estão conectados em cada prefab. Preserve essas referências e os eventos ao trocar a arte.

As linhas de volume e sensibilidade conservam 128 unidades entre os centros; a opção de parallax e o botão Controles seguem o mesmo intervalo. Os sliders continuam com 480 × 96 unidades. Os botões de retorno e saída ficam no rodapé, fora da área rolável, nas medidas do painel do Filipe. Amplie apenas a altura do Content se adicionar opções.

## HUD do boss

Edite `Assets/Prefabs/UI/HUDBoss.prefab`. O layout anterior foi restaurado, com o nome acima da barra. O texto da fase e o valor numérico de vida ficam desativados; o jogador acompanha a vida pelo preenchimento da barra.

Preserve as referências de `HUDBoss` a nome, fase e preenchimento, mesmo com os textos ocultos. Os fades do encontro usam o grupo `PainelBoss`: mantenha os elementos do HUD dentro dele.

Durante a luta, a HUD comum mantém vidas, pontuação e aviso de entregas perdidas. Apenas a barra de entregas e os textos da horda ficam ocultos. Na fuga e na morte, toda a HUD desaparece; ela retorna com fade somente depois da animação e do desaparecimento do boss, aguardando também a troca de cenário quando prevista.

## Aviso de entregas perdidas e tutorial

O objeto `ComboText` mostra `COMBO: X` com prioridade enquanto o combo está ativo. Sem combo, mostra `ENTREGAS PERDIDAS: 1/3` ou `2/3` quando entregáveis passam. O aviso de perdas muda para amarelo e vermelho. Acertar uma entrega aceita zera as perdas e volta a mostrar o combo atual. Três perdas seguidas custam uma vida e zeram o aviso. Sem combo e sem perdas, o texto fica oculto, como antes. A primeira caixa aceita pelo Sombrio e um acerto no boss vulnerável também zeram as perdas. Colisões continuam causando dano diretamente. Os dois avisos usam o mesmo texto, fonte e posição; textos e efeitos de combo, pontuação, recordes e recuperação de vida continuam funcionando.

Edite `Assets/Prefabs/GameObjects/ComoJogarController.prefab`. O fundo é compartilhado pelos dois slides. `SlideEntrega` mantém o texto e o vídeo anteriores e `BotaoProximoSlide` para avançar. À esquerda abaixo do vídeo, `ModoMiraControle` permite escolher Direções Fixas, Livre ou Cursor quando há controle conectado. Usa a mesma preferência do menu e do pause; conectar ou desconectar o controle durante o primeiro slide atualiza sua visibilidade. `SlideEntregasPerdidas` explica a regra das três perdas e contém os botões/opções anteriores. A partida permanece suspensa até clicar em começar no segundo slide; No componente `ComoJogarController`, `Permitir Ocultar Tutorial` está desligado para a BGS: o tutorial aparece em toda partida, ignora a preferência salva e oculta o checkbox Não Aparecer Mais. Para a versão do itch, ligue essa opção no prefab antes da build; a preferência continua válida para todo o tutorial e não é apagada pela configuração da BGS.

O segundo vídeo usa `Assets/Artes/Videos/Tutorial_Perca_Entrega.mp4`, conectado ao componente VideoPlayer de `VideoPlayerPerdas`. Os dois VideoPlayers ficam sob o painel compartilhado, separados dos slides visuais, para preparar os clipes ao abrir o tutorial. Cada vídeo toca somente no slide correspondente; o segundo repete até começar a partida. Seu RenderTexture `Assets/Prefabs/UI/VideoTutorialPerdas.renderTexture` está conectado ao `VideoRenderPerdas`. Preserve o Button da seta do primeiro slide e seu evento `ProximoSlide` ao trocar a arte.

## Corações da vida

Edite `Assets/Prefabs/GameObjects/CanvasHUD.prefab`, em `VidasCoracoes`. Há três imagens iniciais (`Coracao1`, `Coracao2`, `Coracao3`), com layout horizontal e tamanho definido no LayoutElement de cada uma.

- Troque o Sprite de `Coracao1` pela arte final; `HUDManager` usa essa imagem como modelo e aplica o sprite aos demais ícones ao iniciar e ao atualizar as vidas.
- Se a arte já estiver colorida, deixe a cor das imagens branca para respeitar a cor original. O placeholder branco recebe um tint vermelho.
- Preserve o nome `VidasCoracoes` e mantenha o grupo ao lado de `VidasText`, no mesmo Canvas. O controlador existente fica no `GeralManagement`, com referências atribuídas pela cena.
- `VidasText` fica desativado no prefab. Ele permanece como referência para cenas antigas e não precisa ser apagado.
- Com três vidas aparecem três corações; ao perder uma, o último some e ficam dois. A cura devolve o ícone. Vidas extras de cheats também geram ícones, sem alterar a vida real para caber na HUD.

`HUDManager` também aceita referências explícitas para Grupo Coracoes e Sprite Coracao no Inspector, caso a organização da cena precise mudar. Os corações continuam dentro do grupo de fade da HUD comum.

## Morte, fumaça pixel e progressão após o boss

No prefab `Necromante`, `ControleAnimatorNecromante` controla a morte em três trechos: início a **0.5**, meio a **0.25** e final a **0.35**. O meio começa em 30% do clipe e o desmanche em 72%, ajustáveis no Inspector. O controlador percorre o mesmo clipe de arte, respeitando pause e esses tempos.

`EfeitoMorteNecromante` usa cinco sprites da smoke **FX001**, do Free Pixel Art FX Package importado pelo usuário. **Intensidade Brilho = 1** mantém a morte sem glow, conforme aprovado em teste. Os quadros ficam em `Efeitos/FumacaPixel`; a cópia tem GUIDs próprios e não depende da cena de demonstração. Não há fantasmas conectados ao boss.

A smoke começa durante o desmanche, acompanha a parte inferior do corpo e cobre o sprite enquanto ele desaparece. **Duracao Fumaca = 1.2 segundos**, cobertura, altura no corpo e cor são ajustáveis. Antes de chegar ao último frame, o sprite do boss fica invisível. A fumaça percorre seus desenhos, dissipa e seu objeto é destruído ao terminar; o encontro aguarda essa conclusão. A pausa suspende os dois, e reiniciar restaura material e visibilidade do boss.

Após a fuga, ocorre Cidade → Floresta antes do retorno da HUD. Após a segunda vitória, ocorre Floresta → Cidade antes do retorno da HUD. A partir daí os cenários alternam a cada dez hordas concluídas (30, 40, 50...), e o boss reaparece a cada quinze hordas (35, 50, 65...), das fases 2 a 3. Os reencontros seguintes não forçam uma troca extra de cenário; quando os ciclos coincidem, a transição termina antes da entrada do boss.

Em `ProgressaoNecromante`, **Respiro Apos Derrota Definitiva = 8 segundos** segura novos spawns antes da próxima horda. Em `HordaManager`, os valores iniciais são **Multiplicador Pos Necromante = 1.6**, **Intervalo Pos Necromante = 0.6** e **Aumento Por Horda Pos Necromante = 0.05**. Isso aumenta a velocidade, reduz o intervalo de spawn e faz a dificuldade continuar crescendo. O intervalo mínimo seguro do SpawnerManager continua sendo respeitado. Ajuste esses valores pelo Inspector após testar o balanceamento.

`Hordas Entre Reaparecimentos = 15` e `Aumento Dificuldade Reaparecimento = 0.15` controlam o novo ciclo. O primeiro reencontro adicional usa dificuldade 1.50, depois 1.65, 1.80 e assim por diante. Cada vitória completa concede o bônus configurado uma única vez e aumenta o total mostrado nas estatísticas quando houver várias vitórias. Em `SpawnerManager`, `Multiplicador Intervalo Floresta = 1.25` aumenta em 25% o intervalo entre grupos de spawn na Floresta, sem alterar o calendário de hordas.

## Atalhos para conferir a UI durante a partida

No componente `ProgressaoNecromante`, marque `Ativar Cheats`. Os atalhos funcionam no Editor e em Development Build, respeitando pause e game over.

- **Shift + F1**: apresenta o reencontro na Floresta e inicia a fase 3 com sua vida própria cheia.
- **Shift + F2**: apresenta o primeiro encontro, esgota sua vida, mostra a fuga e faz a transição real para a Floresta.
- **Shift + F3**: apresenta o reencontro e esgota os dois trechos de vida para mostrar a morte definitiva e o retorno da HUD comum.
- **Shift + 8 / 9**: primeiro encontro / reencontro, sem derrotar automaticamente.

Os novos testes também estão no menu de contexto do componente. Eles usam os eventos normais de vida, animação, HUD e pontuação; não apagam o registro da partida. Para uma nova partida, reinicie o Play Mode.

## Zumbi Sombrio

O tamanho considera os pixels visíveis da arte, pois o sprite do Sombrio tem mais espaço transparente que o sprite do zumbi comum. Alterar apenas a dimensão do quadro da spritesheet pode mudar essa comparação.

A primeira entrega aumenta o visual em 20% (`Escala Por Entrega = 1.2`), mantendo o collider e a lane. Ao completar as duas entregas, o visual volta ao tamanho original para o joinha.

Após a segunda entrega, o Sombrio conclui o joinha, pisca e fica transparente como os demais entregáveis, mantendo o último frame da sua própria arte. Ele continua se deslocando pela esquerda na altura em que recebeu a entrega e é removido ao sair da câmera; existe também um limite de tempo de limpeza no Inspector. O clipe `ZumbiSombrioTransparente` evita trocar o Sombrio pelo sprite do zumbi comum.

No Animator exclusivo do Sombrio, `RecebeuEntrega` e `Transparente` são parâmetros bool. Preserve seus tipos e as transições: a entrega pode terminar tanto durante a caminhada quanto durante a corrida, e o estado transparente segura o joinha sem reiniciar a animação.

## Rebind em uma etapa futura

A base consultada está em `D:/Documentos/6th-Semester-Project/Assets/Game/Scripts/Input/Rebinding` e em `Assets/Game/Scripts/UI/ControlsSettingsUI.cs` daquele projeto. Ela permite usar o componente de rebind da Unity, salvar alterações, cancelar a captura e tratar conflitos.

Antes de adaptar, precisamos usar as ações de `INPUTS/Gameplay`, separar corretamente teclado/mouse e controle e aplicar os bindings salvos às instâncias de INPUTS criadas por movimentação, mira, entrega, menu e pause. Alterar apenas o InputActionReference do painel não atualiza essas instâncias automaticamente. O catálogo de ícones e os painéis de confirmação do outro projeto são dependências de UI que precisam ser adaptadas; não é necessário trazer os sistemas de multiplayer.
