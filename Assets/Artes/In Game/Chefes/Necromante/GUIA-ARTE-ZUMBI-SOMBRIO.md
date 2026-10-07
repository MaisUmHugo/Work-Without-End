# Arte do Zumbi Sombrio

O Zumbi Sombrio usa o prefab `Assets/Prefabs/Chefes/Necromante/Invocados/ZumbiSombrio.prefab`. A raiz contém a lógica, o collider e a movimentação. O filho `Visual` contém o `SpriteRenderer` e o `Animator`. A arte do Filipe está integrada nesse filho; troque a arte nele, sem mover ou redimensionar a raiz. Mantenha `SpriteRenderer > Color` e o campo `Cor Sombria` do comportamento brancos, para preservar as cores dos sprites. O aviso de preparação ainda pode usar sua cor temporária. Confira o collider com a programação se a silhueta mudar.

O Animator do `Visual` usa `Animacoes/ZumbiSombrio_Arte.overrideController`, exclusivo dele. O Override usa `Animacoes/ZumbiSombrio.controller`, que reaproveita a estrutura e os parâmetros do Zumbi comum em um controller separado. Joinha e queda entram a partir de qualquer estado, inclusive corrida, sem reiniciar o clip a cada quadro. O controller e as animações do Zumbi comum permanecem intactos. Os clips do Filipe estão em `Animacoes/Zumbi Sombrio/` e as imagens em `Sprites/Zumbi Sombrio/`. Crie novos clips a partir das spritesheets e arraste-os para os campos correspondentes no Inspector; preserve os parâmetros e transições já configurados.

| Clip original no Override | Clip integrado | Quando aparece |
| --- | --- | --- |
| `AndandoZumbi` | `ZumbiSombrioWalk` | Avanço comum; repete |
| `CorrendoZumbi` | `ZumbiSombrioRun` | Avanço rápido; usa a mesma spritesheet da caminhada com tempo menor |
| `EscorregandoZumbi` | `ZumbiSombrioFall` | Colisão; toca uma vez e segura o último quadro |
| `RecebeuZumbi` | `ZumbiSombrioEntrega` | Joinha; toca uma vez e segura o último quadro |
| `TransparenteZumbi` | Fallback existente | Estado herdado, sem acionamento no comportamento atual |

O parâmetro e estado `RecebeuEntrega` já existem no controller e agora usam o joinha do Filipe pelo Override. Na variante **entregável** usada na integração, a segunda caixa aciona esse estado. Uma variante não entregável pode continuar sem acioná-lo. O piscar, a reação às entregas e o crescimento ficam no código; não crie versões brancas ou transparentes das sprites para esses efeitos. Não acrescente transições nem altere scripts de combate para entregar só a arte.

Abra o prefab em Prefab Mode para trocar o sprite parado do `Visual` e ajustar **Transform > Local Scale**. A Scene View mostra o tamanho imediatamente; use a janela Animation em Preview para conferir os quadros sem Play Mode. Mantenha o visual dentro de uma lane e peça revisão do collider antes de fixar a escala final. Inclua os `.meta` dos novos sprites e clips; ao substituir arquivos existentes, preserve os `.meta` para não perder referências.
