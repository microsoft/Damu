param appServiceName string
param location string = resourceGroup().location
param tags object = {}
param appServicePlanName string
param authClientId string
@secure()
param authClientSecret string
param authIssuerUri string
param storageAccountName string
param serviceName string = 'backend'
param appSettings object = {}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

module appServicePlan '../core/host/appserviceplan.bicep' = {
  name: 'appserviceplan-${serviceName}'
  params: {
    name: appServicePlanName
    location: location
    tags: tags
    sku: {
      name: 'B1'
      capacity: 1
    }
    kind: 'windows'
  }
}

module backend '../core/host/appservice.bicep' = {
  name: 'web-${serviceName}'
  params: {
    name: appServiceName
    location: location
    tags: union(tags, { 'azd-service-name': serviceName })
    appServicePlanId: appServicePlan.outputs.id
    runtimeName: 'dotnet'
    runtimeVersion: '8'
    scmDoBuildDuringDeployment: true
    managedIdentity: true
    authClientSecret: authClientSecret
    authClientId: authClientId
    authIssuerUri: authIssuerUri
    appSettings: union(appSettings, {
      StorageOptions__BlobStorageConnectionString: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value}'
    })
  }
}

output uri string = backend.outputs.uri
output identityPrincipalId string = backend.outputs.identityPrincipalId
