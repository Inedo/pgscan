# pgscan
Dependency scanner for ProGet.

This command-line tool is used to gather actual dependencies used by a .net/npm/pypi project and publish them to a ProGet instance.

## Example OtterScript Usage

    # Build MyLibrary
    DotNet::Build MyLibrary.csproj
    (
        Configuration: Release
    );

    # Publish dependencies of MyLibrary to the proget.local server
    Exec
    (
        FileName: pgscan.exe
        Arguments: publish --input=MyLibrary.csproj --package-feed=Libraries --proget-url=https://proget.local --consumer-package-source=Libraries --consumer-package-version=$ReleaseNumber
    );
