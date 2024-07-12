targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the the environment which is used to generate a short unique hash used in all resources.')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

param appServicePlanName string = ''
param backendServiceName string = ''
param resourceGroupName string = ''

param searchServiceName string = ''
param searchServiceResourceGroupName string = ''
param searchServiceResourceGroupLocation string = location
param searchServiceSkuName string = ''
param searchIndexName string = 'gptkbindex'
param searchUseSemanticSearch bool = false
param searchSemanticSearchConfig string = 'default'
param searchTopK int = 5
param searchEnableInDomain bool = true
param searchContentColumns string = 'content'
param searchFilenameColumn string = 'filepath'
param searchTitleColumn string = 'title'
param searchUrlColumn string = 'url'

param openAiResourceName string = ''
param openAiResourceGroupName string = ''
param openAiResourceGroupLocation string = location
param formRecognizerServiceName string = ''
param formRecognizerResourceGroupName string = ''
param formRecognizerResourceGroupLocation string = location
param formRecognizerSkuName string = 'S0'
param openAiSkuName string = ''
param openAIModel string = 'gpt-4o'
param openAIModelName string = 'gpt-4o'
param openAITemperature int = 0
param openAITopP int = 1
param openAIMaxTokens int = 1000
param openAIStopSequence string = ''
param openAISystemMessage string = 'You are an AI assistant that helps people find information.'
param openAIStream bool = false
param embeddingDeploymentName string = 'embedding'
param embeddingModelName string = 'text-embedding-ada-002'

@description('Name of the storage account')
param storageAccountName string = ''

@description('Name of the storage container. Default: content')
param storageContainerName string = 'content'

@description('Location of the resource group for the storage account')
param storageResourceGroupLocation string = location

@description('Name of the resource group for the storage account')
param storageResourceGroupName string = ''

// Used for the Azure AD application
param authClientId string
@secure()
param authClientSecret string

// Used for Cosmos DB
param cosmosAccountName string = ''

@description('Id of the user or app to assign application roles')
param principalId string = ''

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

// Create an App Service Plan to group applications under the same payment plan and SKU
module appServicePlan 'core/host/appserviceplan.bicep' = {
  name: 'appserviceplan'
  scope: resourceGroup
  params: {
    name: !empty(appServicePlanName) ? appServicePlanName : '${abbrs.webServerFarms}${resourceToken}'
    location: location
    tags: tags
    sku: {
      name: 'B1'
      capacity: 1
    }
    kind: 'linux'
  }
}

// The application frontend
var appServiceName = !empty(backendServiceName) ? backendServiceName : '${abbrs.webSitesAppService}backend-${resourceToken}'
var authIssuerUri = '${environment().authentication.loginEndpoint}${tenant().tenantId}/v2.0'
module backend 'core/host/appservice.bicep' = {
  name: 'web'
  scope: resourceGroup
  params: {
    name: appServiceName
    location: location
    tags: union(tags, { 'azd-service-name': 'backend' })
    appServicePlanId: appServicePlan.outputs.id
    runtimeName: 'python'
    runtimeVersion: '3.10'
    scmDoBuildDuringDeployment: true
    managedIdentity: true
    authClientSecret: authClientSecret
    authClientId: authClientId
    authIssuerUri: authIssuerUri
    appSettings: {
      // search
      AZURE_SEARCH_INDEX: searchIndexName
      AZURE_SEARCH_SERVICE: searchService.outputs.name
      AZURE_SEARCH_KEY: searchService.outputs.adminKey
      AZURE_SEARCH_USE_SEMANTIC_SEARCH: searchUseSemanticSearch
      AZURE_SEARCH_SEMANTIC_SEARCH_CONFIG: searchSemanticSearchConfig
      AZURE_SEARCH_TOP_K: searchTopK
      AZURE_SEARCH_ENABLE_IN_DOMAIN: searchEnableInDomain
      AZURE_SEARCH_CONTENT_COLUMNS: searchContentColumns
      AZURE_SEARCH_FILENAME_COLUMN: searchFilenameColumn
      AZURE_SEARCH_TITLE_COLUMN: searchTitleColumn
      AZURE_SEARCH_URL_COLUMN: searchUrlColumn
      // openai
      AZURE_OPENAI_RESOURCE: openAi.outputs.name
      AZURE_OPENAI_MODEL: openAIModel
      AZURE_OPENAI_MODEL_NAME: openAIModelName
      AZURE_OPENAI_KEY: openAi.outputs.key
      AZURE_OPENAI_TEMPERATURE: openAITemperature
      AZURE_OPENAI_TOP_P: openAITopP
      AZURE_OPENAI_MAX_TOKENS: openAIMaxTokens
      AZURE_OPENAI_STOP_SEQUENCE: openAIStopSequence
      AZURE_OPENAI_SYSTEM_MESSAGE: openAISystemMessage
      AZURE_OPENAI_STREAM: openAIStream
    }
  }
}
module appServicePlanApi './core/host/appserviceplan.bicep' = {
  name: 'appserviceplanApi'
  scope: resourceGroup
  params: {
    name: !empty(appServicePlanName) ? appServicePlanName : '${abbrs.webServerFarms}${resourceToken}'
    location: location
    tags: tags
    sku: {
      name: 'Y1'
      tier: 'Dynamic'
    }
  }
}

