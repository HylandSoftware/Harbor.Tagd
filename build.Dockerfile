FROM microsoft/dotnet:2.0-sdk
RUN curl -fksSL https://qa-admins.gitlab.hylandqa.net/ca-certificates-hyland/install.sh | bash