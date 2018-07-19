package com.microsoft.azure.management.containerregistry.samples;

import com.microsoft.azure.arm.resources.Region;
import com.microsoft.azure.arm.utils.SdkContext;
import com.microsoft.azure.credentials.AzureCliCredentials;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.BaseImageTriggerType;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.Build;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.BuildGetLogResult;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.BuildStatus;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.BuildStep;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.BuildTask;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.BuildTaskBuildRequest;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.DockerBuildStep;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.OsType;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.PlatformProperties;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.QuickBuildRequest;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.Registry;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.Sku;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.SkuName;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.SourceControlAuthInfo;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.SourceControlType;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.TokenType;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.implementation.ContainerRegistryManager;
import com.microsoft.azure.management.containerregistry.v2018_02_01_preview.implementation.SourceRepositoryPropertiesInner;
import com.microsoft.azure.management.resources.ResourceGroup;
import com.microsoft.azure.management.resources.implementation.ResourceManager;
import com.microsoft.rest.LogLevel;
import com.nimbusds.oauth2.sdk.token.Token;

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

        // ACR Build currently is avaialbe in US_EAST, US_WEST2, US_SOUTH_CENTRAL and EUROPE_WEST
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

        System.out.printf("New resource group: %s\n", rgName);

        // Create a new Azure Contaienr Registry
        ContainerRegistryManager manager = ContainerRegistryManager
                .configure()
                .withLogLevel(LogLevel.BASIC)
                .authenticate(credentials, credentials.defaultSubscriptionId());

        Registry registry = manager.registries().define(acrName)
                .withRegion(region)
                .withExistingResourceGroup(rgName)
                .withSku(new Sku().withName(SkuName.BASIC))
                .create();

        System.out.printf("New registry: %s\n", registry.name());

        // Build a container image using an existing github repositry and push the image to the registry
        QuickBuildRequest quickBuildRequest = new QuickBuildRequest()
                .withImageNames(Arrays.asList("acr-build-sample"))
                .withIsPushEnabled(true)
                .withPlatform(new PlatformProperties().withOsType(OsType.LINUX))
                .withSourceLocation("https://github.com/Azure/acr.git#master:samples/java/build")
                .withDockerFilePath("Dockerfile");
        Build build = manager.registries().queueBuildAsync(rgName, acrName, quickBuildRequest).toBlocking().first();
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

        // Create a build task to automatically schedule build based on push commit
        String buildTaskName = "builtask";
        String githubBuildContext = "Replace with your github repository url, eg: https://github.com/Azure/acr.git#master:samples/java/build";
        String githubBranch = "Replace with your github repositoty branch, eg: master";
        String githubPAT = "Replace with your github personal access token which should have the scopes: admin:repo_hook and repo";
        String dockerFilePath = "Replace with your docker file path relative to githubBuildContext, eg: Dockerfile";

        PlatformProperties platformProperties = new PlatformProperties()
            .withCpu(2)
            .withOsType(OsType.LINUX);

        SourceRepositoryPropertiesInner sourceRepositoryProperties = new SourceRepositoryPropertiesInner()
            .withIsCommitTriggerEnabled(true)
            .withSourceControlType(SourceControlType.GITHUB)
            .withRepositoryUrl(githubBuildContext)
            .withSourceControlAuthProperties(new SourceControlAuthInfo().withTokenType(TokenType.PAT).withToken(githubPAT));

        manager.buildTasks().define(buildTaskName)
            .withExistingRegistry(rgName, acrName)
            .withAlias(buildTaskName)
            .withLocation(region)
            .withPlatform(platformProperties)
            .withSourceRepository(sourceRepositoryProperties)
            .create();

        DockerBuildStep buildStep = new DockerBuildStep()
            .withImageNames(Arrays.asList("acr-build-sample-2"))
            .withBaseImageTrigger(BaseImageTriggerType.RUNTIME)
            .withBranch(githubBranch)
            .withDockerFilePath(dockerFilePath);

        manager.buildSteps().define("buildstep")
            .withExistingBuildTask(rgName, acrName, buildTaskName)
            .withProperties(buildStep)
            .create();

        // After you create the build task, you can push a change to you github repository to trigger a build
        // The following code manually triggers a new build using the build task
        BuildTaskBuildRequest buildTaskBuildRequest = new BuildTaskBuildRequest()
                .withBuildTaskName(buildTaskName);
        build = manager.registries().queueBuildAsync(rgName, acrName, buildTaskBuildRequest).toBlocking().first();
        System.out.printf("New build: %s\n", build.buildId());

        // List all builds in the registry
        System.out.println("List builds:");
        manager.builds().listAsync(rgName, acrName).toBlocking().forEach(b -> System.out.printf("Build: %s: %s\n", b.buildId(), b.status().toString()));

        // Clean the resource
        resourceManager.resourceGroups().deleteByName(resourceGroup.name());
    }

    private static boolean buildInProgress(BuildStatus buildStatus)
    {
        return buildStatus == BuildStatus.QUEUED || buildStatus == BuildStatus.STARTED || buildStatus == BuildStatus.RUNNING;
    }
}
