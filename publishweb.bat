cd YInsights.Web
dotnet publish
cd..
docker login torosent-microsoft.azurecr.io -u torosent -p s6k3sUB7n7+b96ffOjajBzHpXRfxPAFD
docker tag yinsights.web torosent-microsoft.azurecr.io/yinsights.web
docker push torosent-microsoft.azurecr.io/yinsights.web

