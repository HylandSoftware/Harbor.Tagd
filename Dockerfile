FROM microsoft/dotnet:2.0-runtime

RUN curl -fksSL https://qa-admins.gitlab.hylandqa.net/ca-certificates-hyland/install.sh | bash

COPY ./dist .
ENTRYPOINT ["dotnet", "Harbor.Tagd.dll"]