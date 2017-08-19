﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Windows.Web.Http;
using System.Linq;

namespace Microsoft.Toolkit.Uwp.SampleApp.Data
{
    public static class GitHub
    {
        private const string _root = "https://api.github.com";
        private const string _repoName = "UWPCommunityToolkit";
        private const string _repoOwner = "Microsoft";

        private static List<GitHubRelease> _releases;

        public static async Task<List<GitHubRelease>> GetPublishedReleases()
        {
            if (_releases == null)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.TryAppendWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0) like Gecko");

                        var uri = $"{_root}/repos/{_repoOwner}/{_repoName}/releases";
                        var result = await client.GetStringAsync(new Uri(uri));
                        _releases = JsonConvert.DeserializeObject<List<GitHubRelease>>(result).Take(5).ToList();
                    }
                }
                catch (Exception ex)
                {
                }
            }

            return _releases;
        }
    }
}
