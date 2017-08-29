# How to use a custom domain for azure container registry

Azure Container registries has a typical login url of the format `*.azurecr.io`. A customer might like to have a custom domain that associate with its own organization. The following is the guide on how to achieve that.

## Prerequisites

For this example, we suppose that you want to associate `registry.contoso.com` with a Azure Container Registry. You would need the following:

* Setup your organization's DNS zone `.contoso.com`. To create one on Azure, you can follow [this guide](https://docs.microsoft.com/en-us/azure/dns/dns-getstarted-create-dnszone-portal)
* SSL certificate for `registry.contoso.com`, we would call it `contoso.pfx`. Put the password of the certificate to a file named `pwd.txt`. You would optionally also need your signing CA certificate's URL, such as `http://www.contoso.com/pki/ca.cert`
* An instance of Azure Container Registry service as the backend. In this example we would assume it's `docker-registry-contoso.azurecr.io`

## Steps

### Upload your cert into Azure Key Vault

Under [key-vault-setup/](key-vault-setup/), run the following:

1. (Optional) Create an Azure Key Vault, if you don't already have one:

        `.\ensure-vault.ps1 -subscriptionName <subscription> -resourceGroupName <resourceGroup> -vaultName <new VaultName>`

2. Upload `contoso.pfx` to Azure Key Vault:

        `.\upload-cert.ps1 -pfxFilePath <pfxFile> -pfxPwFile <pwdFile> -secretName <new SecretName> -vaultName <vaultName>`

### Deploy and configure an Nginx Docker image on a new Azure VM

Deploy via Azure Portal

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure%2Facr%2Fmaster%2Fdocs%2Fcustom-domain%2Fdocker-vm-deploy%2Fazuredeploy.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>
<a href="http://armviz.io/#/?load=https%3A%2F%2Fraw.githubusercontent.com%2FAzure%2Facr%2Fmaster%2Fdocs%2Fcustom-domain%2Fdocker-vm-deploy%2Fazuredeploy.json" target="_blank">
    <img src="http://armviz.io/visualizebutton.png"/>
</a>

Alternatively, to deploy using powershell script, [docker-vm-deploy/](docker-vm-deploy/), do the following:

1. Edit [azuredeploy.parameters.json](docker-vm-deploy/azuredeploy.parameters.json) and populate all necessary parameters

2. Run the following script to create the new VM:

        `.\deploy.ps1 -resourceGroupName <resourceGroup>`

### Configure DNS zone

Configure the DNS zone so `registry.contoso.com` points to the Azure VM you have just created. If you are using an Azure DNS Zone. You can use the following command:

  `New-AzureRmDnsRecordSet -Name <registry> -RecordType CNAME -ZoneName <contoso.com> -ResourceGroupName <resourceGroup> -Ttl <Ttl> -DnsRecords (New-AzureRmDnsRecordConfig -Cname <AddrToAboveVM>)`

## Quick verification

A simple way to test the setup is to call `docker login` to quickly confirm that the requests are properly forwarded:

  `docker login registry.contoso.com -u <username> -p <password>`
