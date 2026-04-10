//=============================================================================
// Protected API in API Management
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

resource protectedApi 'Microsoft.ApiManagement/service/apis@2025-03-01-preview' = {
  name: 'protected-api'
  parent: apiManagementService
  properties: {
    displayName: 'Protected API'
    path: 'protected'
    protocols: [
      'https'
    ]
    subscriptionRequired: false // API is protected with mTLS
  }
}

// Operation to validate client certificate using validate-client-certificate policy
resource validateUsingPolicy 'Microsoft.ApiManagement/service/apis/operations@2025-03-01-preview' = {
  name: 'validate-using-policy'
  parent: protectedApi
  properties: {
    displayName: 'Validate (using policy)'
    description: 'Validates client certificate using validate-client-certificate policy'
    method: 'GET'
    urlTemplate: '/validate-using-policy'
  }

  resource policies 'policies' = {
    name: 'policy'
    properties: {
      format: 'rawxml'
      value: loadTextContent('./validate-using-policy.operation.xml')
    }
  }
}

// Operation to validate client certificate using context.Request.Certificate property
resource validateUsingContext 'Microsoft.ApiManagement/service/apis/operations@2025-03-01-preview' = {
  name: 'validate-using-context'
  parent: protectedApi
  properties: {
    displayName: 'Validate (using context)'
    description: 'Validates client certificate using the context.Request.Certificate property'
    method: 'GET'
    urlTemplate: '/validate-using-context'
  }

  resource policies 'policies' = {
    name: 'policy'
    properties: {
      format: 'rawxml'
      value: loadTextContent('./validate-using-context.operation.xml')
    }
  }
}
