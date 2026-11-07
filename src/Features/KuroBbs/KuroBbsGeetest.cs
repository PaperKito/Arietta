using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Arietta.Features.KuroBbs;

internal sealed class KuroBbsGeetest
{

    public const string CaptchaId = "ec4aa4174277d822d73f2442a165a2cd";

    private const string Host = "arietta.captcha";
    private const string PageName = "geetest.html";

    private readonly WebView2 _web;
    private TaskCompletionSource<string?>? _pending;
    private bool _prepared;

    public KuroBbsGeetest(WebView2 web) => _web = web;

    public async Task<string?> WaitForValidateAsync(CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending = tcs;
        using var reg = ct.Register(() => tcs.TrySetResult(null));
        try
        {

            Arietta.Helpers.WebviewHost.UseDataFolder();
            await _web.EnsureCoreWebView2Async();
            var core = _web.CoreWebView2;
            if (core is null)
            {
                return null;
            }
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.AreDefaultContextMenusEnabled = false;

            if (!_prepared)
            {
                string dir = Path.Combine(AppConfig.LocalFolder, "webview", "captcha");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, PageName), CaptchaHtml, new UTF8Encoding(false));
                core.SetVirtualHostNameToFolderMapping(Host, dir, CoreWebView2HostResourceAccessKind.DenyCors);
                core.WebMessageReceived -= OnWebMessageReceived;
                core.WebMessageReceived += OnWebMessageReceived;
                _prepared = true;
            }
            core.Navigate($"https://{Host}/{PageName}?t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
            return await tcs.Task;
        }
        catch (Exception ex)
        {
            AppConfig.GetLogger<KuroBbsGeetest>().LogWarning("KuroBBS geetest init failed: {Message}", ex.Message);
            return null;
        }
        finally
        {
            _pending = null;
        }
    }

    private void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        try
        {
            string raw = args.TryGetWebMessageAsString();
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            string type = root.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
            string data = root.TryGetProperty("data", out var d) ? d.GetString() ?? "" : "";
            if (type == "success" && data.Length > 0)
            {
                _pending?.TrySetResult(data);
            }
            else if (type == "error" || type == "close")
            {
                _pending?.TrySetResult(null);
            }
        }
        catch { }
    }

    private const string CaptchaHtml = @"<!DOCTYPE html>
<html lang='zh-CN'>
<head>
<meta charset='utf-8'/>
<meta name='viewport' content='width=device-width,initial-scale=1'/>
<title>Security verification</title>
<style>
  html,body{margin:0;padding:0;width:100%;height:100%;overflow:hidden;font-family:'Microsoft YaHei',sans-serif;background:transparent}
  #tip{box-sizing:border-box;padding:10px;font-size:12px;color:#888;text-align:center;cursor:pointer}
  #tip.ok{color:#0a7b52}
  #tip.err{color:#c0392b}
</style>
<script src='https://static.geetest.com/v4/gt4.js'></script>
</head>
<body>
<div id='tip'>正在加载安全验证…</div>
<script>
(function(){
  var CAPTCHA_ID = 'ec4aa4174277d822d73f2442a165a2cd';
  var tip = document.getElementById('tip');
  var captchaObj = null;

  function post(type, data){
    try {
      if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(JSON.stringify({type:type, data:data || ''}));
      }
    } catch(e) {}
  }
  function setTip(text, cls){ tip.textContent = text; tip.className = cls || ''; }
  function show(){ if (captchaObj) { try { captchaObj.showBox(); } catch(e){} } }
  function fail(msg){ setTip(msg, 'err'); post('error', msg); }

  tip.onclick = show;

  if (!window.initGeetest4) {
    fail('安全验证加载失败，请检查网络后重试');
    return;
  }

  try {
    window.initGeetest4({
      captchaId: CAPTCHA_ID,
      product: 'bind',
      language: 'zho',
      protocol: 'https://'
    }, function(captcha){
      captchaObj = captcha;
      captcha.onReady(function(){
        setTip('请完成滑块验证（点此可重新弹出）', '');
        post('ready', '');
        show();
      }).onSuccess(function(){
        var r = captcha.getValidate();
        if (!r) { fail('未拿到验证结果，请重试'); return; }
        r.captcha_id = CAPTCHA_ID;
        setTip('验证通过，正在发送短信…', 'ok');
        post('success', JSON.stringify(r));
      }).onError(function(){
        setTip('验证出错，点此重试', 'err');
        try { captcha.reset(); } catch(e){}
      }).onClose(function(){
        setTip('验证已关闭，点此重新验证', '');
      });
    });
  } catch(e) {
    fail('安全验证初始化失败');
  }
})();
</script>
</body>
</html>";
}
