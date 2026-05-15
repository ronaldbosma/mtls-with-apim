//=============================================================================
// Application Gateway
//=============================================================================

//=============================================================================
// Imports
//=============================================================================

import { applicationGatewaySettingsType } from '../../99-shared/settings.bicep'
import { getApiManagementFqdn } from '../../99-shared/helpers.bicep'

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

@description('The name of the App Insights instance to use')
param appInsightsName string

@description('The name of the Key Vault that contains the secrets and certificates')
param keyVaultName string

@description('The name of the Log Analytics workspace to use')
param logAnalyticsWorkspaceName string

//=============================================================================
// Variables
//=============================================================================

var applicationGatewayName string = applicationGatewaySettings.applicationGatewayName

//=============================================================================
// Existing Resources
//=============================================================================

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2025-07-01' existing = {
  name: logAnalyticsWorkspaceName
}

resource agwPublicIPAddress 'Microsoft.Network/publicIPAddresses@2025-05-01' existing = {
  name: applicationGatewaySettings.publicIpAddressName
}

resource keyVault 'Microsoft.KeyVault/vaults@2025-05-01' existing = {
  name: keyVaultName
}

resource sslServerCertificateSecret 'Microsoft.KeyVault/vaults/secrets@2025-05-01' existing = {
  name: 'agw-ssl-server-certificate'
  parent: keyVault
}

//=============================================================================
// Resources
//=============================================================================

// Create user-assigned identity for Application Gateway and assign roles to it

resource agwIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: applicationGatewaySettings.identityName
  location: location
  tags: tags
}

module assignRolesToAgwUserAssignedIdentity '../../99-shared/assign-roles-to-principal.bicep' = {
  params: {
    principalId: agwIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    appInsightsName: appInsightsName
    keyVaultName: keyVaultName
  }
}

// Application Gateway

resource applicationGateway 'Microsoft.Network/applicationGateways@2025-05-01' = {
  name: applicationGatewayName
  location: location
  tags: tags

  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${agwIdentity.id}': {}
    }
  }

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
        name: 'port-https'
        properties: {
          port: 443
        }
      }
    ]

    sslCertificates: [
      {
        name: 'agw-ssl-certificate'
        properties: {
          keyVaultSecretId: sslServerCertificateSecret.properties.secretUri
        }
      }
    ]

    httpListeners: [
      {
        name: 'https-listener'
        properties: {
          protocol: 'Https'
          hostName: 'agw-sample.dev'
          frontendIPConfiguration: {
            id: resourceId(
              'Microsoft.Network/applicationGateways/frontendIPConfigurations',
              applicationGatewayName,
              'agw-public-frontend-ip'
            )
          }
          frontendPort: {
            id: resourceId('Microsoft.Network/applicationGateways/frontendPorts', applicationGatewayName, 'port-https')
          }
          sslCertificate: {
            id: resourceId(
              'Microsoft.Network/applicationGateways/sslCertificates',
              applicationGatewayName,
              'agw-ssl-certificate'
            )
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
          hostName: getApiManagementFqdn(apiManagementServiceName)
          requestTimeout: 20
          probe: {
            id: resourceId('Microsoft.Network/applicationGateways/probes', applicationGatewayName, 'apim-gateway-probe')
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
            id: resourceId(
              'Microsoft.Network/applicationGateways/httpListeners',
              applicationGatewayName,
              'https-listener'
            )
          }
          backendAddressPool: {
            id: resourceId(
              'Microsoft.Network/applicationGateways/backendAddressPools',
              applicationGatewayName,
              'apim-gateway-backend-pool'
            )
          }
          backendHttpSettings: {
            id: resourceId(
              'Microsoft.Network/applicationGateways/backendHttpSettingsCollection',
              applicationGatewayName,
              'apim-gateway-backend-settings'
            )
          }
        }
      }
    ]
  }
}

// Diagnostic settings for Application Gateway

#disable-next-line use-recent-api-versions // There isn't a newer version at the moment
resource applicationGatewayDiagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${applicationGatewayName}-diagnostics'
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
