# Lumina Player 1.1.0 — Windows x64

Reprodutor local/offline em C# + WPF (.NET 8) e NAudio. O projeto foi redesenhado para se aproximar da referência visual `design/lumina-player-visual-reference.png`.

## Interface

- Navegação lateral, busca, faixa de boas-vindas, cartões locais e painel lateral de reprodução.
- Barra inferior de transporte e controles de volume.
- Tema escuro/claro com violeta fixo como cor de marca.
- Imagem de fundo local opcional; apenas o fundo muda, não a cor de marca.
- Equalizador de 10 bandas com presets e presets personalizados.

## Funcionalidade atual

- Adicionar pastas locais e pesquisar arquivos de áudio pelo nome.
- Biblioteca apresentada em páginas de até 60 cartões para limitar o número de elementos visuais ativos.
- Duplo clique em uma faixa para reproduzir; play/pause, anterior/próxima, seek, volume, mudo, repetição e embaralhar fila.
- Criar categorias e playlists e adicionar seleção de faixas a uma playlist.
- Configurações guardadas em `%AppData%\LuminaPlayer\config.json`.

A arte dos cartões é um marcador visual local, não uma extração de capas incorporadas nos ficheiros. Artistas, géneros, letras sincronizadas e metadados de álbum ainda não estão implementados. A reprodução depende dos formatos/decoders disponíveis via NAudio e Media Foundation no Windows.

## Desempenho

- WPF nativo; sem WebView, streaming, telemetria ou serviços residentes.
- Sem animações decorativas contínuas.
- Timer de interface limitado a 500 ms.
- Digitalização de pastas executada fora da thread da interface.
- No máximo 60 cartões de música materializados por página.
- Imagem de fundo opcional reduzida para até 1800 px de largura e carregada uma vez.
- EQ usa DSP local e não cria threads por banda.

O consumo real de RAM/CPU depende do dispositivo, do formato de áudio e do driver de áudio e deve ser medido no Windows após a compilação.

## Compilar no Windows

Requisitos: Windows 10/11 x64, .NET 8 SDK e Inno Setup 6.

```powershell
.\publish.ps1
```

Para criar o instalador com Inno Setup instalado:

```powershell
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer/LuminaPlayer.iss
```

O instalador será gerado em `dist/LuminaPlayer-Setup-win-x64.exe`.

## GitHub Actions

O workflow `.github/workflows/build-windows.yml` pode ser iniciado manualmente pela aba Actions. Ao terminar, descarregue o artefacto `LuminaPlayer-Setup-win-x64`.

## Observação de validação

Este pacote contém o código-fonte atualizado. A compilação WPF e o teste de execução precisam ser validados num runner Windows; não declarar a aplicação pronta até o workflow terminar com sucesso e o instalador abrir corretamente.
