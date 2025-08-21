using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace QuickJump.Providers
{
    public class AzureDevopsProvider : IItemsProvider
    {
        private const string organizationUrl = "https://dev.azure.com/Raboweb";
        private readonly TokenCredential tokenCredential;

        public AzureDevopsProvider(ITokenCredentialProvider tokenCredentialProvider)
        {
            tokenCredential = tokenCredentialProvider.GetCredential();
        }

        public string Name => nameof(AzureDevopsProvider);

        public bool LoadDataOnActivate => false;

        public async Task GetItems(Func<Item, Task> value, CancellationToken cancellationToken)
        {
            var token = await tokenCredential.GetTokenAsync(new TokenRequestContext(["499b84ac-1321-427f-aa17-267ca6975798/.default"]), default);
            var connection = new VssConnection(new Uri(organizationUrl), new VssBasicCredential(string.Empty, token.Token));

            var projectClient = await connection.GetClientAsync<ProjectHttpClient>();
            var gitClient = await connection.GetClientAsync<GitHttpClient>();
            var buildClient = await connection.GetClientAsync<BuildHttpClient>();

            var projects = await projectClient.GetProjects();
            foreach (var project in projects.Where(p => p.Name == "SDBI" || p.Name == "Tribe External Reporting"))
            {
                var repos = await gitClient.GetRepositoriesAsync(project.Id, null, cancellationToken);
                foreach (var repo in repos)
                {
                    var item = MapRepoToItem(repo);
                    await value(item);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                var pipelines = await buildClient.GetDefinitionsAsync(project.Id, cancellationToken: cancellationToken);
                foreach (var pipeline in pipelines)
                {
                    var item = MapPipelineToItem(pipeline);
                    await value(item);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        private Item MapRepoToItem(GitRepository resource)
        {
            return new Item
            {
                Id = resource.Id.ToString(),
                Name = resource.Name,
                Type = Types.Uri,
                Description = $"{resource.Name} repo",
                Path = resource.WebUrl,
                Category = Categories.AzureDevOps,
                Provider = Name,
                Icon = "azuredevops"
            };
        }

        private Item MapPipelineToItem(BuildDefinitionReference resource)
        {
            var link = resource.Links.Links["web"] is ReferenceLink webLink && webLink.Href != null 
                    ? webLink.Href
                    : $"https://dev.azure.com/raboweb/{resource.Project}/_build?definitionId={resource.Id}";

            return new Item
            {
                Id = resource.Id.ToString(),
                Name = resource.Name,
                Type = Types.Uri,
                Description = $"{resource.Name} pipeline",
                Path = link,
                Category = Categories.AzureDevOps,
                Provider = Name,
                Icon = "azuredevops"
            };
        }
    }
}
