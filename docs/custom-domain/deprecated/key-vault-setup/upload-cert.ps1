param (
    [Parameter(Mandatory=$true)]
    [string]
    $pfxFilePath,

    [Parameter(Mandatory=$true)]
    [string]
    $pfxPwFile,

    [Parameter(Mandatory=$true)]
    [string]
    $secretName,

    [Parameter(Mandatory=$true)]
    [string]
    $vaultName
)

$pfxPw = [IO.File]::ReadAllText($pfxPwFile)
$pfxContent = get-content $pfxFilePath -Encoding Byte
$pfxContentEncoded = [System.Convert]::ToBase64String($pfxContent)

$certBundleObj = @"
{
"data": "$pfxContentEncoded",
"dataType" :"pfx",
"password": "$pfxPw"
}
"@

$bundleObjBytes = [System.Text.Encoding]::UTF8.GetBytes($certBundleObj)
$bundleObjEncoded = [System.Convert]::ToBase64String($bundleObjBytes)
$secretValue = ConvertTo-SecureString -String $bundleObjEncoded -AsPlainText -Force

Set-AzureKeyVaultSecret -Name $secretName -SecretValue $secretValue -VaultName $vaultName