// The application backend
module function './app/function.bicep' = {
  name: 'function'
  scope: resourceGroup
  params: {
    name: !empty(functionServiceName) ? functionServiceName : '${abbrs.webSitesFunctions}function-${resourceToken}'
    location: location
    tags: tags
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    appServicePlanId: appServicePlan.outputs.id
    storageAccountName: storage.outputs.name
    allowedOrigins: [ backend.outputs.uri ]
    useManagedIdentity: true
    appSettings: {
      AzureOpenAiEmbeddingDeployedModel: ''
      AzureOpenAiEndpoint: ''
      AzureWebJobsStorage: ''
      DocIntelEndPoint: ''
      FUNCTIONS_WORKER_RUNTIME: 'dotnet-isolated'
      IncomingBlobConnStr: ''
      ModelDimensions: ''
      SearchEndpoint: ''
      ProjectPrefix: 'damu'
      NoteJsonFileName: '' // expects ndjson format,
      FHIR_SERVER_URL: ''
      TenantId: ''
      ClientId: ''
      ClientSecret: ''
      Resource: ''
      // the following are optional - complete them if you want to use key base auth, leave blank for managed identity
      SearchKey: ''
      AzureOpenAiKey: ''
      DocIntelKey: ''
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
        name: openAIModel
        model: {
          format: 'OpenAI'
          name: openAIModelName
          version: '2024-05-13'
        }
        capacity: 30
      }
      {
        name: embeddingDeploymentName
        model: {
          format: 'OpenAI'
          name: embeddingModelName
          version: '2'
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

// The application database
module cosmos 'db.bicep' = {
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
    principalType: 'User'
  }
}

module storageRoleFunctionApp 'core/security/role.bicep' = {
  scope: storageResourceGroup
  name: 'storage-role-functionapp'
  params: {
    principalId: function.outputs.SERVICE_FUNCTION_IDENTITY_PRINCIPAL_ID
    roleDefinitionId: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
    principalType: 'User'
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
output AZURE_SEARCH_USE_SEMANTIC_SEARCH bool = searchUseSemanticSearch
output AZURE_SEARCH_SEMANTIC_SEARCH_CONFIG string = searchSemanticSearchConfig
output AZURE_SEARCH_TOP_K int = searchTopK
output AZURE_SEARCH_ENABLE_IN_DOMAIN bool = searchEnableInDomain
output AZURE_SEARCH_CONTENT_COLUMNS string = searchContentColumns
output AZURE_SEARCH_FILENAME_COLUMN string = searchFilenameColumn
output AZURE_SEARCH_TITLE_COLUMN string = searchTitleColumn
output AZURE_SEARCH_URL_COLUMN string = searchUrlColumn

// openai
output AZURE_OPENAI_RESOURCE string = openAi.outputs.name
output AZURE_OPENAI_RESOURCE_GROUP string = openAiResourceGroup.name
output AZURE_OPENAI_ENDPOINT string = openAi.outputs.endpoint
output AZURE_OPENAI_MODEL string = openAIModel
output AZURE_OPENAI_MODEL_NAME string = openAIModelName
output AZURE_OPENAI_SKU_NAME string = openAi.outputs.skuName
output AZURE_OPENAI_KEY string = openAi.outputs.key
output AZURE_OPENAI_EMBEDDING_NAME string = embeddingDeploymentName
output AZURE_OPENAI_TEMPERATURE int = openAITemperature
output AZURE_OPENAI_TOP_P int = openAITopP
output AZURE_OPENAI_MAX_TOKENS int = openAIMaxTokens
output AZURE_OPENAI_STOP_SEQUENCE string = openAIStopSequence
output AZURE_OPENAI_SYSTEM_MESSAGE string = openAISystemMessage
output AZURE_OPENAI_STREAM bool = openAIStream

// cosmos
output AZURE_COSMOSDB_ACCOUNT string = cosmos.outputs.accountName
output AZURE_COSMOSDB_DATABASE string = cosmos.outputs.databaseName
output AZURE_COSMOSDB_CONVERSATIONS_CONTAINER string = cosmos.outputs.containerName

output AUTH_ISSUER_URI string = authIssuerUri
