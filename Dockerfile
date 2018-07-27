FROM microsoft/dotnet:2.0-runtime

COPY ./dist .
ENTRYPOINT ["dotnet", "Harbor.Tagd.dll"]