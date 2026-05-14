param(
    [Parameter(Mandatory = $true)][SecureString]$CertificatePassword,
    [int]$CertificateExpirationInMonths = 600
)


# =====================================================================
# Settings
# =====================================================================

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest


$currentScriptPath = $MyInvocation.MyCommand.Path | Split-Path -Parent
$exportPath = "$currentScriptPath/certificates"


$certificateTree = @{
    Subject         = "CN=APIM Sample Root CA"
    Id              = "root-ca"
    ExpiresInMonths = $CertificateExpirationInMonths
    Intermediates   = @(
        @{
            Subject         = "CN=APIM Sample DEV Intermediate CA"
            Id              = "dev-intermediate-ca"
            ExpiresInMonths = $CertificateExpirationInMonths
            Clients         = @(
                @{ Subject = "CN=Valid Client"; Id = "dev-valid-client"; DnsName = "Valid Client"; StartsAfterMonths = 0; ExpiresInMonths = $CertificateExpirationInMonths },
                @{ Subject = "CN=Unregistered Client"; Id = "dev-unregistered-client"; DnsName = "Unregistered Client"; StartsAfterMonths = 0; ExpiresInMonths = $CertificateExpirationInMonths },
                @{ Subject = "CN=Unprotected API"; Id = "dev-unprotected-api"; DnsName = "Unprotected API"; StartsAfterMonths = 0; ExpiresInMonths = $CertificateExpirationInMonths },

                # Create a client certificate that is not yet valid by setting the NotBefore date in the future. This can be used to test the NotBefore validation in APIM.
                @{ Subject = "CN=Not Yet Valid Client"; Id = "dev-notyetvalid-client"; DnsName = "Not Yet Valid Client"; StartsAfterMonths = $CertificateExpirationInMonths - 1; ExpiresInMonths = $CertificateExpirationInMonths },
                # Create an expired client certificate by setting the NotAfter date in the past. This can be used to test the NotAfter validation in APIM.
                @{ Subject = "CN=Expired Client"; Id = "dev-expired-client"; DnsName = "Expired"; StartsAfterMonths = 0; ExpiresInMonths = 0 }
            )
        },
        @{
            Subject         = "CN=APIM Sample TST Intermediate CA"
            Id              = "tst-intermediate-ca"
            ExpiresInMonths = $CertificateExpirationInMonths
            Clients         = @(
                @{ Subject = "CN=Untrusted Client"; Id = "tst-untrusted-client"; DnsName = "Untrusted Client"; StartsAfterMonths = 0; ExpiresInMonths = $CertificateExpirationInMonths }
            )
        }
    )
}


# =====================================================================
# Load functions
# =====================================================================

. $currentScriptPath/generate-client-certificates.functions.ps1


# =====================================================================
# Generate self-signed certificates
# =====================================================================

$rootCACert = New-SelfSignedRootCACertificate -Subject $certificateTree.Subject -ExpiresInMonths $certificateTree.ExpiresInMonths

$intermediateCACerts = @{}
$clientCerts = @{}

foreach ($intermediate in $certificateTree.Intermediates) {
    $intermediateCert = New-SelfSignedIntermediateCACertificate -Subject $intermediate.Subject -Signer $rootCACert -ExpiresInMonths $intermediate.ExpiresInMonths
    $intermediateCACerts[$intermediate.Id] = $intermediateCert

    foreach ($client in $intermediate.Clients) {
        $dnsName = if ($client.ContainsKey('DnsName')) { $client.DnsName } else { $client.Subject }
        $clientCert = New-SelfSignedClientCertificate -Subject $client.Subject -DnsName $dnsName -Signer $intermediateCert -ExpiresInMonths $client.ExpiresInMonths -StartsAfterMonths $client.StartsAfterMonths
        $clientCerts[$client.Id] = $clientCert
    }
}


# =====================================================================
# Export self-signed certificates
# =====================================================================

if (-not(Test-Path -Path $exportPath)) {
    New-Item -Path $exportPath -ItemType Directory | Out-Null
}

# Export the root CA certificate without private key as base64 encoded X.509 (.cer) files

Export-CertificateAsBase64 -Certificate $rootCACert -OutputFilePath "$exportPath\$($certificateTree.Id).cer"
Export-CertificateAsBase64 -Certificate $rootCACert -OutputFilePath "$exportPath\$($certificateTree.Id).without-markers.cer" -ExcludeMarkers

foreach ($intermediate in $certificateTree.Intermediates) {
    $intermediateCert = $intermediateCACerts[$intermediate.Id]

    # Export the intermediate CA certificate without private key as base64 encoded X.509 (.cer) files
    Export-CertificateAsBase64 -Certificate $intermediateCert -OutputFilePath "$exportPath\$($intermediate.Id).cer"
    Export-CertificateAsBase64 -Certificate $intermediateCert -OutputFilePath "$exportPath\$($intermediate.Id).without-markers.cer" -ExcludeMarkers

    foreach ($client in $intermediate.Clients) {
        $clientCert = $clientCerts[$client.Id]

        # Export the client certificate without private key as base64 encoded X.509 (.cer) files
        Export-CertificateAsBase64 -Certificate $clientCert -OutputFilePath "$exportPath\$($client.Id).cer"
        Export-CertificateAsBase64 -Certificate $clientCert -OutputFilePath "$exportPath\$($client.Id).without-markers.cer" -ExcludeMarkers

        # Export the client certificate with private key as .pfx file
        Export-PfxCertificate -Cert $clientCert -FilePath "$exportPath\$($client.Id).pfx" -Password $CertificatePassword
    }
}


# =====================================================================
# Combine the base64 encoded X.509 (.cer) files into one file
# =====================================================================

# All (CA) certificates in a certificate chain need to be combined when uploading them in Azure Application Gateway

foreach ($intermediate in $certificateTree.Intermediates) {
    Merge-Base64CertificateFiles -InputFilePaths @( "$exportPath\$($intermediate.Id).cer", "$exportPath\$($certificateTree.Id).cer" ) `
        -OutputFilePath "$exportPath\$($intermediate.Id)-with-$($certificateTree.Id).cer"
}
