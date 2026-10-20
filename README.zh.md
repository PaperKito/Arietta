简体中文 | [English](./README.md)

# Arietta

> 名字取自意大利语 *arietta*（*aria* 的指小形式，即「小咏叹调」；该词另有「微风」之义）

Arietta 旨在解决官方启动器的不足，它支持 Kuro Games 旗下所有的 PC 游戏（鸣潮，战双帕弥什）。

除启动器的基本功能外，我根据个人需求增加了额外功能，例如：

- **游戏时长记录**
- **截图管理**
- **账号切换**
- **库街区社区**

还有其他功能等你探索。

---

## 安装

发布版是免安装压缩包，解压后双击 `Arietta.exe` 即可。所有数据默认存放在 exe 同级的 `Arietta\` 目录下，整个目录拷走就能迁移。

- **系统要求**：Windows 10 1809（build 17763）及以上，x64
- 已安装 [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2)
- 已安装 [WebP 映像扩展](https://apps.microsoft.com/detail/9pg2dk419drg)
- 为了更好的使用体验，请在系统设置中开启**透明效果**和**动画效果**

然后在 [GitHub Release](https://github.com/PaperKito/Arietta/releases) 下载对应 CPU 架构的压缩包，解压后运行 `Arietta.exe` 并按提示操作

## 开发

在本地编译应用，你需要安装最新版的 Visual Studio 并选择以下负载：

- .NET 桌面开发
- 使用 C++ 的桌面开发
- 通用 Windows 平台开发

## 数据与隐私

- 数据默认不出本机；只有用户主动配置了 WebDAV，云同步才会发生
- 本项目没有任何服务端，库街区登录态与抽卡链接只在本机与官方接口之间使用

## 第三方

- 部分 UI 结构与属性命名参考了 [Starward](https://github.com/Scighost/Starward)（MIT）
- 其余依赖 NuGet 包见 `src/Arietta.csproj`

## 赞助

开发不易，如果你觉得 Arietta 好用，可以在[赞助页面](https://paperkito.github.io/Arietta/sponsor/)赞助我。

## 许可

MIT，见 `LICENSE`。

Arietta 是社区非商业项目，与库洛游戏（Kuro Games）及哔哩哔哩没有隶属关系；游戏及其素材的版权归各自权利人所有。