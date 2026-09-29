cd ../tools
dotnet tool uninstall -g MiaouVSRG.CLI
dotnet pack
dotnet tool install -g --add-source ./nupkg MiaouVSRG.CLI
cd ../scripts