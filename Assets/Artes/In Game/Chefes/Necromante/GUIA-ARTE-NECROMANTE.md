# Arte do Necromante

O comportamento da luta já escolhe as animações. Para integrar arte, não altere os scripts nem as transições do `Necromante.controller`.

1. Crie os clips a partir das spritesheets. Mantenha cada quadro inteiro e o pivô no centro. As spritesheets estão configuradas com 16 pixels por unidade; preserve o tamanho e o recorte dos quadros ao substituir a arte.
2. Abra `Animacoes/Necromante_Arte.overrideController` e arraste cada clip para o campo `Override` do placeholder correspondente. Os campos ainda sem arte podem continuar com placeholder.
3. Abra o prefab `Prefabs/Chefes/Necromante/Necromante.prefab`. Selecione o filho `Arte` e ajuste **Transform > Local Scale**. A Scene View mostra o novo tamanho imediatamente. Use a janela Animation em Preview para ver os quadros sem Play Mode.
4. Para trocar o projétil, abra `Prefabs/Chefes/Necromante/Projetil/NecromanteProjectile.prefab` e arraste o sprite para o `SpriteRenderer` do filho `Visual`.
5. Teste a luta em Play Mode. O collider da raiz e a posição na cena devem ser revisados com a programação quando o tamanho final estiver definido.

| Placeholder no Override | Arte esperada | Reprodução |
| --- | --- | --- |
| `Placeholder_Idle` | Boss parado | Pode repetir |
| `Placeholder_Movimentacao` | Andar ou flutuar entre posições | Repetir |
| `Placeholder_Invocacao_Inicio` | Levantar o cajado e iniciar o brilho verde | Uma vez; a velocidade se adapta ao preparo do ataque |
| `Placeholder_Invocacao_Carregando` | Cajado verde erguido | Repetir ou sustentar um quadro durante o preparo |
| `Placeholder_Invocacao_Abaixar` | Abaixar o cajado | Uma vez |
| `Placeholder_Invocacao_Sustentar` | Cajado abaixado | Sustentar um quadro até terminar a grande invocação |
| `Placeholder_Teleporte_Fuga` | Desaparecer na fuga | Uma vez; só no fim do primeiro encontro, após a fase 2 |
| `Placeholder_AtaqueProjetil` | Disparo comum | Conforme os quadros disponíveis |
| `Placeholder_TiroCarregado` | Tiro carregado | Conforme os quadros disponíveis |
| `Placeholder_SequenciaRapida` | Sequência rápida | Conforme os quadros disponíveis |
| `Placeholder_Vulneravel` | Vulnerável | Repetir ou sustentar |
| `Placeholder_Morte` | Derrota definitiva | Uma vez |

A invocação troca de `Inicio` para `Carregando` enquanto prepara o ataque. Ao começar as ondas, toca `Abaixar` uma vez e fica em `Sustentar` até o ataque acabar. O teleporte nunca é usado na movimentação comum. `ProjectileRoot` é filho da raiz, separado de `Arte`, para que a escala da arte não altere o disparo.

Mantenha `SpriteRenderer > Color` branco no Necromante e no projétil, para preservar as cores originais das imagens. O feedback de dano e bloqueio usa apenas a opacidade: pisca ao receber dano e reduz brevemente a opacidade ao bloquear uma entrega, restaurando o sprite em seguida. Vulnerabilidade e morte usam suas próprias poses, sem uma cor fixa aplicada sobre a arte.

Nos clips de início da invocação, abaixar o cajado, teleporte e morte, mantenha `Loop Time` desativado. As poses de idle, tiro comum, tiro carregado e vulnerabilidade atualmente usam um quadro; podem ser substituídas por clips completos nos mesmos slots. O ajuste de tamanho da arte não redimensiona o collider da raiz: confira os dois no teste da luta.

Ao substituir uma imagem existente, preserve seu arquivo `.meta`. Ao adicionar um clip ou uma imagem nova, inclua o `.meta` gerado pelo Unity.
