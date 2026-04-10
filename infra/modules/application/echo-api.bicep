//=============================================================================
// Echo API in API Management
//=============================================================================

//=============================================================================
// Parameters
//=============================================================================

@description('The name of the API Management service')
param apiManagementServiceName string

//=============================================================================
// Existing resources
//=============================================================================

resource apiManagementService 'Microsoft.ApiManagement/service@2025-03-01-preview' existing = {
  name: apiManagementServiceName
}

//=============================================================================
// Resources
//=============================================================================

// Echo API

resource echoApi 'Microsoft.ApiManagement/service/apis@2025-03-01-preview' = {
  name: 'echo-api'
  parent: apiManagementService
  properties: {
    displayName: 'Echo API'
    path: 'echo'
    protocols: [
      'https'
    ]
    subscriptionRequired: true
  }

  // Create a GET operation
  resource operations 'operations' = {
    name: 'get'
    properties: {
      displayName: 'Get'
      method: 'GET'
      urlTemplate: '/'
    }

    resource policies 'policies' = {
      name: 'policy'
      properties: {
        format: 'rawxml'
        value: loadTextContent('echo-api.get.xml')
      }
    }
  }
}
