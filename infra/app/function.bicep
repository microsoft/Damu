param name string
param location string = resourceGroup().location
param tags object = {}

param allowedOrigins array = []
param applicationInsightsName string = ''
param appServicePlanName string
@secure()
param appSettings object = {}
param serviceName string = 'index-function'
param storageAccountName string
param useManagedIdentity bool

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

module appServicePlanFunction '../core/host/appserviceplan.bicep' = {
  name: 'appserviceplanApi'
  params: {
    name: appServicePlanName
    location: location
    tags: tags
    sku: {
      name: 'Y1'
      tier: 'Dynamic'
    }
  }
}

module function '../core/host/functions.bicep' = {
  name: '${serviceName}-function'
  params: {
    name: name
    location: location
    tags: union(tags, { 'azd-service-name': serviceName })
    allowedOrigins: allowedOrigins
    alwaysOn: false
    appSettings: union(appSettings, {
      IncomingBlobConnStr: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value}'
    })
    applicationInsightsName: applicationInsightsName
    appServicePlanId: appServicePlanFunction.outputs.id
    runtimeName: 'dotnet-isolated'
    runtimeVersion: '8.0'
    storageAccountName: storageAccountName
    scmDoBuildDuringDeployment: false
    managedIdentity: useManagedIdentity
  }
}

output SERVICE_FUNCTION_IDENTITY_PRINCIPAL_ID string = function.outputs.identityPrincipalId
output SERVICE_FUNCTION_NAME string = function.outputs.name
output SERVICE_FUNCTION_URI string = function.outputs.uri
