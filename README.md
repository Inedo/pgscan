# pgscan

[![Build status](https://buildmaster.inedo.com/api/ci-badges/image?API_Key=badges&$ApplicationId=78)](https://buildmaster.inedo.com/api/ci-badges/link?API_Key=badges&$ApplicationId=78)

This tool is used to gather actual dependencies used by a .net/npm/pypi project and publish them to a ProGet instance. It is available as a standalone tool
for Windows/Linux, an [installable dotnet tool](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools), or a .net class library. Its functionality
is also available in OtterScript directly using the `ProGet::Record-Dependencies` operation.

## Installation (standalone CLI)

Download the latest version from the Releases page.


## Installation (dotnet tool)

Install the tool using dotnet. For example, to install the tool locally to the current tool manifest:

```Batchfile
dotnet tool install pgscan
```

## Usage (CLI/tool with ProGet 2022 or newer)

Execute `pgscan` with the `identify` command. For example, to generate an SBOM and submit the dependencies of v1.0.0 the `MyLibrary` project to ProGet:

```Batchfile
pgscan identify --input=MyLibrary.csproj --proget-url=https://proget.local --consumer-package-version=1.0.0
```


## Usage (CLI/tool with ProGet v6)

Execute `pgscan` with the `publish` command. For example, to submit the dependencies of v1.0.0 the `MyLibrary` project to ProGet's `Libraries` feed:

```Batchfile
pgscan publish --input=MyLibrary.csproj --package-feed=Libraries --proget-url=https://proget.local --consumer-package-source=Libraries --consumer-package-version=1.0.0
```


## Usage (OtterScript)

Use the ProGet::Record-Dependencies operation:

```
ProGet::Record-Dependencies
(
    Project: MyProject.csproj,
    Resource: LocalProGet,
    Feed: Libraries,
    ConsumerVersion: $ReleaseNumber
);
```

## Usage (GitHub Actions)

Use a local `dotnet tool` action to run pgscan on Windows and Linux build agents.

1. Create a [ProGet API key](https://docs.inedo.com/docs/proget-administration-security-api-keys)
   1. Once the API Key is created in ProGet, you will need to add it as a Secret on your GitHub project
   2. Navigate to your project in GitHub
   3. Click "Settings"
   4. Navigate to "Secrets -> Actions" on the right
   5. Click "New repository secret"
   6. Enter a name (ex: `PROGETAPIKEY`) and your API key as the secret value
2. Commit a dotnet tool manifest
   1. At the root of your repository, run `dotnet new tool-manifest` (see [Microsoft's local tool](https://docs.microsoft.com/en-us/dotnet/core/tools/local-tools-how-to-use#create-a-manifest-file) documentation for more information)
   2. Commit this to your git repository
3. Setup .NET 6.0 in your workflow
   - If you are already using dotnet 6 in your workflow, go to the next step.
   - Add the following to your workflow:
    ```yaml
        - name: Setup .NET
          uses: actions/setup-dotnet@v2
          with:
            dotnet-version: 6.0.x
    ```
    - This can be added anywhere before the pgscan steps, but is typically added at the beginning
4. Add the pgscan steps after build/publish steps of your code
```yaml
    - name: Install pgscan
      run: dotnet tool install pgscan
    - name: Run pgscan
      working-directory: ProfiteCalcNet.Console
      run: dotnet tool run pgscan identify --type=nuget --input=MyProject.csproj --project-name=MyProject --version=1.0.0 --project-type=application --proget-url=https://proget.local --api-key=${{ secrets.PROGETAPIKEY }}
```


## Usage (Azure DevOps)

Use a local `dotnet tool` action to run pgscan on Windows and Linux build agents.

1. Create a [ProGet API key](https://docs.inedo.com/docs/proget-administration-security-api-keys)
   1. Once the API Key is created in ProGet, you will need to add it as a secrete Variable on your pipeline.
   2. Navigate to your pipeline in Azure DevOps
   3. Click Edit
   4. Click Variables and then the plus icon
   5. Enter a name (ex: `PROGETAPIKEY`) and your API key as the value
   6. Check "Keep this value Secret"
   7. Click OK
2. Commit a dotnet tool manifest
   1. At the root of your repository, run `dotnet new tool-manifest` (see [Microsoft's local tool](https://docs.microsoft.com/en-us/dotnet/core/tools/local-tools-how-to-use#create-a-manifest-file) documentation for more information)
   2. Commit this to your git repository
3. Add .NET 6.0 in your pipeline
   - If you are already using dotnet 6 in your pipeline, go to the next step.
   - Add the following to your workflow:
    ```yaml
    - task: UseDotNet@2
      inputs:
        packageType: 'sdk'
        version: '6.0.x'
    ```
    - This can be added anywhere before the pgscan steps, but is typically added at the beginning
4. Add the pgscan steps after build/publish steps of your code
   ```yaml
   - script: dotnet tool install pgscan
   - script: dotnet tool run pgscan identify --type=nuget --input=MyProject.csproj --project-name=MyProject --version=1.0.0 --project-type=application --proget-url=https://proget.local --api-key=$(PROGETAPIKEY)
   ```
