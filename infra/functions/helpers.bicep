//=============================================================================
// Helper functions to construct URLs, Key Vault references, etc.
//=============================================================================

// API Management functions

@export()
func getApiManagementFqdn(apimServiceName string) string => '${apimServiceName}.azure-api.net'
