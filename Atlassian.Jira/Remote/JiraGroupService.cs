using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Atlassian.Jira.Remote
{
    internal class JiraGroupService : IJiraGroupService
    {
        private readonly Jira _jira;

        public JiraGroupService(Jira jira)
        {
            _jira = jira;
        }

        public Task AddUserAsync(string groupname, string username, CancellationToken token = default(CancellationToken))
        {
            var resource = "rest/api/latest/group/user";
            var queryParameters = new Dictionary<string, string>();
            queryParameters.Add("groupname", Uri.EscapeDataString(groupname));
            queryParameters.Add("name", username);
            return _jira.RestClient.ExecuteRequestAsync(Method.POST, resource, queryParameters, token);
        }

        public Task CreateGroupAsync(string groupName, CancellationToken token = default(CancellationToken))
        {
            var resource = "rest/api/latest/group";
            var requestBody = JToken.FromObject(new { name = groupName });

            return _jira.RestClient.ExecuteRequestAsync(Method.POST, resource, requestBody, token);
        }

        public Task DeleteGroupAsync(string groupName, string swapGroupName = null, CancellationToken token = default(CancellationToken))
        {
            var resource = "rest/api/latest/group";
            var queryParameters = new Dictionary<string, string>();
            queryParameters.Add("groupname", Uri.EscapeDataString(groupName));

            if (!String.IsNullOrEmpty(swapGroupName))
            {
                queryParameters.Add("swapGroup", Uri.EscapeDataString(swapGroupName));
            }

            return _jira.RestClient.ExecuteRequestAsync(Method.DELETE, resource, queryParameters, token);
        }

        public async Task<IPagedQueryResult<JiraUser>> GetUsersAsync(string groupname, bool includeInactiveUsers = false, int maxResults = 50, int startAt = 0, CancellationToken token = default(CancellationToken))
        {
            var resource = "rest/api/latest/group/member";
            var queryParameters = new Dictionary<string, string>();
            queryParameters.Add("groupname", Uri.EscapeDataString(groupname));
            queryParameters.Add("includeInactiveUsers", $"{includeInactiveUsers}");
            queryParameters.Add("startAt", $"{startAt}");
            queryParameters.Add("maxResults", $"{maxResults}");

            var response = await _jira.RestClient.ExecuteRequestAsync(Method.GET, resource, queryParameters, token).ConfigureAwait(false);
            var serializerSetting = _jira.RestClient.Settings.JsonSerializerSettings;
            var users = response["values"]
                .Cast<JObject>()
                .Select(valuesJson => JsonConvert.DeserializeObject<JiraUser>(valuesJson.ToString(), serializerSetting));

            return PagedQueryResult<JiraUser>.FromJson((JObject)response, users);
        }

        public Task RemoveUserAsync(string groupname, string username, CancellationToken token = default(CancellationToken))
        {
            var resource = "rest/api/latest/group/user";
            var queryParameters = new Dictionary<string, string>();
            queryParameters.Add("groupname", Uri.EscapeDataString(groupname));
            queryParameters.Add("username", Uri.EscapeDataString(username));

            return _jira.RestClient.ExecuteRequestAsync(Method.DELETE, resource, queryParameters, token);

        }
    }
}
