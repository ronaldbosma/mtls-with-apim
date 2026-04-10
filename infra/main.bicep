//=============================================================================
// mTLS with API Management
// Source: https://github.com/ronaldbosma/mtls-with-apim
//=============================================================================

targetScope = 'subscription'

//=============================================================================
// Imports
//=============================================================================

import { getResourceName, generateInstanceId } from './functions/naming-conventions.bicep'
import {
  apiManagementSettingsType
  appInsightsSettingsType
  applicationGatewaySettingsType
  virtualNetworkSettingsType
} from './types/settings.bicep'

//=============================================================================
// Parameters
//=============================================================================

@minLength(1)
@description('Location to use for all resources')
param location string

@minLength(1)
@maxLength(32)
@description('The name of the environment to deploy to')
param environmentName string

//=============================================================================
// Variables
//=============================================================================

// Generate an instance ID to ensure unique resource names
var instanceId string = generateInstanceId(environmentName, location)

var resourceGroupName string = getResourceName('resourceGroup', environmentName, location, instanceId)

var apiManagementSettings apiManagementSettingsType = {
  serviceName: getResourceName('apiManagement', environmentName, location, instanceId)
  sku: 'BasicV2' // BasicV2 is used because the Consumption tier does not support CA certificates.
}

var appInsightsSettings appInsightsSettingsType = {
  appInsightsName: getResourceName('applicationInsights', environmentName, location, instanceId)
  logAnalyticsWorkspaceName: getResourceName('logAnalyticsWorkspace', environmentName, location, instanceId)
  retentionInDays: 30
}

var applicationGatewaySettings applicationGatewaySettingsType = {
  applicationGatewayName: getResourceName('applicationGateway', environmentName, location, instanceId)
  publicIpAddressName: getResourceName('publicIpAddress', environmentName, location, instanceId)
  wafPolicyName: getResourceName('webApplicationFirewallPolicy', environmentName, location, instanceId)
}

var keyVaultName string = getResourceName('keyVault', environmentName, location, instanceId)

var virtualNetworkSettings virtualNetworkSettingsType = {
  virtualNetworkName: getResourceName('virtualNetwork', environmentName, location, instanceId)
  applicationGatewaySubnetName: getResourceName('subnet', environmentName, location, 'agw-${instanceId}')
}

var tags { *: string } = {
  'azd-env-name': environmentName
  'azd-template': 'ronaldbosma/mtls-with-apim'
}

//=============================================================================
// Resources
//=============================================================================

resource resourceGroup 'Microsoft.Resources/resourceGroups@2024-11-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

module appInsights 'modules/services/app-insights.bicep' = {
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    appInsightsSettings: appInsightsSettings
  }
}

module virtualNetwork 'modules/services/virtual-network.bicep' = {
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    virtualNetworkSettings: virtualNetworkSettings
  }
}

module apiManagement 'modules/services/api-management.bicep' = {
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    apiManagementSettings: apiManagementSettings
    appInsightsSettings: appInsightsSettings
    keyVaultName: keyVaultName
  }
  dependsOn: [
    appInsights
  ]
}

module appGateway './modules/services/application-gateway.bicep' = {
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    applicationGatewaySettings: applicationGatewaySettings
    subnetId: virtualNetwork.outputs.agwSubnetId
    apiManagementServiceName: apiManagementSettings.serviceName
    appInsightsSettings: appInsightsSettings
  }
}

module keyVault 'modules/services/key-vault.bicep' = {
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    keyVaultName: keyVaultName
  }
}

module assignRolesToDeployer 'modules/shared/assign-roles-to-principal.bicep' = {
  scope: resourceGroup
  params: {
    principalId: deployer().objectId
    isAdmin: true
    appInsightsName: appInsightsSettings.appInsightsName
    keyVaultName: keyVaultName
  }
  dependsOn: [
    appInsights
    keyVault
  ]
}

//=============================================================================
// Application Resources
//=============================================================================

module echoApi 'modules/application/echo-api.bicep' = {
  scope: resourceGroup
  params: {
    apiManagementServiceName: apiManagementSettings.serviceName
  }
  dependsOn: [
    apiManagement
  ]
}

//=============================================================================
// Outputs
//=============================================================================

// Return the names of the resources
output AZURE_API_MANAGEMENT_NAME string = apiManagementSettings.serviceName
output AZURE_APPLICATION_GATEWAY_NAME string = applicationGatewaySettings.applicationGatewayName
output AZURE_APPLICATION_INSIGHTS_NAME string = appInsightsSettings.appInsightsName
output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = appInsightsSettings.logAnalyticsWorkspaceName
output AZURE_RESOURCE_GROUP string = resourceGroupName

// Return resource endpoints
output AZURE_API_MANAGEMENT_GATEWAY_URL string = apiManagement.outputs.gatewayUrl
output AZURE_KEY_VAULT_URI string = keyVault.outputs.vaultUri
output AZURE_APPLICATION_GATEWAY_PUBLIC_IP_ADDRESS string = appGateway.outputs.publicIpAddress
