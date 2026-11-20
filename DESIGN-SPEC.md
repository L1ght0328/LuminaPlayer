# Lumina Player — visual and implementation baseline

The accompanying `design/lumina-player-visual-reference.png` is the approved concept direction, not a screenshot of the executable.

## Brand
- Product name: Lumina Player.
- Signature: luminous audio-wave mark and wordmark.
- Fixed signature accent: violet/purple in both themes.
- Mood: atmospheric, premium, calm, music-first.

## Layout implemented in this revision
- Left rail: My Music, folders, playlists, equalizer and appearance access.
- Center: search/header, welcome banner, library cards and paging.
- Right rail: current track, playback controls and 10-band equalizer.
- Bottom transport: track status, play/pause, previous/next, shuffle, repeat and volume.
- Local folder picker, filename search, playlists, presets, theme switch and custom background selection.

## Appearance rules
- Dark and light themes.
- User can select or clear a local background image; only the background is customized.
- Purple accent stays fixed; no user accent color selector.
- Panels are translucent but avoid blur and expensive effects.

## Performance rules
- Native WPF controls only; no embedded browser or background services.
- No continuous decorative animations.
- UI timer is 500 ms.
- Folder scanning runs on a worker task.
- Library cards are paged in batches of 60 to cap live UI elements.
- Background image is decoded once at a bounded width.

## Honest feature boundaries
- Track cards currently use a generated visual placeholder; embedded album-art extraction is not yet implemented.
- Artist/album/genre metadata pages, lyrics, and animated spectrum are not implemented.
- Audio format support depends on NAudio and Windows Media Foundation codecs available on the target Windows installation.
- Build and runtime behavior must be verified on Windows before release.
