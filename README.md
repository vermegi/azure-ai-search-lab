# Azure AI Search Lab

## About

This project provides a very easy to use learning and experimentation lab to try out various AI-enabled search scenarios in Azure. It provides a web application front-end which uses [Azure AI Search](https://learn.microsoft.com/azure/search/search-what-is-azure-search) and [Azure OpenAI](https://learn.microsoft.com/azure/ai-services/openai/overview) to execute searches with a variety of options - ranging from simple keyword search, to semantic ranking, vector and hybrid search, and using generative AI to answer search queries in various ways. This allows you to quickly understand what each option does, how it affects the search results, and how various approaches compare against each other.

By default, a few documents are added automatically to allow you to use the application directly. You're encouraged to upload your own documents (which you can also easily do via the web app) so you can experiment with searching over your own content.

There are a number of related and somewhat similar projects, most notably the following:

- ["Chat with your data" Solution Accelerator](https://aka.ms/ChatWithYourDataSolutionAccelerator)
- [ChatGPT + Enterprise data with Azure OpenAI and AI Search](https://aka.ms/EntGPTSearch) (demo)
- [Azure Chat Solution Accelerator powered by Azure Open AI Service](https://github.com/microsoft/azurechat)
- [Sample Chat App with Azure OpenAI](https://github.com/Microsoft/sample-app-aoai-chatGPT) (sample)

The main differentiators for this project however are that it's extremely [easy to set up](#deployment) and that it's aimed to be a *teaching and experimentation app*, rather than a sample focusing on just one service or scenario, or showing how you can build a realistic production-ready service yourself.

**Learn about different options and fire off a search query:**

![Search options](media/search-options.png)

**See a sequence diagram of what will happen, depending on the options you've chosen:**

![Sequence diagram](media/sequence-diagram.png)

**Get search results, including *semantic answers* when available:**

![Search results](media/search-results.png)

**Compare the results of various built-in scenarios:**

![Compare scenarios](media/compare-results.png)

## Architecture

```mermaid
graph TD
  acs[Azure AI Search]
  aoai[Azure OpenAI]
  webapp[Web App]
  functionapp[Function Apps]
  storage[Storage Account]

  webapp -->|External vectorization: Generate query embeddings for vector search| aoai
  webapp -->|Send chat requests| aoai
  webapp -->|Send search requests| acs
  webapp -->|Upload new documents| storage
  functionapp -->|External vectorization: Generate embeddings for chunks| aoai
  functionapp -->|Push model: Push chunks into search index| acs
  acs -->|Integrated vectorization: Generate embeddings for chunks and search queries| aoai
  acs -->|Populate search index from documents| storage
  acs -->|External vectorization: Generate chunks and embeddings to index| functionapp
  aoai -->|Find relevant context to build prompt for Azure OpenAI on your data| acs
```

When you deploy the solution, it creates an [Azure AI Search](https://learn.microsoft.com/azure/search/search-what-is-azure-search) service which indexes document content from a blob storage container. (Note that documents are assumed to be in English.)

The documents in the index are also chunked into smaller pieces, and vector embeddings are created for these chunks using either [integrated vectorization](https://learn.microsoft.com/azure/search/vector-search-integrated-vectorization), or external vectorization using a Function App. This allows you to easily try out [vector and hybrid search](https://learn.microsoft.com/azure/search/vector-search-overview). With Azure AI Search on its own, the responses *always* come directly from the source data, rather than being generated by an AI model. You can optionally use [semantic ranking](https://learn.microsoft.com/azure/search/semantic-search-overview) which *does* use AI, not to generate content but to increase the relevancy of the results and provide semantic answers and captions.

The solution also deploys an [Azure OpenAI](https://learn.microsoft.com/azure/ai-services/openai/overview) service. It provides an embeddings model to generate the vector representations of the document chunks and search queries, and a GPT model to generate answers to your search queries. If you choose the option to use [Azure OpenAI "on your data"](https://learn.microsoft.com/azure/ai-services/openai/concepts/use-your-data), these AI-generated responses can be grounded in (and even limited to) the information in your Azure AI Search indexes. This option allows you to let Azure OpenAI orchestrate the [Retrieval Augmented Generation (RAG)](https://aka.ms/what-is-rag) pattern. This means your search query will first be used to retrieve the most relevant documents (or preferably *smaller chunks of those documents*) from your private data source. Those search results are then used as context in the prompt that gets sent to the AI model, along with the original search query. This allows the AI model to generate a response based on the most relevant source data, rather than the public data that was used to train the model. Next to letting Azure OpenAI orchestrate the RAG pattern, the web application can also use [Semantic Kernel](https://learn.microsoft.com/semantic-kernel/overview/) to perform that orchestration, using a prompt and other parameters you can control yourself.

## Deployment

Deploying the solution is as easy as clicking the "Deploy to Azure" button below. This gives you a fully configured environment to experiment with, and even comes with a few sample documents to search through.

Before you do that however, the most important choice you need to make is which Azure region to use, given the following constraints:

- The region must [support the GPT chat completions and embeddings models you want to use in Azure OpenAI](https://learn.microsoft.com/azure/ai-services/openai/concepts/models).
- The region must [support AI Enrichment in Azure AI Search](https://azure.microsoft.com/explore/global-infrastructure/products-by-region/?products=search&regions=all) in order to use skillsets.
- At the time of writing (September 2023), if you are using `gpt-35-turbo` then only model version `0301` is supported for [Azure OpenAI on your data](https://learn.microsoft.com/azure/ai-services/openai/concepts/use-your-data); model version `0613` can therefore not be used with `gpt-35-turbo`, but it is supported when using `gpt-35-turbo-16k`.

This means the following Azure region and model combinations are currently supported:

| Azure Region      | `gpt-4` (version `0613`) | `gpt-4-32k` (version `0613`) | `gpt-35-turbo` (version `0301`) | `gpt-35-turbo-16k` (version `0613`) | `text-embedding-ada-002` (version `2`) |
| ----------------- | ------------------------ | ---------------------------- | ------------------------------- | ----------------------------------- | -------------------------------------- |
| East US           | ✅                        | ✅                            | ✅                               | ✅                                   | ✅                                      |
| UK South          | ✅                        | ✅                            | ✅                               | ✅                                   | ✅                                      |
| France Central    | ✅                        | ✅                            | ✅                               | ✅                                   | ✅                                      |
| Japan East        | ✅                        | ✅                            | ❌                               | ✅                                   | ✅                                      |
| North Central US  | ❌                        | ❌                            | ❌                               | ✅                                   | ✅                                      |
| Switzerland North | ❌                        | ❌                            | ❌                               | ✅                                   | ✅                                      |
| South Central US  | ❌                        | ❌                            | ✅                               | ❌                                   | ✅                                      |
| West Europe       | ❌                        | ❌                            | ✅                               | ❌                                   | ✅                                      |

Once you've decided on a region, you can deploy the solution with its default parameters, or change these for your specific needs.

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fvermegi%2Fazure-ai-search-lab%2Fmain%2Fazuredeploy.json)

The following deployment parameters are used:

| Parameter                        | Purpose                                                                                                                                                                                                                                                                                                      | Default value                                                                                                                                                                                                                                                                                                                                                      |
| -------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `resourcePrefix`                 | A prefix to use for all deployed Azure resources (maximum 8 characters, lowercase letters only)                                                                                                                                                                                                              |                                                                                                                                                                                                                                                                                                                                                                    |
| `embeddingModelName`             | The name of the [Azure OpenAI embedding model](https://learn.microsoft.com/azure/ai-services/openai/concepts/models) to use                                                                                                                                                                                  | `text-embedding-ada-002`                                                                                                                                                                                                                                                                                                                                           |
| `embeddingModelVersion`          | The version of the embedding model to use                                                                                                                                                                                                                                                                    | `2`                                                                                                                                                                                                                                                                                                                                                                |
| `embeddingModelVectorDimensions` | The [dimensions](https://learn.microsoft.com/azure/ai-services/openai/concepts/models#embeddings-models) of the vectors generated by the embedding model                                                                                                                                                     | `1536`                                                                                                                                                                                                                                                                                                                                                             |
| `gptModelName`                   | The name of the [Azure OpenAI GPT model](https://learn.microsoft.com/azure/ai-services/openai/concepts/models) to use                                                                                                                                                                                        | `gpt-35-turbo`                                                                                                                                                                                                                                                                                                                                                     |
| `gptModelVersion`                | The version of the GPT model to use                                                                                                                                                                                                                                                                          | `0301`                                                                                                                                                                                                                                                                                                                                                             |
| `searchServiceSku`               | The [name of the Azure AI Search service tier](https://learn.microsoft.com/azure/search/search-sku-tier) to use (note that this impacts certain [limits](https://learn.microsoft.com/azure/search/search-limits-quotas-capacity), for example the maximum blob size for indexers is 16 MB on the basic tier) | `basic`                                                                                                                                                                                                                                                                                                                                                            |
| `initialDocumentUrls`            | A space-separated list of URLs for the documents to include by default                                                                                                                                                                                                                                       | A [resiliency](https://azure.microsoft.com/mediahandler/files/resourcefiles/resilience-in-azure-whitepaper/Resiliency-whitepaper.pdf) and [compliance](https://azure.microsoft.com/mediahandler/files/resourcefiles/data-residency-data-sovereignty-and-compliance-in-the-microsoft-cloud/Data_Residency_Data_Sovereignty_Compliance_Microsoft_Cloud.pdf) document |

After the solution was deployed, simply browse to the App Service web app to start searching!

You can of course also deploy the [ARM template](azuredeploy.json) manually, and if you intend to run the web app locally then you can use the [`azuredeploy.ps1` PowerShell script](azuredeploy.ps1) to deploy the ARM template and automatically populate the required ASP.NET Core user secrets for the local web app.

## Authentication

By default, the web app is publicly accessible after deployment, which also means the document content is freely searchable. This isn't a problem for the documents that are included by default (which are public anyway), but if you're uploading your own documents you probably want to add authentication to the web app.

This can easily be done by setting up the built-in [authentication and authorization feature on App Service](https://learn.microsoft.com/azure/app-service/overview-authentication-authorization), which can make the web app accessible only by users from your own organization for example. If you're using Microsoft Entra ID (formerly Azure AD) as the identity provider, then you can also easily [restrict your application to a set of users](https://learn.microsoft.com/azure/active-directory/develop/howto-restrict-your-app-to-a-set-of-users), by configuring user assignment to be required.

## Configuration

The ARM template deploys the services and sets the configuration settings for the Web App and Function Apps. Most of these shouldn't be changed as they contain connection settings between the various services, but you can change the settings below for the App Service Web App.

> Note that the settings of the Function Apps shouldn't be changed, as the [power skill](https://github.com/Azure-Samples/azure-search-power-skills/tree/main/Vector/EmbeddingGenerator) was tweaked for this project to take any relevant settings from the request sent by the Azure AI Search skillset instead of from configuration (for example, the embedding model and chunk size to use).

| Setting                              | Purpose                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                | Default value                                                                                                                                                                                                                                                                                                                                                      |
| ------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `OpenAIApiVersion`                   | The API version of Azure OpenAI to use                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 | `2023-12-01-preview`                                                                                                                                                                                                                                                                                                                                               |
| `OpenAIEmbeddingDeployment`*         | The deployment name of the [Azure OpenAI embedding model](https://learn.microsoft.com/azure/ai-services/openai/concepts/models) to use                                                                                                                                                                                                                                                                                                                                                                                                 | `text-embedding-ada-002`                                                                                                                                                                                                                                                                                                                                           |
| `OpenAIEmbeddingVectorDimensions`    | The [dimensions](https://learn.microsoft.com/azure/ai-services/openai/concepts/models#embeddings-models) of the vectors generated by the embedding model                                                                                                                                                                                                                                                                                                                                                                               | `1536`                                                                                                                                                                                                                                                                                                                                                             |
| `OpenAIGptDeployment`                | The deployment name of the [Azure OpenAI GPT model](https://learn.microsoft.com/azure/ai-services/openai/concepts/models) to use                                                                                                                                                                                                                                                                                                                                                                                                       | `gpt-35-turbo`                                                                                                                                                                                                                                                                                                                                                     |
| `StorageContainerNameBlobDocuments`* | The name of the storage container that contains the documents                                                                                                                                                                                                                                                                                                                                                                                                                                                                          | `blob-documents`                                                                                                                                                                                                                                                                                                                                                   |
| `StorageContainerNameBlobChunks`*    | The name of the storage container that contains the document chunks                                                                                                                                                                                                                                                                                                                                                                                                                                                                    | `blob-chunks`                                                                                                                                                                                                                                                                                                                                                      |
| `TextChunkerPageLength`              | In case of integrated vectorization, the number of characters per page (chunk) when splitting documents into smaller pieces                                                                                                                                                                                                                                                                                                                                                                                                            | `2000`                                                                                                                                                                                                                                                                                                                                                             |
| `TextChunkerPageOverlap`             | In case of integrated vectorization, the number of characters to overlap between consecutive pages (chunks)                                                                                                                                                                                                                                                                                                                                                                                                                            | `500`                                                                                                                                                                                                                                                                                                                                                              |
| `TextEmbedderNumTokens`              | In case of external vectorization, the number of tokens per chunk when splitting documents into smaller pieces                                                                                                                                                                                                                                                                                                                                                                                                                         | `2048`                                                                                                                                                                                                                                                                                                                                                             |
| `TextEmbedderTokenOverlap`           | In case of external vectorization, the number of tokens to overlap between consecutive chunks                                                                                                                                                                                                                                                                                                                                                                                                                                          | `0`                                                                                                                                                                                                                                                                                                                                                                |
| `TextEmbedderMinChunkSize`           | In case of external vectorization, the minimum number of tokens of a chunk (smaller chunks are excluded)                                                                                                                                                                                                                                                                                                                                                                                                                               | `10`                                                                                                                                                                                                                                                                                                                                                               |
| `SearchIndexNameBlobDocuments`*      | The name of the search index that contains the documents                                                                                                                                                                                                                                                                                                                                                                                                                                                                               | `blob-documents`                                                                                                                                                                                                                                                                                                                                                   |
| `SearchIndexNameBlobChunks`*         | The name of the search index that contains the document chunks                                                                                                                                                                                                                                                                                                                                                                                                                                                                         | `blob-chunks`                                                                                                                                                                                                                                                                                                                                                      |
| `SearchIndexerSkillType`*            | The type of chunking and embedding skill to use as part of the documents indexer: `integrated` uses [integrated vectorization](https://learn.microsoft.com/azure/search/vector-search-integrated-vectorization); `pull` uses a custom skill with a [knowledge store](https://learn.microsoft.com/azure/search/knowledge-store-concept-intro) to store the chunk data in blobs and a separate indexer to pull these into the document chunks index; `push` directly uploads the data from a custom skill into the document chunks index | `integrated`                                                                                                                                                                                                                                                                                                                                                       |
| `SearchIndexerScheduleMinutes`*      | The number of minutes between indexer executions in Azure AI Search                                                                                                                                                                                                                                                                                                                                                                                                                                                                    | `5`                                                                                                                                                                                                                                                                                                                                                                |
| `InitialDocumentUrls`                | A space-separated list of URLs for the documents to include by default                                                                                                                                                                                                                                                                                                                                                                                                                                                                 | A [resiliency](https://azure.microsoft.com/mediahandler/files/resourcefiles/resilience-in-azure-whitepaper/Resiliency-whitepaper.pdf) and [compliance](https://azure.microsoft.com/mediahandler/files/resourcefiles/data-residency-data-sovereignty-and-compliance-in-the-microsoft-cloud/Data_Residency_Data_Sovereignty_Compliance_Microsoft_Cloud.pdf) document |
| `DefaultSystemRoleInformation`       | The default instructions for the AI model                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              | "You are an AI assistant that helps people find information."                                                                                                                                                                                                                                                                                                      |
| `DefaultCustomOrchestrationPrompt`   | The default prompt for the AI model when using custom orchestration                                                                                                                                                                                                                                                                                                                                                                                                                                                                    | A prompt that instructs the AI model to respond from provided data sources (with citations).                                                                                                                                                                                                                                                                       |
| `DisableUploadDocuments`             | Set this to `true` to disable the functionality to upload documents, preventing uploads by users of the Web App (you can still upload documents directly to the Azure storage container if you have permissions there)                                                                                                                                                                                                                                                                                                                 | `false`                                                                                                                                                                                                                                                                                                                                                            |
| `DisableResetSearchConfiguration`    | Set this to `true` to disable the functionality to reset the search configuration by users of the Web App                                                                                                                                                                                                                                                                                                                                                                                                                              | `false`                                                                                                                                                                                                                                                                                                                                                            |

*If you change this setting, you have to reset the search configuration (which can be done through the web app) to apply the new settings and regenerate the chunks and vectors.
