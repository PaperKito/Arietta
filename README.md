English | [简体中文](./README.zh.md)

# Arietta

> The name comes from Italian *arietta* — the diminutive of *aria* ("a little aria"); the word also means "a light breeze".

Arietta aims to make up for what the official launcher is missing. It supports all Kuro Games PC titles (Wuthering Waves, Punishing: Gray Raven).

Besides the launcher basics, I added extra features driven by my own needs, for example:

- **Playtime tracking**
- **Screenshot management**
- **Account switching**
- **KuroBBS community**

And there is more to explore.

---

## Install

The release is a portable zip: unzip it and double-click `Arietta.exe`. All data lives by default in an `Arietta\` folder next to the exe — move the whole folder and you have migrated.

- **System requirements**: Windows 10 1809 (build 17763) or later, x64
- [.NET 10 runtime](https://dotnet.microsoft.com/download/dotnet/10.0) installed — the portable build is not self-contained, so the **Desktop Runtime** is the safest pick (it includes the base runtime)
- [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2) installed
- [WebP Image Extensions](https://apps.microsoft.com/detail/9pg2dk419drg) installed
- For the best experience, enable **Transparency effects** and **Animation effects** in Windows Settings

Then download the archive for your platform from [GitHub Releases](https://github.com/PaperKito/Arietta/releases), unzip it, run `Arietta.exe` and follow the prompts.

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

Development takes real effort. If you find Arietta useful, you can sponsor me via [the sponsor page](https://paperkitoblog-dpbyupmog00q.edgeone.dev/sponsor/).

## License

MIT — see `LICENSE`.

Arietta is a non-commercial community project and is not affiliated with Kuro Games or bilibili; the games and their assets are copyrighted by their respective owners.
