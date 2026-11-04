# LinuxPartsFinder

The Linux edition of [WinPartsFinder](https://github.com/Ian-Johnston/WinPartsFinder) by Ian Johnston.

LinuxPartsFinder searches several electronic component suppliers at once - Mouser, DigiKey, element14 (Farnell, CPC
and Newark) and TME through their official APIs, plus website links for RS, Rapid, LCSC, AliExpress and any you
add - and puts stock, prices, minimum order and lifecycle status side by side in one grid.

## Download

Get the latest `.AppImage` from the **Releases** page:

- **LinuxPartsFinder-linux-x64.AppImage** - most PCs and laptops (Intel or AMD).
- **LinuxPartsFinder-linux-arm64.AppImage** - 64-bit ARM computers, e.g. a Raspberry Pi 4 or 5 with a 64-bit OS.

Not sure which? Open a terminal and run `uname -m`: **x86_64** means x64, **aarch64** means arm64.

Needs a 64-bit Linux desktop from about 2020 or later (for example Ubuntu 20.04, Debian 11, Fedora 33, Linux Mint 20
or newer). Nothing needs installing - the AppImage is the whole program in one file.

## First launch

Linux won't run a downloaded file until it's marked as executable. Once:

1. Move the `.AppImage` wherever you'd like to keep it, e.g. your home folder or `~/Applications`.
2. Right-click it > **Properties** > **Permissions**, and tick **Allow executing file as program** (the wording varies
   a little between desktops).
3. Double-click it to run.

Or in a terminal, in the folder you saved it to:

```bash
chmod +x LinuxPartsFinder-linux-x64.AppImage
./LinuxPartsFinder-linux-x64.AppImage
```

To update, download the new `.AppImage` and do the same - your settings and keys are kept.

## API keys

Each user needs their own free API keys - click **Settings…**, then **API Help** for step-by-step instructions.
Keys are kept in your desktop's keyring (they show as LinuxPartsFinder in Passwords and Keys, or KWallet Manager on
KDE), never in the app or its settings file. On a desktop without a keyring they're saved in
`~/.config/LinuxPartsFinder/keys.json`, which only your user account can open.

Settings are saved in `~/.config/LinuxPartsFinder/settings.json`.

## Building

C# with [Avalonia UI](https://avaloniaui.net), .NET 10. `dotnet build` builds and runs it on Windows for testing.
The Linux downloads are built by GitHub Actions (`.github/workflows/release.yml`): push a tag such as `v1.0.0` for a
release, or run the workflow by hand for a test build.

## Licence

Free for personal, non-commercial use - see About in the app for the full licence and disclaimer.
© 2026 Ian Johnston.
