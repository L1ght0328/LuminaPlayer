# Lumina Player — Windows 10 x64

Player desktop nativo para Windows 10 (64-bit), sem navegador e sem serviço online. O projeto usa WPF/.NET 8 e NAudio. A interface e a reprodução rodam no processo desktop; não há console no executável publicado.

## Stack escolhida

- **C# + .NET 8 + WPF**: GUI Windows nativa, integração com janelas de seleção, barra de tarefas, atalhos e baixo overhead comparado a Electron.
- **NAudio**: saída de áudio WASAPI/Windows, leitura dos formatos suportados pelo NAudio/Media Foundation.
- **DSP próprio (biquads peaking)**: equalização de 10 bandas em tempo real, sem depender de um equalizador externo.
- **JSON em `%AppData%`**: biblioteca, categorias, playlists e preferências.

## Formatos de áudio

O player tenta abrir cada arquivo usando `AudioFileReader` e, se necessário, `MediaFoundationReader`. MP3, WAV, AIFF e formatos reconhecidos pelos codecs do Windows são suportados. FLAC/OGG dependem de codecs Media Foundation instalados no Windows; a lista de extensões é extensível em `LibraryService.cs`. Não incluí um pacote de codecs de terceiros para manter o instalador pequeno e evitar redistribuição/licenciamento de codecs.

## Requisitos para COMPILAR

- Windows 10 64-bit atualizado.
- Visual Studio 2022 (workload **Desenvolvimento para desktop com .NET**) ou .NET 8 SDK.
- Internet apenas na máquina de desenvolvimento para baixar o pacote NuGet NAudio. O aplicativo final funciona offline.

## Abrir e compilar no Visual Studio

1. Extraia o ZIP.
2. Abra `LuminaPlayer.sln` no Visual Studio 2022.
3. Aguarde a restauração dos pacotes NuGet.
4. Selecione `Release` e `x64`.
5. Menu **Compilar > Publicar LuminaPlayer** ou use o comando abaixo.

## Gerar EXE independente (sem console)

Abra o **Developer PowerShell for Visual Studio** ou PowerShell na pasta que contém `LuminaPlayer.csproj` e execute:

```powershell
dotnet restore
dotnet publish .\LuminaPlayer.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false `
  -o ..\publish
```

O projeto define `OutputType=WinExe`, então não abre janela de terminal. O executável estará em `publish\LuminaPlayer.exe`. Copie a pasta `publish` para o PC destino (ou distribua o EXE se o publish single-file tiver incluído todas as dependências).

**Nota:** publicação self-contained inclui o runtime .NET e tende a produzir um arquivo maior, mas não exige instalar .NET no PC do usuário. Para reduzir tamanho, remova `--self-contained true` e publique framework-dependent, mas o PC destino precisará do .NET Desktop Runtime 8.


## Gerar instalador `.exe` automaticamente (recomendado)

O repositório inclui `publish.ps1`, `installer/LuminaPlayer.iss` e um workflow do GitHub Actions em `.github/workflows/build-windows.yml`. Como WPF só pode ser compilado em Windows, o workflow usa um runner Windows para publicar o app self-contained x64 e empacotá-lo em um instalador Inno Setup.

1. Envie esta pasta para um repositório GitHub.
2. Abra a aba **Actions** e habilite workflows, se o GitHub solicitar.
3. Execute **Build Windows installer** em **Run workflow**.
4. Quando terminar, abra a execução e baixe o artefato `LuminaPlayer-Setup-win-x64`. Ele contém `LuminaPlayer-Setup-win-x64.exe`.
5. Para criar também uma Release pública, crie e envie uma tag começando com `v`, por exemplo `v1.0.0`; o workflow anexará o instalador à Release.

O instalador cria atalhos no Menu Iniciar e, opcionalmente, na área de trabalho. O aplicativo publicado inclui o runtime .NET, então o computador de destino não precisa instalar o .NET separadamente.

## Uso

- **Adicionar pasta**: escolhe uma pasta no seletor nativo do Windows; a varredura é recursiva.
- **Duplo clique em faixa**: toca e cria a fila a partir da lista atual.
- **Categorias e playlists**: crie categoria, renomeie categoria, crie playlist e adicione seleção.
- **Playlists**: duplo clique na playlist carrega sua fila.
- **Loop**: alterna Desligado → Uma faixa → Playlist.
- **Equalizador**: 10 bandas, -12 a +12 dB; presets e presets personalizados salvos localmente.
- **Preferências**: tema, cor de destaque, layout compacto e visibilidade de seções.
- **Atalhos**: Espaço, Ctrl+Seta direita e Ctrl+Seta esquerda.

## Observações técnicas

- Pastas de playlists são categorias virtuais dentro do Lumina; arquivos originais não são movidos.
- A varredura inicial é síncrona para manter o projeto enxuto. Bibliotecas muito grandes podem levar alguns segundos; para milhares de arquivos, o próximo passo é mover o scanner para `Task.Run` com progresso/cancelamento.
- O player usa uma cadeia de DSP em ponto flutuante. O equalizador é aplicado em tempo real ao áudio reproduzido.
- A saída de áudio é encerrada ao fechar a janela.
