//=============================================================================
// Application Gateway
//=============================================================================

//=============================================================================
// Imports
//=============================================================================

import { applicationGatewaySettingsType, appInsightsSettingsType } from '../../types/settings.bicep'
import { getApiManagementFqdn } from '../../functions/helpers.bicep'

//=============================================================================
// Parameters
//=============================================================================

@description('Location to use for all resources')
param location string = resourceGroup().location

@description('The tags to associate with the resource')
param tags object

@description('The settings for the Application Gateway')
param applicationGatewaySettings applicationGatewaySettingsType

@description('The ID of the subnet to use for the API Management service')
param subnetId string

@description('The name of the API Management Service to use')
param apiManagementServiceName string

@description('The settings for App Insights')
param appInsightsSettings appInsightsSettingsType

//=============================================================================
// Existing Resources
//=============================================================================

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2025-07-01' existing = {
  name: appInsightsSettings.logAnalyticsWorkspaceName
}

//=============================================================================
// Resources
//=============================================================================

// Public IP address

resource agwPublicIPAddress 'Microsoft.Network/publicIPAddresses@2024-10-01' = {
  name: applicationGatewaySettings.publicIpAddressName
  location: location
  tags: tags
  sku: {
    name: 'Standard'
  }
  properties: {
    publicIPAddressVersion: 'IPv4'
    publicIPAllocationMethod: 'Static'
    idleTimeoutInMinutes: 4
  }
}

// Application Gateway

resource applicationGateway 'Microsoft.Network/applicationGateways@2024-10-01' = {
  name: applicationGatewaySettings.applicationGatewayName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'Standard_v2'
      tier: 'Standard_v2'
    }
    enableHttp2: false
    autoscaleConfiguration: {
      minCapacity: 0
      maxCapacity: 2
    }

    gatewayIPConfigurations: [
      {
        name: 'agw-subnet-ip-config'
        properties: {
          subnet: {
            id: subnetId
          }
        }
      }
    ]

    // Frontend

    frontendIPConfigurations: [
      {
        name: 'agw-public-frontend-ip'
        properties: {
          publicIPAddress: {
            id: agwPublicIPAddress.id
          }
        }
      }
    ]

    frontendPorts: [
      {
        name: 'port-http'
        properties: {
          port: 80
        }
      }
    ]

    httpListeners: [
      {
        name: 'http-listener'
        properties: {
          protocol: 'Http'
          // requireServerNameIndication: false
          frontendIPConfiguration: {
            id: resourceId('Microsoft.Network/applicationGateways/frontendIPConfigurations', applicationGatewaySettings.applicationGatewayName, 'agw-public-frontend-ip')
          }
          frontendPort: {
            id: resourceId('Microsoft.Network/applicationGateways/frontendPorts', applicationGatewaySettings.applicationGatewayName, 'port-http')
          }
        }
      }
    ]

    // Backend

    backendAddressPools: [
      {
        name: 'apim-gateway-backend-pool'
        properties: {
          backendAddresses: [
            {
              fqdn: getApiManagementFqdn(apiManagementServiceName)
            }
          ]
        }
      }
    ]

    probes: [
      {
        name: 'apim-gateway-probe'
        properties: {
          pickHostNameFromBackendHttpSettings: true
          interval: 30
          timeout: 30
          path: '/status-0123456789abcdef'
          protocol: 'Https'
          unhealthyThreshold: 3
          match: {
            statusCodes: [
              '200-399'
            ]
          }
        }
      }
    ]

    backendHttpSettingsCollection: [
      {
        name: 'apim-gateway-backend-settings'
        properties: {
          port: 443
          protocol: 'Https'
          cookieBasedAffinity: 'Disabled'
          hostName: '${apiManagementServiceName}.azure-api.net'
          requestTimeout: 20
          probe: {
            id: resourceId('Microsoft.Network/applicationGateways/probes', applicationGatewaySettings.applicationGatewayName, 'apim-gateway-probe')
          }
        }
      }
    ]

    // Rules

    requestRoutingRules: [
      {
        name: 'apim-routing-rule'
        properties: {
          priority: 10
          ruleType: 'Basic'
          httpListener: {
            id: resourceId('Microsoft.Network/applicationGateways/httpListeners', applicationGatewaySettings.applicationGatewayName, 'http-listener')
          }
          backendAddressPool: {
            id: resourceId('Microsoft.Network/applicationGateways/backendAddressPools', applicationGatewaySettings.applicationGatewayName, 'apim-gateway-backend-pool')
          }
          backendHttpSettings: {
            id: resourceId('Microsoft.Network/applicationGateways/backendHttpSettingsCollection', applicationGatewaySettings.applicationGatewayName, 'apim-gateway-backend-settings')
          }
        }
      }
    ]
  }
}

// Diagnostic settings for Application Gateway

#disable-next-line use-recent-api-versions // There isn't a newer version at the moment
resource applicationGatewayDiagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${applicationGatewaySettings.applicationGatewayName}-diagnostics'
  scope: applicationGateway
  properties: {
    workspaceId: logAnalyticsWorkspace.id
    logs: [
      {
        categoryGroup: 'AllLogs'
        enabled: true
      }
    ]
  }
}

//=============================================================================
// Outputs
//=============================================================================

output publicIpAddress string = agwPublicIPAddress.properties.ipAddress
