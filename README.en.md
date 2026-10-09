English | [简体中文](./README.md)

# Arietta

> The name comes from Italian *arietta* — the diminutive of *aria* ("a little aria"); the word also means "a light breeze".

Arietta is an unofficial, non-commercial community launcher for Kuro Games titles. It fills in the gaps the official launcher leaves behind, and it supports every PC game Kuro Games ships — Wuthering Waves and Punishing: Gray Raven — across 8 servers (CN, B, global, and more), with per-server data isolation.

On top of the basics — picking a server, launching, updating — I added a fair amount around my own needs:

- **Playtime tracking** — counts only the sessions launched from this launcher; total time, launch count, active days, longest single session, plus bar charts for the last 15 days / 12 weeks / 12 months
- **Gacha log** — import an aqua link and it pulls the records for you; banner analytics with pity counters and 5★ / 4★ breakdowns, matching paimon.moe's definitions
- **KuroBBS community** — daily check-in and monthly calendar, character builds, Wuthering Waves Tower of Adversity / Echoes / Endless Binance, and PGR resource reports plus combat records
- **Screenshot management** — auto-locates the game's screenshot folder, groups them by date, batch-deletes to the system recycle bin
- **Install & update** — pick your resource tier (UHD / HD / SD), differential and krzip group patches, resumable downloads, multi-connection segmented acceleration, pre-download, and file verification with one-click repair
- **In-game settings** — display mode, resolution, HDR, voice language, plus gamepad key mapping
- **Dynamic background** — pulls the official animated backgrounds, or bring your own image / video; dominant-color extraction retints the whole UI accent
- **WebDAV sync** — single-database incremental sync with dual-anchor fingerprints, with automatic timestamped backups kept as history
- **Toolbox** — ADB serial emulator, KuroBBS token / did capture

The UI ships in 10 languages (Simplified Chinese, Traditional Chinese, English, Japanese, Korean, French, German, Spanish, Thai, Brazilian Portuguese) and switches instantly without a restart.

And there is more to explore.

---

## Install

The release is a portable zip: unzip it and double-click `Arietta.exe`. All data lives by default in an `Arietta\` folder next to the exe — move the whole folder and you have migrated.

- **System requirements**: Windows 10 1809 (build 17763) or later, x64
- [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2) installed
- [WebP Image Extensions](https://apps.microsoft.com/detail/9pg2dk419drg) installed
- For the best experience, enable **Transparency effects** and **Animation effects** in Windows Settings

Then download the archive for your platform from [GitHub Releases](https://github.com/Pizazz-stack/Arietta/releases), unzip it, run `Arietta.exe` and follow the prompts.

## Development

To build the app locally, you need the latest Visual Studio with the following workloads:

- .NET desktop development
- Desktop development with C++
- Universal Windows Platform development

## Data & Privacy

- Data stays on your machine by default; cloud sync only happens after you explicitly configure WebDAV
- This project runs no server of its own — the KuroBBS login state and the gacha links are only used between your machine and the official endpoints

## Third-party

- Parts of the UI structure and property naming reference [Starward](https://github.com/Scighost/Starward) (MIT)
- The remaining NuGet dependencies are listed in `src/Arietta.csproj`

## Sponsor

Development takes real effort. If you find Arietta useful, you can sponsor me via ------

## License

MIT — see `LICENSE`.

Arietta is a non-commercial community project and is not affiliated with Kuro Games or bilibili; the games and their assets are copyrighted by their respective owners.
