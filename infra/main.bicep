targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the the environment which is used to generate a short unique hash used in all resources.')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
@allowed(['eastus','westus2'])
param location string

param appServicePlanName string = ''
param backendServiceName string = ''
param resourceGroupName string = ''

param searchServiceName string = ''
param searchServiceResourceGroupName string = ''
param searchServiceResourceGroupLocation string = location
param searchServiceSkuName string = ''
param searchIndexName string = 'damu-index'
param searchSemanticSearchConfig string = 'damu-semantic-config'

param openAiResourceName string = ''
param openAiResourceGroupName string = ''
param openAiResourceGroupLocation string = location
param formRecognizerServiceName string = ''
param formRecognizerResourceGroupName string = ''
param formRecognizerResourceGroupLocation string = location
param formRecognizerSkuName string = 'S0'
param openAiSkuName string = ''

@description('Name of the chat completion model deployment')
param chatDeploymentName string = 'chat'

@description('Name of the chat completion model')
param chatModelName string = 'gpt-4o'
param chatModelVersion string = '2024-05-13'

param embeddingDeploymentName string = 'embedding'
param embeddingModelName string = 'text-embedding-ada-002'
param embeddingModelVersion string = '2'
param embeddingVectorDimension int = 1536

@description('Name of the storage account')
param storageAccountName string = ''

@description('Name of the storage container. Default: content')
param storageContainerName string = 'notes'

@description('Location of the resource group for the storage account')
param storageResourceGroupLocation string = location

@description('Name of the resource group for the storage account')
param storageResourceGroupName string = ''

// Used for the Azure AD application
param authClientId string
@secure()
param authClientSecret string

// Used for Cosmos DB
@description('Is chat history enabled')
param isHistoryEnabled bool = false
param cosmosAccountName string = ''

@description('Id of the user or app to assign application roles')
param principalId string = ''

@description('Name of the Azure Function App Service Plan')
param functionAppServicePlanName string = ''
@description('Name of the Azure Function App')
param functionServiceName string = ''

@description('Name of the Azure Log Analytics workspace')
param logAnalyticsName string = ''

@description('Name of the Azure Application Insights dashboard')
param applicationInsightsDashboardName string = ''

@description('Name of the Azure Application Insights resource')
param applicationInsightsName string = ''

var abbrs = loadJsonContent('abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = { 'azd-env-name': environmentName }

// Organize resources in a resource group
resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: !empty(resourceGroupName) ? resourceGroupName : '${abbrs.resourcesResourceGroups}${environmentName}'
  location: location
  tags: tags
}

resource openAiResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' existing = if (!empty(openAiResourceGroupName)) {
  name: !empty(openAiResourceGroupName) ? openAiResourceGroupName : resourceGroup.name
}

resource searchServiceResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' existing = if (!empty(searchServiceResourceGroupName)) {
  name: !empty(searchServiceResourceGroupName) ? searchServiceResourceGroupName : resourceGroup.name
}

resource formRecognizerResourceGroup 'Microsoft.Resources/resourceGroups@2024-03-01' existing = if (!empty(formRecognizerResourceGroupName)) {
  name: !empty(formRecognizerResourceGroupName) ? formRecognizerResourceGroupName : resourceGroup.name
}

resource storageResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' existing = if (!empty(storageResourceGroupName)) {
  name: !empty(storageResourceGroupName) ? storageResourceGroupName : resourceGroup.name
}

// Monitor application with Azure Monitor
module monitoring 'core/monitor/monitoring.bicep' = {
  name: 'monitoring'
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    includeApplicationInsights: true
    logAnalyticsName: !empty(logAnalyticsName) ? logAnalyticsName : '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    applicationInsightsName: !empty(applicationInsightsName) ? applicationInsightsName : '${abbrs.insightsComponents}${resourceToken}'
    applicationInsightsDashboardName: !empty(applicationInsightsDashboardName) ? applicationInsightsDashboardName : '${abbrs.portalDashboards}${resourceToken}'
  }
}

// Storage account
module storage 'core/storage/storage-account.bicep' = {
  name: 'storage'
  scope: storageResourceGroup
  params: {
    name: !empty(storageAccountName) ? storageAccountName : '${abbrs.storageStorageAccounts}${resourceToken}'
    location: storageResourceGroupLocation
    publicNetworkAccess: 'Enabled'
    tags: tags
    sku: {
      name: 'Standard_LRS'
    }
    deleteRetentionPolicy: {
      enabled: true
      days: 2
    }
    containers: [
      {
        name: storageContainerName
        publicAccess: 'Blob'
      }
    ]
  }
}

