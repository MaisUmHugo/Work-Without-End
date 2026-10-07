# Almas do Necromante

Recursos derivados de **CFXR2 Souls Escape**, do **Cartoon FX Remaster Free**, de Jean Moreno, já importado pelo usuário neste projeto.

O prefab adaptado fica em `Assets/Prefabs/Chefes/Necromante/VFX/AlmasNecromante.prefab`. Os materiais, texturas e includes necessários foram copiados para esta pasta com GUIDs próprios. O Ubershader foi exportado pelo importador oficial para um `.shader` comum, com um nome específico do projeto; os avisos de autoria nos arquivos foram preservados.

A adaptação remove os scripts de demonstração, o controlador de limpeza do pacote e a luz 3D. `EfeitoMorteNecromante` controla a criação, brilho, pausa e limpeza no encontro do jogo 2D. As partículas preservam sua configuração e os slots vazios de material usados pelo efeito original. Soft particles estão desativadas nos materiais para funcionar sem exigir textura de profundidade da câmera 2D.

Esses recursos funcionam sem precisar importar a demonstração ou os scripts Editor do Cartoon FX na branch de UI. Preserve as referências do prefab e os includes locais dos shaders ao organizar a pasta.
