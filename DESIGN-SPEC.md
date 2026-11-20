# Lumina Player — visual specification 1.2.0

## Design intent

Reproduce the supplied 1280x720 reference as a native Windows desktop application, preserving the Lumina violet identity while keeping the interface light and offline-first.

## Layout

- Top bar: Lumina wordmark, wide search field, add-folder action and compact menu.
- Left rail: library navigation (My Music, Explore, Playlists, Artists, Albums, Genres, Folders), linked roots, equalizer and appearance controls.
- Center: purple welcome banner, category tabs, local folder tiles and a paged 3-column music-card grid.
- Right rail: current cover and metadata, seek, transport, low-cost static waveform, 10-band equalizer and local lyrics panel.
- Bottom dock: current track, transport, timeline, mute and volume.

## Visual language

- Core brand accent: Lumina violet (#B65CFF). Accent variants are optional but brand identity remains violet by default.
- Dark surfaces: near-black plum (#100D1B), translucent plum panels and subtle violet borders.
- Light mode changes the neutral surfaces and text, not the basic identity.
- Background image is optional and affects the window background only; cards and panels retain legible surfaces.
- Rounded corners, restrained borders, clear hierarchy, compact typography.

## Performance constraints

- Native WPF; no embedded browser or cloud service.
- No continuous decorative animation. Waveform bars are static by design.
- Playback UI timer: 500 ms.
- Library enumeration and metadata extraction run off the UI thread.
- Track cards are paged to 60 items.
- Cover art is extracted to a local cache and decoded at reduced width only when needed for the now-playing pane.
