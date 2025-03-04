﻿using Microsoft.Extensions.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MyBlog.Core
{
    public class InformationProvider : IInformationProvider
    {
        public const string GitHubApiRoot = "https://api.github.com";
        public const string Owner = "abbatepabloo";
        public const string BlogName = "myblog";

        private readonly HttpClient httpClient;

        public InformationProvider(HttpClient httpClient)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<GitHubUserInfo?> GetUserInfoAsync()
        {
            return await httpClient.GetFromJsonAsync<GitHubUserInfo>($"{GitHubApiRoot}/users/{Owner}");
        }

        public async Task<GitHubCommitInfo?> GetCommitInfoAsync(string path)
        {
            var url = $"{GitHubApiRoot}/repos/{Owner}/{BlogName}/commits?page=1&per_page=1&path={path}";
            var commit = await httpClient.GetFromJsonAsync<IEnumerable<GitHubCommitInfo>>(url);
            return commit?.FirstOrDefault();
        }

        public async Task<string?> GetMarkDownContentInHtmlFormat(string url, bool excerpt)
        {
            if (url is null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            var response = await httpClient.GetFromJsonAsync<GitHubFileInformation>(url);
            if (response == null || response.content == null) return null;

            var mkContent = Encoding.UTF8.GetString(Convert.FromBase64String(response.content));

            if (excerpt)
            {
                var firstLines = string.Join('\n', mkContent.Split('\n').Take(5)) + "...";
                return Markdig.Markdown.ToHtml(firstLines);
            }
            return Markdig.Markdown.ToHtml(mkContent);
        }

        public Task<GitHubSearchResult?> GetLastEntriesAsync()
        {
            return SearchAsync($"extension:md sort:author-date-desc", "page=0&per_page=10");
        }

        public Task<GitHubSearchResult?> GetEntriesAsync(int from, int count)
        {
            return SearchAsync($"extension:md sort:author-date-desc", $"page={from}&per_page={count}");
        }

        public Task<GitHubSearchResult?> SearchInFileContentAsync(string query, int from = 0, int count = int.MaxValue)
        {
            return SearchAsync($"{query} in:file extension:md sort:author-date-desc", $"page={from}&per_page={count}");
        }

        protected virtual async Task<GitHubSearchResult?> SearchAsync(string queryWithClauses, string? extraParams = null)
        {
            var url = $"{GitHubApiRoot}/search/code?q={queryWithClauses} user:{Owner} repo:{Owner}/{BlogName} path:/MyBlog/Entries";
            if (!string.IsNullOrEmpty(extraParams)) url += $"&{extraParams}";
            return await httpClient.GetFromJsonAsync<GitHubSearchResult>(url);
        }
    }
}
