cd generateuserarticles
dotnet publish
cd..
cd getgeneraltopics
dotnet publish
cd..
cd getlasttopics
dotnet publish
cd..
cd gettrendingtopics
dotnet publish
cd..
cd hnarticles
dotnet publish
cd..
cd processarticleswithcalais
dotnet publish
cd..
cd YInsights.Web
dotnet publish
cd..
docker login torosent-microsoft.azurecr.io -u torosent -p s6k3sUB7n7+b96ffOjajBzHpXRfxPAFD
docker tag generateuserarticles torosent-microsoft.azurecr.io/generateuserarticles
docker push torosent-microsoft.azurecr.io/generateuserarticles
docker tag getgeneraltopics torosent-microsoft.azurecr.io/getgeneraltopics
docker push torosent-microsoft.azurecr.io/getgeneraltopics
docker tag getlasttopics torosent-microsoft.azurecr.io/getlasttopics
docker push torosent-microsoft.azurecr.io/getlasttopics
docker tag gettrendingtopics torosent-microsoft.azurecr.io/gettrendingtopics
docker push torosent-microsoft.azurecr.io/gettrendingtopics
docker tag hnarticles torosent-microsoft.azurecr.io/hnarticles
docker push torosent-microsoft.azurecr.io/hnarticles
docker tag processarticleswithcalais torosent-microsoft.azurecr.io/processarticleswithcalais
docker push torosent-microsoft.azurecr.io/processarticleswithcalais
docker tag yinsights.web torosent-microsoft.azurecr.io/yinsights.web
docker push torosent-microsoft.azurecr.io/yinsights.web

