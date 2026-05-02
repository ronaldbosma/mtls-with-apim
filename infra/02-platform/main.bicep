//=============================================================================
// mTLS with API Management - Platform layer
//=============================================================================

targetScope = 'subscription'

//=============================================================================
// Imports
//=============================================================================

import { getResourceName } from '../99-shared/naming-conventions.bicep'
import { getTemplateTags } from '../99-shared/helpers.bicep'
import { apiManagementSettingsType, applicationGatewaySettingsType, virtualNetworkSettingsType } from '../99-shared/settings.bicep'

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

@description('The ID of the environment instance')
param instanceId string

@description('The name of the resource group in which to deploy the resources.')
param resourceGroupName string

@description('The name of the public IP address for the Application Gateway')
param agwPublicIpAddressName string

@description('The name of the App Insights instance to use')
param appInsightsName string

@description('The name of the Key Vault that will contain the secrets')
param keyVaultName string

@description('The name of the Log Analytics workspace to use')
param logAnalyticsWorkspaceName string

//=============================================================================
// Variables
//=============================================================================

var apiManagementSettings apiManagementSettingsType = {
  serviceName: getResourceName('apiManagement', environmentName, location, instanceId)
  sku: 'BasicV2' // BasicV2 is used because the Consumption tier does not support CA certificates.
}

var applicationGatewaySettings applicationGatewaySettingsType = {
  applicationGatewayName: getResourceName('applicationGateway', environmentName, location, instanceId)
  publicIpAddressName: agwPublicIpAddressName
  wafPolicyName: getResourceName('webApplicationFirewallPolicy', environmentName, location, instanceId)
}
var virtualNetworkSettings virtualNetworkSettingsType = {
  virtualNetworkName: getResourceName('virtualNetwork', environmentName, location, instanceId)
  applicationGatewaySubnetName: getResourceName('subnet', environmentName, location, 'agw-${instanceId}')
}

var tags { *: string } = getTemplateTags(environmentName)

//=============================================================================
// Existing Resources
//=============================================================================

resource resourceGroup 'Microsoft.Resources/resourceGroups@2025-04-01' existing = {
  name: resourceGroupName
}

//=============================================================================
// Resources
//=============================================================================

module apiManagement 'modules/api-management.bicep' = {
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    apiManagementSettings: apiManagementSettings
    appInsightsName: appInsightsName
    keyVaultName: keyVaultName
  }
}

module virtualNetwork 'modules/virtual-network.bicep' = {
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    virtualNetworkSettings: virtualNetworkSettings
  }
}

module appGateway 'modules/application-gateway.bicep' = {
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    applicationGatewaySettings: applicationGatewaySettings
    subnetId: virtualNetwork.outputs.agwSubnetId
    apiManagementServiceName: apiManagementSettings.serviceName
    logAnalyticsWorkspaceName: logAnalyticsWorkspaceName
  }
}

//=============================================================================
// Outputs
//=============================================================================

// Return the names of the resources
output AZURE_API_MANAGEMENT_NAME string = apiManagementSettings.serviceName
output AZURE_APPLICATION_GATEWAY_NAME string = applicationGatewaySettings.applicationGatewayName

// Return resource endpoints
output AZURE_API_MANAGEMENT_GATEWAY_URL string = apiManagement.outputs.gatewayUrl
