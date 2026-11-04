# LinuxPartsFinder

The Linux edition of [WinPartsFinder](https://github.com/Ian-Johnston/WinPartsFinder) by Ian Johnston.

LinuxPartsFinder searches several electronic component suppliers at once - Mouser, DigiKey, element14 (Farnell, CPC
and Newark) and TME through their official APIs, plus website links for RS, Rapid, LCSC, AliExpress and any you
add - and puts stock, prices, minimum order and lifecycle status side by side in one grid.

## Download

Linux downloads will be on the **Releases** page, for 64-bit PCs (x64) and 64-bit ARM computers (arm64).

Needs a 64-bit Linux desktop from about 2020 or later (for example Ubuntu 20.04, Debian 11, Fedora 33, Linux Mint 20
or newer).

## API keys

Each user needs their own free API keys - click **Settings…**, then **API Help** for step-by-step instructions.
Keys are kept in your desktop's keyring (they show as LinuxPartsFinder in Passwords and Keys, or KWallet Manager on
KDE), never in the app or its settings file. On a desktop without a keyring they're saved in
`~/.config/LinuxPartsFinder/keys.json`, which only your user account can open.

Settings are saved in `~/.config/LinuxPartsFinder/settings.json`.

## Building

C# with [Avalonia UI](https://avaloniaui.net), .NET 10. `dotnet build` builds and runs it on Windows for testing.

## Licence

Free for personal, non-commercial use - see About in the app for the full licence and disclaimer.
© 2026 Ian Johnston.
