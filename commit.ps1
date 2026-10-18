# 发布仓库手动提交脚本。
#
# 职责只有一件：把「上次提交之后到底改了哪些文件」摊开给你看，等你亲笔写备注，确认后才提交。
# 它不会自己跑，也不会自己 push —— 仓库历史不再被 sync 时间戳刷屏。
#
# 用法：双击同目录的 commit.bat，或命令行运行本脚本。
# 参数：
#   -ListAll   变更很多时不再折叠预览，直接列出全部文件
#   -NoPush    提交后不询问推送（默认会问，默认不推）
#
# 完整流程：先在 MyWutheringLauncher 跑 tools\Remove-Comments.ps1 导出，再回来运行本脚本。

param(
    [switch]$ListAll,
    [switch]$NoPush,
    [int]$Preview = 25
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8   # git 输出按 UTF-8 解码，否则中文路径/备注会乱码
[Console]::InputEncoding  = [System.Text.Encoding]::UTF8

$Root = $PSScriptRoot
Push-Location $Root
try {
    # ---------- 0. 环境校验 ----------
    git rev-parse --is-inside-work-tree 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "当前目录不是 git 仓库：$Root" }

    $branch = (git rev-parse --abbrev-ref HEAD).Trim()
    $hasHead = $false
    git rev-parse --verify HEAD 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) { $hasHead = $true }

    Write-Host ""
    Write-Host "════════════════════════════════════════════════════════" -ForegroundColor DarkCyan
    Write-Host " Arietta 发布仓库 · 手动提交" -ForegroundColor Cyan
    Write-Host "════════════════════════════════════════════════════════" -ForegroundColor DarkCyan
    Write-Host " 目录     $Root"
    Write-Host " 分支     $branch"
    if ($hasHead) {
        $sha  = (git log -1 --pretty=%h).Trim()
        $sub  = (git log -1 --pretty=%s).Trim()
        $when = (git log -1 --pretty=%cr).Trim()
        Write-Host " 上次提交 $sha  $sub  （$when）"
    }
    else { Write-Host " 上次提交 （空仓库，本次为首提交）" -ForegroundColor Yellow }

    # ---------- 1. 清掉 filter-repo 残留 ----------
    # git filter-repo 跑完会在仓库根留 .git-rewrite/，它不该进版本库，否则每次都被当成新增文件
    if (Test-Path '.git-rewrite') {
        Write-Host ""
        Write-Host "发现 git filter-repo 残留目录 .git-rewrite/（不该提交）" -ForegroundColor Yellow
        $a = (Read-Host '  删除它？[Y/n]').Trim()
        if ($a -eq '' -or $a -eq 'y' -or $a -eq 'Y') {
            Remove-Item '.git-rewrite' -Recurse -Force
            Write-Host '  已删除' -ForegroundColor Green
        }
        else { Write-Host '  保留（它会被一并提交）' -ForegroundColor DarkYellow }
    }

    # ---------- 2. 收集真实增量 ----------
    # 先 add 到暂存区，未跟踪的新文件才会出现在 diff 里；确认前若取消会 git reset 还原。
    # 关掉 autocrlf：发布目录里全是脚本生成的文件，不让 git 再动换行，避免假 diff 和一屏 CRLF 警告
    git -c core.autocrlf=false -c core.safecrlf=false add -A | Out-Null

    $names = @(git -c core.quotePath=false diff --cached --name-status) | Where-Object { $_.Trim() -ne '' }
    if ($names.Count -eq 0) {
        Write-Host ""
        Write-Host "工作区与上次提交完全一致，没有可提交的内容。" -ForegroundColor Yellow
        Pop-Location
        exit 0
    }

    # numstat：每个文件的 +/- 行数；二进制文件是 -  -
    $num = @{}
    foreach ($line in (git -c core.quotePath=false diff --cached --numstat)) {
        if (-not $line.Trim()) { continue }
        $p = $line -split "`t"
        if ($p.Count -lt 3) { continue }
        $num[$p[2]] = @($p[0], $p[1])
    }

    $rows = @()
    foreach ($line in $names) {
        $p = $line -split "`t"
        $st = $p[0]
        $path = if ($p.Count -gt 2) { $p[2] } else { $p[1] }   # 重命名：取新路径
        switch -Regex ($st) {
            '^A' { $kind = '新增' }
            '^M' { $kind = '修改' }
            '^D' { $kind = '删除' }
            '^R' { $kind = '改名' }
            '^T' { $kind = '类型' }
            default { $kind = '其他' }
        }
        $statTxt = ''
        if ($num.ContainsKey($path)) {
            $a = $num[$path][0]; $d = $num[$path][1]
            if ($a -eq '-' -or $d -eq '-') { $statTxt = '二进制' }
            else { $statTxt = "+$a -$d" }
        }
        $rows += [pscustomobject]@{ Kind = $kind; Path = ($path -replace '\\', '/'); Stat = $statTxt }
    }

    $addSum = 0; $delSum = 0
    foreach ($k in $num.Keys) {
        if ($num[$k][0] -match '^\d+$') { $addSum += [int]$num[$k][0] }
        if ($num[$k][1] -match '^\d+$') { $delSum += [int]$num[$k][1] }
    }

    $nAdd = @($rows | Where-Object { $_.Kind -eq '新增' }).Count
    $nMod = @($rows | Where-Object { $_.Kind -eq '修改' }).Count
    $nDel = @($rows | Where-Object { $_.Kind -eq '删除' }).Count
    $nRen = @($rows | Where-Object { $_.Kind -eq '改名' }).Count

    # ---------- 3. 摊开给你看 ----------
    Write-Host ""
    Write-Host "────────────────────────────────────────────────────────" -ForegroundColor DarkGray
    $sum = "新增 $nAdd  ·  修改 $nMod  ·  删除 $nDel"
    if ($nRen) { $sum += "  ·  改名 $nRen" }
    Write-Host " 本次变更  $sum" -ForegroundColor Cyan
    Write-Host " 合计      $($rows.Count) 个文件，+$addSum -$delSum 行" -ForegroundColor Cyan

    # 按目录聚合，一眼看出动的是哪几块
    $dirs = @{}
    foreach ($r in $rows) {
        $d = [IO.Path]::GetDirectoryName($r.Path) -replace '\\', '/'
        if (-not $d) { $d = '(root)' }   # 用等宽 ASCII，中文括号在 PadRight 下会把列撑歪
        if ($dirs.ContainsKey($d)) { $dirs[$d]++ } else { $dirs[$d] = 1 }
    }
    Write-Host ""
    # 列宽按本批实际最长路径算，长路径从左侧截断（…开头），保证一行放得下不折行
    function Fit([string]$s, [int]$w) {
        if ($s.Length -le $w) { return $s.PadRight($w) }
        return ('…' + $s.Substring($s.Length - ($w - 1)))
    }
    $dirList = @($dirs.GetEnumerator() | Sort-Object -Property Value -Descending | Select-Object -First 12)
    $dw = @($dirList | ForEach-Object { $_.Key.Length } | Measure-Object -Maximum).Maximum
    if ($dw -gt 56) { $dw = 56 }
    Write-Host " 涉及目录：" -ForegroundColor DarkGray
    foreach ($d in $dirList) {
        Write-Host ("   " + (Fit $d.Key $dw) + " " + $d.Value.ToString().PadLeft(4) + " 个")
    }
    if ($dirs.Count -gt 12) { Write-Host "   …还有 $($dirs.Count - 12) 个目录" }

    Write-Host ""
    Write-Host " 文件明细：" -ForegroundColor DarkGray
    $shown = $rows
    if (-not $ListAll -and $rows.Count -gt $Preview) {
        $shown = $rows | Select-Object -First $Preview
    }
    $fw = @($shown | ForEach-Object { $_.Path.Length } | Measure-Object -Maximum).Maximum
    if ($fw -gt 56) { $fw = 56 }
    foreach ($r in $shown) {
        $mark = switch ($r.Kind) { '新增' { '+' } '修改' { '~' } '删除' { '-' } '改名' { '>' } default { '?' } }
        $clr  = switch ($r.Kind) { '新增' { 'Green' } '修改' { 'Yellow' } '删除' { 'Red' } '改名' { 'Cyan' } default { 'Gray' } }
        # 整行一次输出：Write-Host -NoNewline 跨多次调用在重定向时会被切成多行
        Write-Host ("   " + $mark + " " + (Fit $r.Path $fw) + "  " + $r.Stat) -ForegroundColor $clr
    }
    if ($shown.Count -lt $rows.Count) {
        Write-Host "   …还有 $($rows.Count - $shown.Count) 个（加 -ListAll 看全部）" -ForegroundColor DarkGray
    }

    if ($rows.Count -gt 150) {
        Write-Host ""
        Write-Host " ⚠ 变更量异常大（$($rows.Count) 个文件），多半是剥离脚本规则变动导致的全量重写。" -ForegroundColor Yellow
        Write-Host "   建议先抽查几个文件的 diff 再决定，避免又是一次「全量提交」。" -ForegroundColor Yellow
    }

    # ---------- 4. 备注由你亲笔写 ----------
    Write-Host ""
    Write-Host "────────────────────────────────────────────────────────" -ForegroundColor DarkGray
    # 一路回车到底 = 用默认的 sync <时间> 提交（和以前的自动同步一个味道）；想放弃就输 q
    $defaultMsg = 'sync ' + (Get-Date -Format 'yyyy-MM-dd HH:mm')
    Write-Host " 提交备注（直接回车 = $defaultMsg ；写自己的更有用；输入 q 放弃）" -ForegroundColor Cyan
    $subject = (Read-Host '  >').Trim()
    if ($subject -eq 'q' -or $subject -eq 'Q') {
        git reset | Out-Null
        Write-Host " 已放弃，暂存区已还原" -ForegroundColor Yellow
        Pop-Location
        exit 0
    }
    if ($subject -eq '') { $subject = $defaultMsg }

    Write-Host " 补充说明（可多行，直接回车结束；没有就直接回车）" -ForegroundColor DarkGray
    $body = @()
    $more = (Read-Host '  >').Trim()
    while ($more -ne '') {
        $body += $more
        $more = (Read-Host '  >').Trim()
    }

    $msg = $subject
    if ($body.Count) { $msg = $subject + "`r`n`r`n" + ($body -join "`r`n") }
    $msgFile = Join-Path $env:TEMP 'arietta-release-msg.txt'
    [IO.File]::WriteAllText($msgFile, $msg, (New-Object System.Text.UTF8Encoding $false))

    # ---------- 5. 最后一道确认 ----------
    Write-Host ""
    Write-Host " 即将提交 $($rows.Count) 个文件到 $branch" -ForegroundColor Cyan
    Write-Host " 备注：$subject" -ForegroundColor White
    $ok = (Read-Host ' 确认提交？[Y/n]').Trim()
    if ($ok -ne '' -and $ok -ne 'y' -and $ok -ne 'Y') {
        git reset | Out-Null
        Write-Host " 已放弃，暂存区已还原" -ForegroundColor Yellow
        Pop-Location
        exit 0
    }

    git -c i18n.commitEncoding=utf-8 commit -F $msgFile
    if ($LASTEXITCODE -ne 0) {
        Write-Host " 提交失败，暂存区保持原样" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    Remove-Item $msgFile -Force -ErrorAction SilentlyContinue
    Write-Host " 已提交：$(git log -1 --pretty=%h)  $subject" -ForegroundColor Green

    # ---------- 6. 推送单独决定，默认不推 ----------
    if (-not $NoPush) {
        git remote get-url origin 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) {
            $push = (Read-Host " 推送到 origin/$branch ？[y/N]").Trim()
            if ($push -eq 'y' -or $push -eq 'Y') {
                git push -u origin $branch
                if ($LASTEXITCODE -eq 0) { Write-Host ' 已推送' -ForegroundColor Green }
                else { Write-Host ' 推送失败：检查远程地址/凭据（首次推送会弹登录窗口）' -ForegroundColor Yellow }
            }
            else { Write-Host ' 未推送（本地已提交，随时可 git push）' -ForegroundColor DarkGray }
        }
        else { Write-Host ' 未配置远程，跳过推送' -ForegroundColor DarkGray }
    }
}
finally {
    if ((Get-Location).Path -ne (Get-Item $Root).FullName) { Pop-Location }
}

Write-Host ""