// The application frontend
var appServiceName = !empty(backendServiceName) ? backendServiceName : '${abbrs.webSitesAppService}backend-${resourceToken}'
var authIssuerUri = '${environment().authentication.loginEndpoint}${tenant().tenantId}/v2.0'
module backend 'app/backend.bicep' = {
  name: 'web'
  scope: resourceGroup
  params: {
    location: location
    tags: union(tags, { 'azd-service-name': 'backend' })
    appServicePlanName: !empty(appServicePlanName) ? appServicePlanName : '${abbrs.webServerFarms}backend-${resourceToken}'
    appServiceName: appServiceName
    storageAccountName: storage.outputs.name
    authClientSecret: authClientSecret
    authClientId: authClientId
    authIssuerUri: authIssuerUri
    appSettings: {
      // frontend settings
      'FrontendSettings:auth_enabled': 'false'
      'FrontendSettings:feedback_enabled': 'false'
      'FrontendSettings:ui:title': 'Damu'
      'FrontendSettings:ui:chat_description': 'This chatbot is configured to answer your questions.'
      'FrontendSettings:ui:show_share_button': true
      'FrontendSettings:sanitize_answer': false
      'FrontendSettings:history_enabled': isHistoryEnabled
      // search
      'AISearchOptions:Endpoint': searchService.outputs.endpoint
      'AISearchOptions:ApiKey': ''
      'AISearchOptions:IndexName': searchIndexName
      'AISearchOptions:SemanticConfigurationName': searchSemanticSearchConfig
      // openai
      'OpenAIOptions:Endpoint': openAi.outputs.endpoint
      'OpenAIOptions:ApiKey': ''
      'OpenAIOptions:ChatDeployment': chatDeploymentName
      'OpenAIOptions:EmbeddingDeployment': embeddingDeploymentName
      // storage
      'StorageOptions:BlobStorageEndpoint': storage.outputs.primaryEndpoints.blob
      'StorageOptions:BlobStorageConnectionString': ''
      'StorageOptions:BlobStorageContainerName': storageContainerName      
    }
  }
}

// Index Orchestrator
var functionAppServiceName = !empty(functionServiceName) ? functionServiceName : '${abbrs.webSitesFunctions}function-${resourceToken}'
module function './app/function.bicep' = {
  name: 'function'
  scope: resourceGroup
  params: {
    name: functionAppServiceName
    location: location
    tags: tags
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    appServicePlanName: !empty(functionAppServicePlanName) ? functionAppServicePlanName : '${abbrs.webServerFarms}function-${resourceToken}'
    storageAccountName: storage.outputs.name
    useManagedIdentity: true
    appSettings: {
      AzureOpenAiEmbeddingDeployment: embeddingDeploymentName
      AzureOpenAiEmbeddingModel: embeddingModelName
      AzureOpenAiEndpoint: openAi.outputs.endpoint
      DocIntelEndPoint: formRecognizer.outputs.endpoint
      FUNCTIONS_WORKER_RUNTIME: 'dotnet-isolated'
      ModelDimensions: embeddingVectorDimension
      ProjectPrefix: environmentName
      SearchEndpoint: searchService.outputs.endpoint
    }
  }
}

module openAi 'core/ai/cognitiveservices.bicep' = {
  name: 'openai'
  scope: openAiResourceGroup
  params: {
    name: !empty(openAiResourceName) ? openAiResourceName : '${abbrs.cognitiveServicesAccounts}${resourceToken}'
    location: openAiResourceGroupLocation
    tags: tags
    sku: {
      name: !empty(openAiSkuName) ? openAiSkuName : 'S0'
    }
    deployments: [
      {
        name: chatDeploymentName
        model: {
          format: 'OpenAI'
          name: chatModelName
          version: chatModelVersion
        }
        capacity: 30
      }
      {
        name: embeddingDeploymentName
        model: {
          format: 'OpenAI'
          name: embeddingModelName
          version: embeddingModelVersion
        }
        capacity: 30
      }
    ]
  }
}

module formRecognizer 'core/ai/cognitiveservices.bicep' = {
  name: 'formrecognizer'
  scope: formRecognizerResourceGroup
  params: {
    name: !empty(formRecognizerServiceName) ? formRecognizerServiceName : '${abbrs.cognitiveServicesFormRecognizer}${resourceToken}'
    kind: 'FormRecognizer'
    location: formRecognizerResourceGroupLocation
    tags: tags
    sku: {
      name: formRecognizerSkuName
    }
  }
}

module searchService 'core/search/search-services.bicep' = {
  name: 'search-service'
  scope: searchServiceResourceGroup
  params: {
    name: !empty(searchServiceName) ? searchServiceName : 'gptkb-${resourceToken}'
    location: searchServiceResourceGroupLocation
    tags: tags
    authOptions: {
      aadOrApiKey: {
        aadAuthFailureMode: 'http401WithBearerChallenge'
      }
    }
    sku: {
      name: !empty(searchServiceSkuName) ? searchServiceSkuName : 'standard'
    }
    semanticSearch: 'free'
  }
}

// The chat history database
module cosmos 'db.bicep' = if (isHistoryEnabled) {
  name: 'cosmos'
  scope: resourceGroup
  params: {
    accountName: !empty(cosmosAccountName) ? cosmosAccountName : '${abbrs.documentDBDatabaseAccounts}${resourceToken}'
    location: location
    tags: tags
    principalIds: [principalId, backend.outputs.identityPrincipalId]
  }
}


