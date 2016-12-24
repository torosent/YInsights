FROM microsoft/aspnetcore:1.1.0
ENTRYPOINT ["dotnet", "YInsights.Web.dll"]
ARG source=.
WORKDIR /app
EXPOSE 80
COPY $source .
