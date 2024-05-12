rm -r ./bin
dotnet publish ./folder-size/folder-size.csproj -c Release --output ./bin -p:PublishSingleFile=true --self-contained false