param (
	$templateFile = 'azuredeploy.json',
	$templateParams = 'azuredeploy.parameters.json',
	
	[Parameter(Mandatory=$true)]
	[string]
	$resourceGroupName
)

New-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -TemplateFile $templateFile -TemplateParameterFile $templateParams

