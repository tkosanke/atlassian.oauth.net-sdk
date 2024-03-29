﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Atlassian.Jira.Remote
{
    internal class ProjectVersionService : IProjectVersionService
    {
        private readonly Jira _jira;

        public ProjectVersionService(Jira jira)
        {
            _jira = jira;
        }

        public async Task<IEnumerable<ProjectVersion>> GetVersionsAsync(string projectKey, CancellationToken token = default(CancellationToken))
        {
            var cache = _jira.Cache;

            if (!cache.Versions.Values.Any(v => String.Equals(v.ProjectKey, projectKey)))
            {
                var resource = String.Format("rest/api/latest/project/{0}/versions", projectKey);
                var remoteVersions = await _jira.RestClient.ExecuteRequestAsync<RemoteVersion[]>(Method.GET, resource, null, token).ConfigureAwait(false);
                var versions = remoteVersions.Select(remoteVersion =>
                {
                    remoteVersion.ProjectKey = projectKey;
                    return new ProjectVersion(_jira, remoteVersion);
                });
                cache.Versions.TryAdd(versions);
                return versions;
            }
            else
            {
                return cache.Versions.Values.Where(v => String.Equals(v.ProjectKey, projectKey));
            }
        }

        public async Task<IPagedQueryResult<ProjectVersion>> GetPagedVersionsAsync(string projectKey, int startAt = 0, int maxResults = 50, CancellationToken token = default(CancellationToken))
        {
            var settings = _jira.RestClient.Settings.JsonSerializerSettings;
            var resource = String.Format("rest/api/latest/project/{0}/version", projectKey);
            var queryParameters = new Dictionary<string, string>();
            queryParameters.Add("startAt", $"{startAt}");
            queryParameters.Add("maxResults", $"{maxResults}");

            var result = await _jira.RestClient.ExecuteRequestAsync(Method.GET, resource, null, token).ConfigureAwait(false);
            var versions = result["values"]
                .Cast<JObject>()
                .Select(versionJson =>
                {
                    var remoteVersion = JsonConvert.DeserializeObject<RemoteVersion>(versionJson.ToString(), settings);
                    remoteVersion.ProjectKey = projectKey;
                    return new ProjectVersion(_jira, remoteVersion);
                });

            return PagedQueryResult<ProjectVersion>.FromJson((JObject)result, versions);
        }

        public async Task<ProjectVersion> CreateVersionAsync(ProjectVersionCreationInfo projectVersion, CancellationToken token = default(CancellationToken))
        {
            var settings = _jira.RestClient.Settings.JsonSerializerSettings;
            var serializer = JsonSerializer.Create(settings);
            var resource = "/rest/api/latest/version";
            var requestBody = JToken.FromObject(projectVersion, serializer);
            var remoteVersion = await _jira.RestClient.ExecuteRequestAsync<RemoteVersion>(Method.POST, resource, requestBody, token).ConfigureAwait(false);
            remoteVersion.ProjectKey = projectVersion.ProjectKey;
            var version = new ProjectVersion(_jira, remoteVersion);

            _jira.Cache.Versions.TryAdd(version);

            return version;
        }

        public async Task DeleteVersionAsync(string versionId, string moveFixIssuesTo = null, string moveAffectedIssuesTo = null, CancellationToken token = default(CancellationToken))
        {
            var resource = String.Format("/rest/api/latest/version/{0}", versionId);
            var queryParameters = new Dictionary<string, string>();
            queryParameters.Add("moveFixIssuesTo", String.Format("{0}", String.IsNullOrEmpty(moveFixIssuesTo) ? null : Uri.EscapeDataString(moveFixIssuesTo)));
            queryParameters.Add("moveAffectedIssuesTo", String.Format("{0}", String.IsNullOrEmpty(moveAffectedIssuesTo) ? null : Uri.EscapeDataString(moveAffectedIssuesTo)));

            await _jira.RestClient.ExecuteRequestAsync(Method.DELETE, resource, null, token).ConfigureAwait(false);

            _jira.Cache.Versions.TryRemove(versionId);
        }

        public async Task<ProjectVersion> GetVersionAsync(string versionId, CancellationToken token = default(CancellationToken))
        {
            var resource = String.Format("rest/api/latest/version/{0}", versionId);
            var remoteVersion = await _jira.RestClient.ExecuteRequestAsync<RemoteVersion>(Method.GET, resource, null, token).ConfigureAwait(false);

            return new ProjectVersion(_jira, remoteVersion);
        }

        public async Task<ProjectVersion> UpdateVersionAsync(ProjectVersion version, CancellationToken token = default(CancellationToken))
        {
            var resource = String.Format("rest/api/latest/version/{0}", version.Id);
            var serializerSettings = _jira.RestClient.Settings.JsonSerializerSettings;
            var versionJson = JsonConvert.SerializeObject(version.RemoteVersion, serializerSettings);
            var remoteVersion = await _jira.RestClient.ExecuteRequestAsync<RemoteVersion>(Method.PUT, resource, versionJson, token).ConfigureAwait(false);

            return new ProjectVersion(_jira, remoteVersion);
        }
    }
}
