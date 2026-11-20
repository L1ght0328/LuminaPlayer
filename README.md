# Lumina Player 1.3.0 — Windows x64

Reprodutor de música local/offline para Windows 10/11, desenvolvido em C# + WPF (.NET 8), NAudio e TagLibSharp. A interface segue a referência visual Lumina: sidebar, banner violeta, biblioteca em cartões, painel “Tocando agora”, equalizador e dock inferior.

## Interação implementada

- Clique em um cartão de música para iniciar a reprodução imediatamente.
- Ctrl+clique em cartões para selecionar várias faixas; use o menu para adicioná-las a uma playlist.
- “Tocar tudo” cria uma fila com a visualização filtrada.
- Pastas adicionadas aparecem na sidebar e como cartões de pasta; duplo clique na pasta inicia sua fila.
- Abas Todas, Músicas, Álbuns, Artistas, Gêneros e Pastas filtram a biblioteca.
- Em Álbuns, Artistas e Gêneros, clicar em um cartão inicia uma fila com as faixas daquele grupo.
- Play/pause, anterior, próxima, shuffle, repetição desligada/faixa/playlist, seek no painel lateral e no dock inferior, volume e mudo.
- Atalho Espaço para play/pause; Ctrl+seta direita/esquerda para próxima/anterior.
- Playlists e categorias podem ser criadas diretamente na sidebar. Duplo clique numa playlist inicia sua fila.
- Busca por título, artista, álbum, gênero e nome do arquivo.

## Biblioteca e metadados

- Adicionar várias pastas locais e reexaminar a biblioteca pelo menu.
- Varredura recursiva, ignorando diretórios de junção/symlink e tratando pastas sem permissão.
- Metadados de título, artista, álbum e gênero via TagLibSharp.
- Capas incorporadas e arquivos `cover.jpg`, `cover.png`, `folder.jpg`, `album.jpg` ou `front.jpg`.
- Capas incorporadas são extraídas para o cache local do Lumina; imagens são carregadas sob demanda.
- Paginação de até 60 cartões para evitar criar milhares de controles WPF de uma vez.
- Quando uma pasta não contém formatos reconhecidos, o status informa que nenhum áudio compatível foi encontrado.

## Reprodução e equalizador

- Reprodução por NAudio com WASAPI compartilhado e fallback para WaveOut quando a inicialização WASAPI falhar.
- Equalizador de 10 bandas com Flat, Bass Boost, Rock, Pop e presets personalizados salvos.
- As bandas configuradas são reaplicadas ao iniciar cada faixa.
- Visualizador de nível de áudio baseado no sinal de saída; atualização limitada ao timer de 500 ms.
- Letras locais em `.lrc` ou `.txt` com o mesmo nome da faixa. O conteúdo é exibido; sincronização temporal de linhas ainda não está implementada.

## Formatos

A lista da biblioteca inclui MP3, WAV, AIFF, WMA, M4A, M4B, AAC e FLAC. A decodificação depende dos codecs Media Foundation disponíveis no Windows; MP3/WAV são os formatos mais amplamente suportados. FLAC/M4A/AAC/WMA variam conforme a versão e os codecs do Windows. Arquivos que o Windows não consiga decodificar mostrarão uma mensagem com o erro retornado pelo decodificador. OGG e OPUS foram removidos da lista para não apresentar como reproduzíveis formatos sem decoder garantido no pacote.

## Desempenho

- WPF nativo, sem WebView, streaming, anúncios, telemetria ou serviços residentes.
- Sem animações decorativas permanentes.
- Timer de interface de 500 ms.
- Varredura e leitura de metadados/capas fora da thread visual.
- Cache de metadados durante a sessão.
- Máximo de 60 cartões materializados por página.
- EQ e visualizador no pipeline de áudio, sem threads dedicadas por banda.
- O consumo real de RAM/CPU precisa ser medido no Windows; este projeto não declara benchmarks não medidos.

## Personalização

- Tema escuro/claro.
- Fundo com imagem local opcional.
- Cores de destaque alternativas.
- Equalizador e sidebar podem ser ocultados pelo menu.
- Layout compacto.
- Configuração salva localmente em `%AppData%\\LuminaPlayer\\config.json`.

## Compilar e criar instalador

Requisitos: Windows 10/11 x64, .NET 8 SDK e Inno Setup 6.

```powershell
.\\publish.ps1
```

Para gerar o instalador:

```powershell
& "C:\\Program Files (x86)\\Inno Setup 6\\ISCC.exe" installer/LuminaPlayer.iss
```

O instalador é gerado em `dist/LuminaPlayer-Setup-win-x64.exe`.

## GitHub Actions

O workflow `.github/workflows/build-windows.yml` pode ser iniciado manualmente em **Actions → Build Windows installer → Run workflow**. Ao terminar, baixe o artefato `LuminaPlayer-Setup-win-x64`.

## Validação

O XAML foi validado como XML e os nomes de eventos do XAML foram comparados com os handlers C#. O ambiente de preparação não dispõe do SDK .NET/Windows para compilar e executar WPF. A compilação no runner Windows e o teste de inicialização/reprodução no PC de destino continuam obrigatórios antes de considerar esta versão estável.
