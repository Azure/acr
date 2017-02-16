param (
	$subscriptionName,
	$resourceGroupName,
	$vaultName
}

if ($subscriptionName)
{
	Select-AzureRmSubscription -SubscriptionName $subscriptionName
}

Get-AzureRmKeyVault -vaultName $vaultName -ev notPresent -ea 0
if ($notPresent)
{
    New-AzureRmKeyVault -VaultName $vaultName -ResourceGroupName $resourceGroupName -sku standard -EnabledForDeployment
}
