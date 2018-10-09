package com.microsoft.azure.management.containerregistry.samples;

import com.microsoft.azure.arm.resources.Region;
import com.microsoft.azure.arm.utils.SdkContext;
import com.microsoft.azure.credentials.AzureCliCredentials;
import com.microsoft.azure.management.containerregistry.v2018_09_01.*;
import com.microsoft.azure.management.containerregistry.v2018_09_01.implementation.ContainerRegistryManager;
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

public class ManageTask
{
    public static void main( String[] args ) 
        throws IOException, InterruptedException { 

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
        RunRequest runRequest = new DockerBuildRequest()
                .withImageNames(Arrays.asList("java-sample:{{.Run.ID}}"))
                .withIsPushEnabled(true)
                .withPlatform(new PlatformProperties().withOs(OS.LINUX).withArchitecture(Architecture.AMD64))
                .withSourceLocation("https://github.com/Azure/acr.git#master:samples/java/build")
                .withDockerFilePath("Dockerfile")
                .withTimeout(60*10)
                .withAgentConfiguration(new AgentProperties().withCpu(2));
        Run run = manager.registries().scheduleRunAsync(rgName, acrName, runRequest).toBlocking().first();
        String runId = run.runId();

        System.out.printf("New run: %s\n", runId);

        // Poll the run status and wait for completion
        while (runInProgress(run.status())) {
            System.out.printf("%tT: In progress: %s. Wait 5 seconds\n", LocalDateTime.now(), run.status());
            Thread.sleep(5000);
            run = manager.runs().getAsync(rgName, acrName, runId).toBlocking().first();
        }

        // Get the log link
        RunGetLogResult logResult = manager.runs().getLogSasUrlAsync(rgName, acrName, runId).toBlocking().first();
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

        // Create a task to automatically schedule run based on push commit and pull request
        String githubRepoUrl = "Replace with your github repository url, eg: https://github.com/Azure/acr.git";
        String githubContext = "Replace with your github repository url with context, eg: https://github.com/Azure/acr.git#master:samples/java/build";
        String githubBranch = "Replace with your github repositoty branch, eg: master";
        String githubPAT = "Replace with your github personal access token which should have the scopes: admin:repo_hook and repo";
        String dockerFilePath = "Replace with your docker file path relative to githubContext, eg: Dockerfile";

        PlatformProperties platform = new PlatformProperties()
            .withOs(OS.LINUX)
            .withArchitecture(Architecture.AMD64);

        TaskStepProperties step = new DockerBuildStep()
            .withImageNames(Arrays.asList("java-sample:{{.Run.ID}}"))
            .withDockerFilePath(dockerFilePath)
            .withContextPath(githubContext);

        BaseImageTrigger baseImageTrigger = new BaseImageTrigger()
            .withName("SampleBaseImageTrigger")
            .withBaseImageTriggerType(BaseImageTriggerType.RUNTIME);
        SourceTrigger sourceTrigger = new SourceTrigger()
            .withName("SampleSourceTrigger")
            .withSourceRepository(new SourceProperties()
                                    .withSourceControlType(SourceControlType.GITHUB)
                                    .withBranch(githubBranch)
                                    .withRepositoryUrl(githubRepoUrl)
                                    .withSourceControlAuthProperties(new AuthInfo().withTokenType(TokenType.PAT).withToken(githubPAT)))
            .withSourceTriggerEvents(Arrays.asList(SourceTriggerEvent.COMMIT, SourceTriggerEvent.PULLREQUEST))
            .withStatus(TriggerStatus.ENABLED);
        TriggerProperties trigger = new TriggerProperties()
            .withBaseImageTrigger(baseImageTrigger)
            .withSourceTriggers(Arrays.asList(sourceTrigger));

        AgentProperties agentConfiguration = new AgentProperties().withCpu(2);

        Task task = manager.tasks().define("SampleTask")
            .withExistingRegistry(rgName, acrName)
            .withLocation(region)
            .withPlatform(platform)
            .withStep(step)
            .withTrigger(trigger)
            .withAgentConfiguration(agentConfiguration)
            .create();

        // After you create the task, you can push a change or create a pull request to you github repository to trigger a run
        // The following code manually triggers a new run using the task
        runRequest = new TaskRunRequest()
                .withTaskName(task.name());
        run = manager.registries().scheduleRunAsync(rgName, acrName, runRequest).toBlocking().first();

        System.out.printf("New run: %s\n", run.runId());

        // Schedule a multi-step task run
        runRequest = new FileTaskRunRequest()
                .withPlatform(new PlatformProperties().withOs(OS.LINUX))
                .withSourceLocation("https://github.com/Azure/acr.git#master:samples/java/build")
                .withTaskFilePath("acb.yaml")
                .withTimeout(60*10)
                .withAgentConfiguration(new AgentProperties().withCpu(2));
        run = manager.registries().scheduleRunAsync(rgName, acrName, runRequest).toBlocking().first();

        System.out.printf("New run: %s\n", run.runId());

        // List all runs in the registry
        System.out.println("List runs:");
        manager.runs().listAsync(rgName, acrName).toBlocking().forEach(r -> System.out.printf("Run: %s: %s\n", r.runId(), r.status().toString()));

        // Clean the resource
        resourceManager.resourceGroups().deleteByName(resourceGroup.name());
    }

    private static boolean runInProgress(RunStatus runStatus)
    {
        return runStatus == RunStatus.QUEUED || runStatus == RunStatus.STARTED || runStatus == RunStatus.RUNNING;
    }
}
