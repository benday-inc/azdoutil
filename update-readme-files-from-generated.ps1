dotnet test .\Benday.AzureDevOpsUtil.UnitTests

Copy-Item -Path .\generated-readme-files\README-for-nuget.md -Destination .
Copy-Item -Path .\generated-readme-files\README.md -Destination .

dotnet build