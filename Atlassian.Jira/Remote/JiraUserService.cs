using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Atlassian.Jira.Remote
{
    internal class JiraUserService : IJiraUserService
    {
        private readonly Jira _jira;

        public JiraUserService(Jira jira)
        {
            _jira = jira;
        }

        public async Task<JiraUser> CreateUserAsync(JiraUserCreationInfo user, CancellationToken token = default(CancellationToken))
        {
            var resource = "rest/api/latest/user";
            var requestBody = JToken.FromObject(user);

            return await _jira.RestClient.ExecuteRequestAsync<JiraUser>(Method.POST, resource, requestBody, token).ConfigureAwait(false);
        }

        public Task DeleteUserAsync(string username, CancellationToken token = default(CancellationToken))
        {
            var resource = String.Format("rest/api/latest/user?username={0}", Uri.EscapeDataString(username));
            return _jira.RestClient.ExecuteRequestAsync(Method.DELETE, resource, null, token);
        }

        public Task<JiraUser> GetUserAsync(string username, CancellationToken token = default(CancellationToken))
        {
            var queryParameters = new Dictionary<string, string>();
            queryParameters.Add("username", Uri.EscapeDataString(username));
            return _jira.RestClient.ExecuteRequestAsync<JiraUser>(Method.GET, "rest/api/latest/user", queryParameters, null, token);
        }

        public Task<IEnumerable<JiraUser>> SearchUsersAsync(string query, JiraUserStatus userStatus = JiraUserStatus.Active, int maxResults = 50, int startAt = 0, CancellationToken token = default(CancellationToken))
        {
            var queryParameters = new Dictionary<string, string>();
            queryParameters.Add("username", Uri.EscapeDataString(query));
            queryParameters.Add("includeActive", userStatus.HasFlag(JiraUserStatus.Active).ToString());
            queryParameters.Add("includeInactive", userStatus.HasFlag(JiraUserStatus.Inactive).ToString());
            queryParameters.Add("startAt", startAt.ToString());
            queryParameters.Add("maxResults", maxResults.ToString());

            return _jira.RestClient.ExecuteRequestAsync<IEnumerable<JiraUser>>(Method.GET, "rest/api/latest/user/search", queryParameters, null, token);
        }
    }
}
