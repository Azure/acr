package com.microsoft.azure.management.containerregistry.samples;

import com.microsoft.azure.arm.resources.Region;
import com.microsoft.azure.arm.utils.SdkContext;
import com.microsoft.azure.credentials.AzureCliCredentials;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.Build;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.BuildGetLogResult;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.BuildStatus;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.OsType;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.PlatformProperties;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.QuickBuildRequest;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.Registry;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.Sku;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.SkuName;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.implementation.ContainerRegistryManager;
import com.microsoft.azure.management.resources.ResourceGroup;
import com.microsoft.azure.management.resources.implementation.ResourceManager;
import com.microsoft.rest.LogLevel;

import org.apache.http.HttpResponse;
import org.apache.http.client.methods.HttpGet;
import org.apache.http.impl.client.CloseableHttpClient;
import org.apache.http.impl.client.HttpClientBuilder;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.time.LocalDateTime;
import java.util.Arrays;

public class ManageBuild
{
    public static void main( String[] args ) 
        throws IOException, InterruptedException { 

        // ACR Build currently is avaialbe in US_EAST, US_WEST2 and EUROPE_WEST
        String region = Region.US_EAST.toString();
        String rgName = SdkContext.randomResourceName("rg", 20);
        String acrName = SdkContext.randomResourceName("acr", 20);

        // Read the Azure credentail from the auth file
        // See how to create an auth file: https://github.com/Azure/azure-libraries-for-java/blob/master/AUTH.md
        AzureCliCredentials credentials = AzureCliCredentials.create();

        // Create a new resource group
        ResourceManager resourceManager = ResourceManager
                .configure()
                .withLogLevel(LogLevel.BASIC)
                .authenticate(credentials)
                .withSubscription(credentials.defaultSubscriptionId());

        ResourceGroup resourceGroup = resourceManager.resourceGroups()
                .define(rgName)
                .withRegion(region)
                .create();

        System.out.printf("New resource group: %s\n", resourceGroup.name());

        // Create a new Azure Contaienr Registry
        ContainerRegistryManager manager = ContainerRegistryManager
                .configure()
                .withLogLevel(LogLevel.BASIC)
                .authenticate(credentials, credentials.defaultSubscriptionId());

        Registry registry = manager.registries().define(acrName)
                .withRegion(region)
                .withExistingResourceGroup(resourceGroup.name())
                .withSku(new Sku().withName(SkuName.BASIC))
                .create();

        System.out.printf("New registry: %s\n", registry.name());

        // Build a container image using an existing github repositry and push the image to the registry
        QuickBuildRequest request = new QuickBuildRequest()
                .withImageNames(Arrays.asList("acr-build-sample"))
                .withIsPushEnabled(true)
                .withPlatform(new PlatformProperties().withOsType(OsType.LINUX))
                .withSourceLocation("https://github.com/Azure/acr.git#master:samples/java/build")
                .withDockerFilePath("Dockerfile");
        Build build = manager.registries().queueBuildAsync(rgName, acrName, request).toBlocking().first();
        String buildId = build.buildId();

        System.out.printf("New build: %s\n", buildId);

        // Poll the build status and wait for completion
        while (buildInProgress(build.status())) {
            System.out.printf("%tT: In progress: %s. Wait 5 seconds\n", LocalDateTime.now(), build.status());
            Thread.sleep(5000);
            build = manager.builds().getAsync(rgName, acrName, buildId).toBlocking().first();
        }

        // Get the log link
        BuildGetLogResult logResult = manager.builds().getLogLinkAsync(rgName, acrName, buildId).toBlocking().first();
        String logLink = logResult.logLink();
        try (CloseableHttpClient httpClient = HttpClientBuilder.create().build()) {
            HttpGet logRequest = new HttpGet(logLink);
            HttpResponse logResponse = httpClient.execute(logRequest);

            BufferedReader bufferReader = new BufferedReader(
                new InputStreamReader(logResponse.getEntity().getContent()));

            String line;

            while ((line = bufferReader.readLine()) != null) {
                System.out.println(line);
            }
        }

        // Clean the resource
        resourceManager.resourceGroups().deleteByName(resourceGroup.name());
    }

    private static boolean buildInProgress(BuildStatus buildStatus)
    {
        return buildStatus == BuildStatus.QUEUED || buildStatus == BuildStatus.STARTED || buildStatus == BuildStatus.RUNNING;
    }
}