// USER ROLES
module storageRoleUser 'core/security/role.bicep' = {
  scope: storageResourceGroup
  name: 'storage-role-user'
  params: {
    principalId: principalId
    roleDefinitionId: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
    principalType: 'User'
  }
}

module openAiRoleUser 'core/security/role.bicep' = {
  scope: openAiResourceGroup
  name: 'openai-role-user'
  params: {
    principalId: principalId
    roleDefinitionId: '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
    principalType: 'User'
  }
}

module searchRoleUser 'core/security/role.bicep' = {
  scope: searchServiceResourceGroup
  name: 'search-role-user'
  params: {
    principalId: principalId
    roleDefinitionId: '1407120a-92aa-4202-b7e9-c0e197c71c8f'
    principalType: 'User'
  }
}

module searchIndexDataContribRoleUser 'core/security/role.bicep' = {
  scope: searchServiceResourceGroup
  name: 'search-index-data-contrib-role-user'
  params: {
    principalId: principalId
    roleDefinitionId: '8ebe5a00-799e-43f5-93ac-243d3dce84a7'
    principalType: 'User'
  }
}

module searchServiceContribRoleUser 'core/security/role.bicep' = {
  scope: searchServiceResourceGroup
  name: 'search-service-contrib-role-user'
  params: {
    principalId: principalId
    roleDefinitionId: '7ca78c08-252a-4471-8644-bb5ff32d4ba0'
    principalType: 'User'
  }
}

// SYSTEM IDENTITIES
module storageRoleBackend 'core/security/role.bicep' = {
  scope: storageResourceGroup
  name: 'storage-role-backend'
  params: {
    principalId: backend.outputs.identityPrincipalId
    roleDefinitionId: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
    principalType: 'ServicePrincipal'
  }
}

module storageRoleFunctionApp 'core/security/role.bicep' = {
  scope: storageResourceGroup
  name: 'storage-role-functionapp'
  params: {
    principalId: function.outputs.SERVICE_FUNCTION_IDENTITY_PRINCIPAL_ID
    roleDefinitionId: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
    principalType: 'ServicePrincipal'
  }
}

module openAiRoleBackend 'core/security/role.bicep' = {
  scope: openAiResourceGroup
  name: 'openai-role-backend'
  params: {
    principalId: backend.outputs.identityPrincipalId
    roleDefinitionId: '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
    principalType: 'ServicePrincipal'
  }
}

module openAiRoleFunctionApp 'core/security/role.bicep' = {
  scope: openAiResourceGroup
  name: 'openai-role-functionapp'
  params: {
    principalId: function.outputs.SERVICE_FUNCTION_IDENTITY_PRINCIPAL_ID
    roleDefinitionId: '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
    principalType: 'ServicePrincipal'
  }
}

module searchRoleBackend 'core/security/role.bicep' = {
  scope: searchServiceResourceGroup
  name: 'search-role-backend'
  params: {
    principalId: backend.outputs.identityPrincipalId
    roleDefinitionId: '1407120a-92aa-4202-b7e9-c0e197c71c8f'
    principalType: 'ServicePrincipal'
  }
}

module searchRoleFunctionApp 'core/security/role.bicep' = {
  scope: searchServiceResourceGroup
  name: 'search-role-functionapp'
  params: {
    principalId: function.outputs.SERVICE_FUNCTION_IDENTITY_PRINCIPAL_ID
    roleDefinitionId: '1407120a-92aa-4202-b7e9-c0e197c71c8f'
    principalType: 'ServicePrincipal'
  }
}

output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output AZURE_RESOURCE_GROUP string = resourceGroup.name

output BACKEND_URI string = backend.outputs.uri

// search
output AZURE_SEARCH_INDEX string = searchIndexName
output AZURE_SEARCH_SERVICE string = searchService.outputs.name
output AZURE_SEARCH_SERVICE_RESOURCE_GROUP string = searchServiceResourceGroup.name
output AZURE_SEARCH_SKU_NAME string = searchService.outputs.skuName
output AZURE_SEARCH_KEY string = searchService.outputs.adminKey
output AZURE_SEARCH_SEMANTIC_SEARCH_CONFIG string = searchSemanticSearchConfig

// openai
output AZURE_OPENAI_RESOURCE string = openAi.outputs.name
output AZURE_OPENAI_RESOURCE_GROUP string = openAiResourceGroup.name
output AZURE_OPENAI_ENDPOINT string = openAi.outputs.endpoint
output AZURE_OPENAI_CHAT_NAME string = chatDeploymentName
output AZURE_OPENAI_CHAT_MODEL string = chatModelName
output AZURE_OPENAI_SKU_NAME string = openAi.outputs.skuName
output AZURE_OPENAI_KEY string = openAi.outputs.key
output AZURE_OPENAI_EMBEDDING_NAME string = embeddingDeploymentName

output AUTH_ISSUER_URI string = authIssuerUri
