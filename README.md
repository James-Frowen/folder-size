# folder-size
Calculates folder size for windows

## Build
```
dotnet publish ./folder-size/folder-size.csproj -c Release --output ./bin
```

## Usage

1) Build application
2) Run 
```
folder-size.exe <target>
```
3) Result will look like this
```
.vs                                                | 1.9        MB
.git                                               | 383.7      KB
folder-size/                                       | 76.8       KB
.gitignore                                         | 10.3       KB
folder-size.sln                                    | 1.1        KB
README.md                                          | 0.0        bytes
```