dotnet publish -o dist -c Release
cd dist; zip -r ../Deployment.zip *; cd ..
 az webapp deployment source config-zip -g launchdarkly -n lddemo --src Deployment.zip