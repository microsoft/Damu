# Project

This is an accelerator for combining natural language queries and FHIR queries into a patient data copilot.

## Features
- Natural language chat UI and AI generative answers
- blob triggered function to process, vector embed and index clinical notes in JSON format
- RAG pattern via hybrid semantic search on vector embedded clinical notes in JSON format
- FHIR API plugin to retrieve patient data (untested)
- Semantic function for LLM-generated FHIR queries based on user query to use with FHIR API plugin (untested)
- Reference citations view support for retrieved supporting data
- Search results in table format with export support (WIP)

## Application Architecture and Flow

![Application Architecture Flow Diagram](./docs/App%20Architecture.png)

- **User Interface**:  The application’s chat interface is a react/js web application. This interface is what accepts user queries, routes request to the application backend, and displays generated responses.
	- originally based on the sample front end found in [Sample Chat App with AOAI - GitHub](https://github.com/microsoft/sample-app-aoai-chatGPT) 
- **Backend**: The application backend is an [ASP.NET Core Minimal API](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/overview). The backend, deployed to an Azure App Service, hosts the react web application and the Semantic Kernel orchestration of the different services. Services and SDKs used in the RAG chat application include:
	- [Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/overview) – orchestrates the RAG pattern completion between the services while managing chat history and other capabilities – ready for easy extension with additional plugin functions easy (more data sources, logic, actions, etc.).
	- [Azure AI Search](https://learn.microsoft.com/azure/search/search-what-is-azure-search) – searches indexed documents using vector search capabilities.
	- [Azure OpenAI Service](https://learn.microsoft.com/azure/search/search-what-is-azure-search) – provides the Large Language Models to generate responses.
- **Document Preparation**: an IndexOrchestration Azure Function is included for chunking, embedding and indexing clinical note JSON blobs. The Azure Function is triggered on new and overwritten blobs in a `notes` container of the deployed storage account. Services and SDKs used in this process include:
	- [Document Intelligence](https://learn.microsoft.com/en-us/azure/ai-services/document-intelligence/overview?view=doc-intel-4.0.0) – used for analyzing the documents via the [pre-built layout model](https://learn.microsoft.com/en-us/azure/ai-services/document-intelligence/concept-layout?view=doc-intel-4.0.0&tabs=sample-code) as a part of the chunking process and for HTML support inside the JSON properties.
	- [Azure OpenAI Service](https://learn.microsoft.com/azure/ai-services/openai/overview) – provides the Large Language Models to generate vectoring embeddings for the indexed document chunks.
	- [Azure AI Search](https://learn.microsoft.com/azure/search/search-what-is-azure-search) – indexes embedded document chunks from the data stored in an Azure Storage Account. This makes the documents searchable using [vector search](https://learn.microsoft.com/azure/search/search-get-started-vector) capabilities.

# Getting Started

This sample application, as deployed, includes the following Azure components: 

![Deployed Infrastructure Architecture Diagram](./docs/Infra%20Architecture.png)

## Account Requirements

In order to deploy and run this example, you'll need

- **Azure Account** - If you're new to Azure, get an [Azure account for free](https://aka.ms/free) and you'll get some free Azure credits to get started.
- **Azure subscription with access enabled for the Azure OpenAI service** - [You can request access](https://aka.ms/oaiapply). You can also visit [the Cognitive Search docs](https://azure.microsoft.com/free/cognitive-search/) to get some free Azure credits to get you started.
- **Azure account permissions** - Your Azure Account must have `Microsoft.Authorization/roleAssignments/write` permissions, such as [User Access Administrator](https://learn.microsoft.com/azure/role-based-access-control/built-in-roles#user-access-administrator) or [Owner](https://learn.microsoft.com/azure/role-based-access-control/built-in-roles#owner).

> [!WARNING]<br>
> By default this sample will create an Azure AI Search resource that has a monthly cost, as well as Document Intelligence (previously Form Recognizer) resource that has cost per document page. You can switch them to free versions of each of them if you want to avoid this cost by changing the parameters file under the infra folder (though there are some limits to consider)

## Cost estimation

Pricing varies per region and usage, so it isn't possible to predict exact costs for your usage. However, you can try the [Azure pricing calculator](https://azure.microsoft.com/pricing/calculator/) for the resources below:

- [**Azure App Service**](https://azure.microsoft.com/en-us/pricing/details/app-service/linux/)
- [**Azure Functions**](https://azure.microsoft.com/en-us/pricing/details/functions/)
- [**Azure OpenAI Service**](https://azure.microsoft.com/pricing/details/cognitive-services/openai-service/)
- [**Azure Document Intelligence**](https://azure.microsoft.com/pricing/details/form-recognizer/)
- [**Azure AI Search**](https://azure.microsoft.com/pricing/details/search/)
- [**Azure Blob Storage**](https://azure.microsoft.com/pricing/details/storage/blobs/)
- [**Azure Monitor**](https://azure.microsoft.com/pricing/details/monitor/)

## Deployment
This project supports `azd` for easy deployment of the complete application, as defined in the main.bicep resources.  

See [Deployment Instructions here](./infra/README.md).

### Process notes into the Search Service with Blob trigger

1. Initialize the index:
	1. In Azure: navigate to the deployed Azure Function, the name should start with `func-function-`
	1. Under the `Functions` tab on the Overview page, click on the `UpsertIndex` function.
	1. Click `Test/Run`, leave all defaults and click `Run`.
1. Add documents to be indexed, trigger the function:
	1. In Azure: navigate to the deployed storage account.
	1. Browse into the `Data storage/Containers` blade and into the `notes` container.
	1. Click `Upload` and add note JSON files to be processed.
1. Confirm successful indexing:
	1. In Azure: navigate to the deployed AI Search service
	1. Browse into the `Indexes` blade (Search Management) 
	1. A new index should exist, prefixed with the environment name provided during deployment.
	1. Open and search in the index to confirm content from the files uploaded are properly searchable.

> NOTE <br>
> It may take several minutes to see processed documents in the index

## Running locally for Dev and Debug
As many cloud resources are required to run the client app and minimal API even locally, deployment to Azure first will provision all the necessary services. You can then configure your local user secrets to point to those required cloud resources before building and running locally for the purposes of debugging and development.

**Required cloud resources:**
- Azure AI Search
- Azure OpenAI Service
	- chat model
	- embedding model
- Azure Document Intelligence
- Storage Account for blob trigger

### Running the ChatApp.Server and ChatApp.Client locally
1. Configure user secrets for the ChatApp.Server project, based on deployed resources.
	1. In Azure: navigate to the deployed Web App, and into the `Configuration` blade (under Settings).
	1. Copy the below required settings from the deployed Environment Variables into your local user secrets:
	```json
	{
		"OpenAIOptions:Endpoint": "YOUR_OPENAI_ENDPOINT",
		"OpenAIOptions:EmbeddingDeployment": "embedding",
		"OpenAIOptions:ChatDeployment": "chat",
		"FrontendSettings:ui:title": "Damu",
		"FrontendSettings:ui:show_share_button": "True",
		"FrontendSettings:ui:chat_description": "This chatbot is configured to answer your questions",
		"FrontendSettings:sanitize_answer": "false",
		"FrontendSettings:history_enabled": "false",
		"FrontendSettings:feedback_enabled": "false",
		"FrontendSettings:auth_enabled": "false",
		"ENABLE_CHAT_HISTORY": "false",
		"AzureAdOptions:TenantId": "YOUR_TENANT_ID",
		"AISearchOptions:SemanticConfigurationName": "YOUR_SEMANTIC_CONFIGURATION_NAME",
		"AISearchOptions:IndexName": "YOUR_INDEX_NAME",
		"AISearchOptions:Endpoint": "YOUR_SEARCH_SERVICE_ENDPOINT"
	}
	```
	> NOTE <br>
	> See `appsettings.json` in the ChatApp.Server project for more settings that can be configured in user secrets if using optional features such as CosmosDB for history.
1. Build and run the ChatApp.Server and ChatApp.Client projects
1. Open a browser and navigate to `https://localhost:5173` as instructed in the terminal to interact with the chat client.

### Running the IndexOrchestration function locally

1. Configure user secrets for the IndexOrchestration project, based on deployed resources.
	1. In Azure: navigate to the deployed Azure Function, and into the `Configuration` blade (under Settings).
	1. Copy the below required settings from the deployed Environment Variables into your local secrets:
	```json
	{
		"AzureOpenAiEmbeddingDeployment": "embedding",
		"AzureOpenAiEmbeddingModel": "text-embedding-ada-002",
		"AzureOpenAiEndpoint": "YOUR_OPENAI_ENDPOINT",
		"AzureWebJobsStorage": "UseDevelopmentStorage=true",
		"DocIntelEndPoint": "YOUR_DOC_INTELLIGENCE_ENDPOINT",
		"FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
		"IncomingBlobConnStr": "YOUR_INCOMING_BLOB_CONNECTION_STRING",
		"ModelDimensions": 1536,
		"ProjectPrefix": "YOUR_PROJECT_PREFIX",
		"SearchEndpoint": "YOUR_SEARCH_SERVICE_ENDPOINT"
	}
	```
	> NOTE <br>
	> See `local_settings_example.json` in the IndexOrchestration project for more settings that can optionally be configured.
1. Build and run the IndexOrchestration project
1. Upload or overwrite a note JSON file in the `notes` container of the storage account to trigger the function.


# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
