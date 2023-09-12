using System.Text;
using Azure.AISearch.WebApp.Models;
using Microsoft.SemanticKernel;

namespace Azure.AISearch.WebApp.Services;

// This is a simple, somewhat naive implementation of a custom orchestration service that uses Semantic Kernel to generate answers.
// For inspiration, see:
// https://github.com/Azure-Samples/azure-search-openai-demo-csharp/blob/feature/embeddingSearch/app/backend/Services/RetrieveThenReadApproachService.cs
// https://github.com/Azure-Samples/azure-search-openai-demo-csharp/blob/feature/embeddingSearch/app/backend/Extensions/SearchClientExtensions.cs

public class SemanticKernelSearchService : ISearchService
{
    private readonly AppSettings settings;
    private readonly AzureCognitiveSearchService azureCognitiveSearchService;
    private readonly IKernel kernel;

    public SemanticKernelSearchService(AppSettings settings, AzureCognitiveSearchService azureCognitiveSearchService)
    {
        ArgumentNullException.ThrowIfNull(settings.OpenAIGptDeployment);
        ArgumentNullException.ThrowIfNull(settings.OpenAIEndpoint);
        ArgumentNullException.ThrowIfNull(settings.OpenAIApiKey);
        this.settings = settings;
        this.azureCognitiveSearchService = azureCognitiveSearchService;
        this.kernel = Kernel.Builder
            .WithAzureChatCompletionService(this.settings.OpenAIGptDeployment, this.settings.OpenAIEndpoint, this.settings.OpenAIApiKey, true)
            .Build();
    }

    public bool CanHandle(SearchRequest request)
    {
        return request.Engine == EngineType.CustomOrchestration;
    }

    public async Task<SearchResponse> SearchAsync(SearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request.Query);
        var prompt = string.IsNullOrWhiteSpace(request.CustomOrchestrationPrompt) ? this.settings.GetDefaultCustomOrchestrationPrompt() : request.CustomOrchestrationPrompt;
        var function = this.kernel.CreateSemanticFunction(prompt,
            maxTokens: request.MaxTokens ?? Constants.Defaults.MaxTokens,
            temperature: request.Temperature ?? Constants.Defaults.Temperature,
            topP: request.TopP ?? Constants.Defaults.TopP,
            frequencyPenalty: request.FrequencyPenalty ?? Constants.Defaults.FrequencyPenalty,
            presencePenalty: request.PresencePenalty ?? Constants.Defaults.PresencePenalty,
            stopSequences: (request.StopSequences ?? Constants.Defaults.StopSequences).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        var response = new SearchResponse();
        var context = this.kernel.CreateNewContext();
        context.Variables["query"] = request.Query;

        // Query the search index for relevant data first, by passing through the original request
        // to the Azure Cognitive Search service.
        var azureCognitiveSearchResponse = await this.azureCognitiveSearchService.SearchAsync(request);

        // Copy the document results over, as these are used to generate the answer.
        response.SearchResults = azureCognitiveSearchResponse.SearchResults;

        // Build a string with all the sources, where each source is prefixed with the document title.
        var sourcesBuilder = new StringBuilder();
        foreach (var result in azureCognitiveSearchResponse.SearchResults)
        {
            foreach (var caption in result.Captions)
            {
                sourcesBuilder.AppendLine($"{result.DocumentTitle}: {Normalize(caption)}");
            }
            foreach (var highlight in result.Highlights.SelectMany(h => h.Value))
            {
                sourcesBuilder.AppendLine($"{result.DocumentTitle}: {Normalize(highlight)}");
            }
        }

        // Add the sources string to the context, so that the semantic function can use it to construct the prompt.
        context.Variables["sources"] = sourcesBuilder.ToString();

        // Run the semantic function to generate the answer.
        var answer = await this.kernel.RunAsync(context.Variables, function);
        response.Answers.Add(new SearchAnswer { Text = answer.Result });
        return response;
    }

    private static string Normalize(string value)
    {
        return value.Replace('\r', ' ').Replace('\n', ' ');
    }
}