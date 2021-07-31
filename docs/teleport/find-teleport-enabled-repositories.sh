#!/bin/bash
# Prerequisites:
# azure cli (logged in)
# jq 
# usage: find-teleport-enabled-repositories.sh acr-name

ACR_NAME=$1

enabled_repos=()

IFS=$'\n' # Each iteration of the for loop should read until we find an end-of-line
for row in $(az acr repository list --name ${ACR_NAME} |  jq '.[]' | jq @sh)
do
    # Run the row through the shell interpreter to remove enclosing double-quotes
    stripped=$(echo $row | xargs echo)
    stripped=$(echo $stripped | xargs echo)  
    
    is_teleport_enabled=$(az acr repository show --name "${ACR_NAME}" --repository "${stripped}"  --query "changeableAttributes.teleportEnabled")

    if [[ "$is_teleport_enabled" = 'true' ]]; then
        echo "$stripped -> Enabled"
        enabled_repos+=("$stripped")
    else 
        echo "$stripped -> Disabled"
    fi

done

unset IFS

echo ""
echo "Summary:"
echo "Enabled Repositories:"
echo ""

for value in "${enabled_repos[@]}"
do
     echo $value
done