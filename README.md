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
