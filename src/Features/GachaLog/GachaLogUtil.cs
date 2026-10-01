using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Arietta.Features.GachaLog;





public static partial class GachaLogUtil
{
    [GeneratedRegex(@"https.*/aki/gacha/index.html#/record[^""'\s]+")]
    private static partial Regex UrlPattern();

    [GeneratedRegex(@"\\u([0-9A-Fa-f]{4})")]
    private static partial Regex UnicodeEscapePattern();

    
    
    
    
    public static string? GetLogFileUrl(string logFile)
    {
        string? decrypted = DecryptLog(logFile);
        if (decrypted is null)
        {
            return null;
        }

        string? last = null;
        foreach (Match match in UrlPattern().Matches(decrypted))
        {
            last = match.Value;
        }
        return last is not null && UnicodeEscapePattern().IsMatch(last) ? DecodeUnicodeEscapes(last) : last;
    }


    
    
    
    
    
    public static string? DecryptLog(string logFile)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                using var stream = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var buffer = new MemoryStream();
                stream.CopyTo(buffer);
                byte[] bytes = buffer.ToArray();
                for (int i = 0; i < bytes.Length; i++)
                {
                    int b = bytes[i] & 0xFF;
                    bytes[i] = (byte)(((b & 0x0F) % 2) == 1 ? b ^ 0xA5 : b ^ 0xEF);
                }
                return Encoding.UTF8.GetString(bytes);
            }
            catch (IOException) when (attempt < 2)
            {
                Thread.Sleep(200);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }


    
    
    
    public static GachaParams GetParamFromUrl(string url)
    {
        var parameters = new GachaParams();
        int queryIndex = url.IndexOf('?');
        if (queryIndex < 0)
        {
            return parameters;
        }

        foreach (string pair in url[(queryIndex + 1)..].Split('&'))
        {
            string[] kv = pair.Split('=', 2);
            if (kv.Length < 2)
            {
                continue;
            }
            switch (kv[0])
            {
                case "player_id": parameters.PlayerId = kv[1]; break;
                case "record_id": parameters.RecordId = kv[1]; break;
                case "resources_id": parameters.CardPoolId = kv[1]; break;
                case "gacha_type": parameters.CardPoolType = kv[1]; break;
                case "svr_id": parameters.ServerId = kv[1]; break;
                case "lang": parameters.LanguageCode = kv[1]; break;
            }
        }
        return parameters;
    }


    private static string DecodeUnicodeEscapes(string s)
    {
        return UnicodeEscapePattern().Replace(s, m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
    }
}
