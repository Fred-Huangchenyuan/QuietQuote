# 用 VS Code 更新源码并发布 v1.4.0

已有 GitHub 仓库时，不需要再次 git init、新建仓库或重复添加 origin，也不需要安装 GitHub CLI。
下面每个命令按顺序执行；某一步报错就先停在该步。

## 1. 打开仓库

VS Code → File → Open Folder，选择含 .git、README.md、src、scripts 的现有仓库文件夹。
Terminal → New Terminal，使用 PowerShell，执行：

```powershell
git status
git branch --show-current
git remote -v
```

本次交付使用的分支是 main，origin 为 https://github.com/Fred-Huangchenyuan/QuietQuote。
如果看到 not a git repository，说明终端目录不对；切回现有仓库，别在上级目录初始化新仓库。

## 2. 本地试用与打包

本次已提供测试过的 v1.4.0 Windows ZIP，可以直接上传。自行修改源码后应重新生成：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\package.ps1
```

成功后 dist 内有 QuietQuote-v1.4.0-windows.zip 和 SHA256SUMS.txt。
先从系统托盘退出旧版，解压新 ZIP 到一个可写文件夹，运行 QuietQuote.exe。
需要保留旧设置时，在启动新版前把旧 settings.json 复制到新版 EXE 旁。

右键浮窗 → 设置 → 行情窗口：

1. 将“并行窗口数量”设为需要的数量，例如 3。
2. “正在配置的窗口”选窗口 1，设置 OKX / 永续 / 原币种。
3. 选窗口 2，设置 Binance / 现货 / BTC / USDT。
4. 选窗口 3，设置 Binance / 永续 / ETH / USDT。
5. 外观与刷新页调整共享设置，然后保存。

可选验证：scripts/test.ps1 -Desktop 做本地桌面测试；scripts/test.ps1 -MarketLive 联网检查四类公共行情。

## 3. 提交并推送源码

回到 VS Code 的仓库终端，逐条执行：

```powershell
git status --short
git add .
git diff --cached --stat
git commit -m "feat: add Binance, spot markets and multiple windows"
git push origin main
```

顺序是 **add → commit → push**。add 暂存改动，commit 保存本地版本，push 上传到 GitHub。
.gitignore 已排除 EXE、ZIP、.build、dist、个人设置及行情缓存；它们不会通过上述 git add . 被加入源码提交。
检查暂存清单，确认没有夹带自己另外放入仓库的私人文件。

首次认证按 Git/VS Code 提示在浏览器登录 GitHub。若提示 Author identity unknown，仅在当前仓库设置你的姓名和提交邮箱，再重新执行 commit：

```powershell
git config user.name "你的 GitHub 昵称"
git config user.email "你的 GitHub 提交邮箱"
```

若提示 non-fast-forward 或 fetch first：不要强推。先确保本地改动已提交，再运行：

```powershell
git pull --rebase origin main
git push origin main
```

如果 rebase 报冲突，停止后续步骤，先解决冲突；不要覆盖远端内容。

## 4. 查看 GitHub 自动构建

打开仓库 → **Actions** → **Windows build**，选择本次 commit，等待绿色勾。
Actions 的 Artifact 是构建产物；成功构建不会自动创建公开 Release。
Mac 工作流单独处理；本次 Windows 新功能尚未移植到 Mac。

## 5. 创建真正可下载的 Release

打开 [创建 Release](https://github.com/Fred-Huangchenyuan/QuietQuote/releases/new)：

1. Choose a tag / Select tag：输入 v1.4.0，选择创建这个新标签。
2. Target：选择 main，确认此时已含刚才推送的新 commit。
3. Release title：填 QuietQuote v1.4.0。
4. 描述：复制 docs/releases/v1.4.0.md 的正文。
5. 在附件区域上传 dist/QuietQuote-v1.4.0-windows.zip 和 dist/SHA256SUMS.txt。
6. 等两个文件全部上传完成；这次是 Windows 正式版，不勾 Set as a pre-release。
7. 如果显示 Set as the latest release，将它选中；最后点击 Publish release。

如果 v1.4.0 已存在，先检查对应 Release 是否就是这次版本；不要删除或覆盖旧标签来强行发布不同内容。

## 6. 验证访客下载

打开仓库 README，点击第一行下载链接。应看到 v1.4.0，在 Assets 中看到 Windows ZIP。
用无痕窗口再下载一次，确认普通访客可访问；解压后应有 QuietQuote.exe。
GitHub 自动附带的 Source code (zip) 是源码，不是 Windows 应用包。

以后发布新版本：更新 version.txt、更新日志和 release notes，重新打包，再用新版本号重复步骤 3–6。

官方参考：[提交本地代码](https://docs.github.com/en/migrations/importing-source-code/using-the-command-line-to-import-source-code/adding-locally-hosted-code-to-github)、[管理 Releases](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository)。
