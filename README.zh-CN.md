# [⬇ 下载链接 / Download](https://github.com/Fred-Huangchenyuan/QuietQuote/releases)

进入发布页面后，在 **Assets** 中选择 Windows ZIP 压缩包。macOS 版为实验性预览，状态说明见下方。

# QuietQuote

这个文件夹可直接提交到GitHub，见[最简上传步骤](UPLOAD.zh-CN.md)。Windows版已本地验证；新增[macOS原生预览源码](macos/README.md)及Apple Silicon/Intel自动构建，**尚未在Mac上编译或实测**。请先确认Mac CI成功，再进行真机验收。

一个轻量的 Windows 悬浮行情胶囊：支持 OKX 与 Binance 的现货／永续行情、1–8 个独立悬浮窗口，支持七段数码管数字、自动明暗配色、毛玻璃和中英文界面。无需 API 密钥。

[English](README.md) · [VS Code 更新与发布教程](UPLOAD.zh-CN.md)

| 浅色 | 深色 |
| --- | --- |
| ![浅色](docs/images/light.png) | ![深色](docs/images/dark.png) |

图片为示例价格，并非实时行情。

## 下载与使用

从 [**Releases 下载页面**](https://github.com/Fred-Huangchenyuan/QuietQuote/releases)的 **Assets** 下载 `QuietQuote-v1.4.0-windows.zip`，解压到可写文件夹，双击 `QuietQuote.exe`。升级前从托盘退出旧版。复制旧 `settings.json` 可保留偏好。

建议 Windows 10/11 + .NET Framework 4.8。无需安装、管理员权限或额外字体。当前版本未做代码签名。

- OKX 现货／永续、Binance 现货／USDⓈ-M 永续，全部使用公共 API，无需密钥。
- 并行窗口数量可设为 1–8；每个窗口独立选择平台、市场、币对并保存位置。
- 默认窗口仍为 OKX `KORU-USDT-SWAP`；新增窗口默认为 Binance BTC/USDT 现货。
- 币对及计价单位来自官方列表；不含交割合约及 Binance 币本位合约。
- 刷新间隔 1–3600 秒；每次请求完成后等待指定间隔。
- 扁胶囊／圆形可选，宽高、背景浓度、数字大小、置顶可调。
- 关闭毛玻璃后，可选自动适应背景、浅色或深色。
- 设置内切换中文／English，保存后菜单和提示同步切换。
- 左键拖动；右键或双击打开设置；`Ctrl+Alt+Q` 或双击托盘图标隐藏／恢复全部窗口。
- 断线和过期有提示，自动重试；切换币种会清空旧价并丢弃旧请求。

**多开设置：**右键 → 设置 → 行情窗口，先调整并行窗口数量，再用“正在配置的窗口”逐个选择窗口的平台、市场类型、币种和单位，最后保存。外观及刷新间隔统一应用于全部窗口；取消不会改变原配置。

显示的是最新成交价，不是标记价或指数价。计价单位选择对应的实际币对，不作汇率换算。

Binance 现货价格接口不含成交时间，悬停提示明确显示“接收时间”；不能据此判断最后一笔成交发生在何时。其他已支持接口使用交易所时间。断线保留旧价并标记，首次连接失败显示横线，不会偷偷改用另一平台的价格。

**Mac 预览源码仍为单窗口、仅 OKX 永续版本，尚未同步 v1.4 多市场功能，也未在此 Windows 环境编译或实测。**

## 本地构建

在仓库根目录运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\package.ps1
```

`package.ps1` 会重新构建并运行离线核心测试。程序在 `.build/app/`，发布 ZIP 和 SHA256 校验值在 `dist/`。

可选：`test.ps1 -Desktop` 打开测试窗口并检查绘制和背景采样，使用受控 HTTP 响应验证异步切换与错误处理；`test.ps1 -Live` 还会读取真实 OKX 行情。两者需要交互式 Windows 桌面。单独执行 `test.ps1 -MarketLive` 可验证四类官方目录及 BTC 实时行情；CI 仅运行离线核心测试。

## 隐私与兼容性

只请求所选平台的 OKX／Binance 公共行情接口；没有账户接入、交易、遥测或自动更新。使用系统代理。`settings.json` 与 `markets-平台-市场.json`（兼容读取旧 `markets.json`） 保存在程序旁边，并已从 Git 跟踪中忽略。

自动配色每秒在本机采样周围最多 16 个像素，只计算明暗，不保存截图、不上传屏幕内容。它不能精确知道浮窗正下方的内容；复杂背景可手动固定配色。

毛玻璃效果依赖 Windows 合成器。模糊请求已返回成功，但最终桌面合成效果未完整目视确认；不可用时使用半透明外观。不同 DPI、多显示器、远程桌面、所有币种均未穷尽实测。快捷键可能被占用，可使用托盘。

## 许可证

[MIT](LICENSE)。本项目独立开发，与 OKX、Binance 无隶属或官方背书关系。
