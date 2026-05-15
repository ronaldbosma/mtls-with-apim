<#
  This PowerShell script is executed after the core layer is provisioned.
  It will import certificates into Azure Key Vault.
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$SubscriptionId = $env:AZURE_SUBSCRIPTION_ID,

    [Parameter(Mandatory = $false)]
    [string]$KeyVaultName = $env:AZURE_KEY_VAULT_NAME
)

# Validate required parameters
if ([string]::IsNullOrEmpty($SubscriptionId)) {
    throw "SubscriptionId parameter is required. Please provide it as a parameter or set the AZURE_SUBSCRIPTION_ID environment variable."
}

if ([string]::IsNullOrEmpty($KeyVaultName)) {
    throw "KeyVaultName parameter is required. Please provide it as a parameter or set the AZURE_KEY_VAULT_NAME environment variable."
}


# First, ensure the Azure CLI is logged in and set to the correct subscription
az account set --subscription $SubscriptionId
if ($LASTEXITCODE -ne 0) {
    throw "Unable to set the Azure subscription. Please make sure that you're logged into the Azure CLI with the same credentials as the Azure Developer CLI."
}


$currentScriptPath = $MyInvocation.MyCommand.Path | Split-Path -Parent
$certificatesPath = Join-Path $currentScriptPath "..\..\..\self-signed-certificates\certificates"


# DISCLAIMER: Hardcoding passwords is only acceptable for local development and demo purposes.
# In real-world scenarios, never hardcode passwords or commit certificates with private keys to source control.
# Use proper secret/certificate management solutions instead.
$certificatePassword = "P@ssw0rd"


# =====================================================================
# Import certificates into Azure Key Vault
# =====================================================================

# List of certificates to import as certificates (excluding expired)
$certificatesToImport = @(
    "dev-notyetvalid-client.pfx",
    "dev-unprotected-api.pfx",
    "dev-unregistered-client.pfx",
    "dev-valid-client.pfx",
    "tst-untrusted-client.pfx"
)

foreach ($certificateFileName in $certificatesToImport) {
    $certificateName = [System.IO.Path]::GetFileNameWithoutExtension($certificateFileName)
    $certificateFilePath = Join-Path $certificatesPath $certificateFileName

    Write-Host "Importing certificate '$certificateName' from '$certificateFilePath' into Azure Key Vault '$KeyVaultName'"

    az keyvault certificate import `
        --file $certificateFilePath `
        --name $certificateName `
        --vault-name $KeyVaultName `
        --password $certificatePassword `
        --output none

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to import certificate '$certificateName' into Key Vault '$KeyVaultName'."
    }
}


# =====================================================================
# Store expired certificates into Azure Key Vault as secrets
# (importing expired certificates in Key Vault fails)
# =====================================================================

$expiredCertFileName = "dev-expired-client.pfx"
$expiredCertName = [System.IO.Path]::GetFileNameWithoutExtension($expiredCertFileName)
$expiredCertFilePath = Join-Path $certificatesPath $expiredCertFileName

Write-Host "Storing expired certificate '$expiredCertName' from '$expiredCertFilePath' as a plain secret in Azure Key Vault '$KeyVaultName'"

$expiredCertBytes = [System.IO.File]::ReadAllBytes($expiredCertFilePath)
$expiredCertBase64 = [Convert]::ToBase64String($expiredCertBytes)

az keyvault secret set `
    --vault-name $KeyVaultName `
    --name $expiredCertName `
    --value $expiredCertBase64 `
    --output none

if ($LASTEXITCODE -ne 0) {
    throw "Failed to store expired certificate '$expiredCertName' as a secret in Key Vault '$KeyVaultName'."
}
