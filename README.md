# Lumina Player — Windows 10/11 x64

Reprodutor de música local, offline e leve, construído em C#/.NET 8 com WPF e NAudio.

## Prioridades de projeto

- Sem streaming, anúncios, telemetria, serviços em segundo plano ou conexão obrigatória com a internet.
- Interface WPF nativa, sem animações contínuas nem capas baixadas da internet.
- Atualização do relógio do player a cada 500 ms.
- Biblioteca indexada em memória durante a sessão para evitar revarrer o disco a cada tecla digitada na busca.
- Configurações pequenas em JSON dentro de `%AppData%\LuminaPlayer`.
- Publicação self-contained x64: o usuário final não precisa instalar o .NET separadamente.

## Recursos

- Adicionar pastas locais e pesquisar músicas.
- Reprodução, pausa, anterior, próxima, seek, volume e mudo.
- Repetição de faixa e de fila.
- Playlists e categorias locais.
- Equalizador de 10 bandas com presets Flat, Bass Boost, Rock e Pop, além de presets personalizados.
- Tema claro/escuro e acento configurável no arquivo de configuração.
- Atalhos: Espaço para play/pause; Ctrl+Seta esquerda/direita para faixa anterior/próxima.

## Formatos de áudio

A reprodução usa NAudio e os decodificadores disponíveis no Windows Media Foundation. MP3 e WAV são os formatos de referência. M4A/AAC, WMA, AIFF e FLAC dependem dos codecs presentes na versão do Windows. OGG/Opus podem não estar disponíveis em instalações padrão do Windows; não são prometidos como compatíveis nesta compilação.

## Gerar o instalador no GitHub Actions

1. Envie o conteúdo desta pasta para um repositório GitHub.
2. Confirme que `.github/workflows/build-windows.yml` está presente.
3. Abra a aba **Actions**.
4. Selecione **Build Windows installer** e use **Run workflow** na branch `main`.
5. Ao terminar com ✓ verde, abra a execução e baixe o artifact `LuminaPlayer-Setup-win-x64`.
6. Extraia o ZIP do artifact. Dentro dele estará `LuminaPlayer-Setup-win-x64.exe`.

## Gerar localmente em Windows

Requisitos para quem compila: .NET 8 SDK e Inno Setup 6.

Abra PowerShell na pasta do projeto e execute:

```powershell
./publish.ps1
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer/LuminaPlayer.iss
```

O instalador será criado em `dist/LuminaPlayer-Setup-win-x64.exe`.

## Observação de validação

O código deve ser compilado e testado em Windows x64 antes de distribuir a terceiros. Este pacote contém o projeto e o pipeline de build; não contém um binário `.exe` pré-compilado.
