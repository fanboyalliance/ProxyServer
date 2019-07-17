using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;

namespace Proxy.Middleware
{
    public class ProxyMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly HttpClient _client = new HttpClient();
        private readonly char _sign = '™';
        
        public ProxyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            using (var responseMessage = await _client.GetAsync($"http://{StaticKeeper.MainUri}{context.Request.Path.ToString()}"))
            {
                context.Response.StatusCode = (int)responseMessage.StatusCode;
                context.Response.ContentType = "text/html";
                var response = await responseMessage.Content.ReadAsStringAsync();

                try
                {
                    response = ChangeUri(response);
                    response = TurnOffHabrAuthorization(response);
                    response = ChangeSixLetterWords(response);
                }
                catch (Exception e)
                {
                    throw new Exception();
                }
                
                var byteArray = Encoding.UTF8.GetBytes(response);
                var stream = new MemoryStream(byteArray);
                await stream.CopyToAsync(context.Response.Body);
            }
            
            return;
            await _next(context);
        }
        
        private string ChangeUri(string html)
        {
            var uriRegex = new Regex($@"(https:\/\/{StaticKeeper.MainUri}\/)");
            html = uriRegex.Replace(html, $"http://{StaticKeeper.Host}:{StaticKeeper.Port}/");
            
            return html;
        }
        
        private string TurnOffHabrAuthorization(string html)
        {
            var index = html.IndexOf("login.js");
            if (index > 0)
            {
                html = html.Insert(index, $"{_sign}");
            }

            return html;
        }

        private string ChangeSixLetterWords(string html)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            var htmlBody = htmlDoc.DocumentNode.SelectSingleNode("//body");

            var foundWords = FindSixLetterWords(htmlBody);
            
            if (foundWords.Count > 0)
            {
                htmlBody.InnerHtml = AddSignToFoundWords(foundWords,  htmlBody.InnerHtml);
            }
            else
            {
                return html;
            }
            
            using (var writer = new StringWriter())
            {
                htmlDoc.Save(writer);
                html = writer.ToString();
                var bodyRegex = new Regex(@"<\s*body[^>]*>([\s\S]*?)<\/body>");
                html = bodyRegex.Replace(html, htmlBody.InnerHtml);
            }
            
            return html;
        }
        
        private List<string> FindSixLetterWords(HtmlNode body)
        {
            var sixLetterReg = new Regex(@"(?<=(?:\s|\G|\A|\>))[a-zA-Zа-яА-Я]{6}(?=(?:\s|\Z|\.|\?|\!|\:))");
            var foundWords = new List<string>();
            
            foreach (var child in body.ChildNodes)
            {
                var sb = new StringBuilder();
                if (child.Name.Equals("div"))
                {
                    var matches = sixLetterReg.Matches(child.InnerText);
                    sb.Append(child.InnerText);
                    for (int i = 0; i < matches.Count; i++)
                    {
                        var word = matches[i].Value.Trim();
                        
                        if (!sb.ToString().Contains(word + _sign))
                        {
                            foundWords.Add(word);
                        }
                    }
                }
            }

            return foundWords;
        }

        private string AddSignToFoundWords(List<string> foundWords, string html)
        {
            var sb = new StringBuilder(html);
            for (int i = 0; i < foundWords.Count; i++)
            {
                var word = foundWords[i];
                for (int index = 0; ; index += word.Length) 
                {
                    var changedHtml = sb.ToString();
                    index = changedHtml.IndexOf(word, index);
                    if (index == -1)
                    {
                        break;
                    }
                    
                    if (CheckWordBoundary(changedHtml, index, word.Length) &&
                        (changedHtml[index + word.Length] != _sign))
                    {
                        sb = sb.Insert(index + word.Length, _sign);
                    }
                }
            }

            return sb.ToString();
        }

        private bool CheckWordBoundary(string text, int index, int wordLength)
        {
            if (!char.IsLetterOrDigit(text[index + wordLength]))
            {
                if (index == 0)
                {
                    return true;
                }

                if (!char.IsLetterOrDigit(text[index-1]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}