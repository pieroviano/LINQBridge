IF NOT DEFINED Configuration SET Configuration=Release
MSBuild.exe Net30.LinqBridge.sln -t:restore -p:RestorePackagesConfig=true
MSBuild.exe Net30.LinqBridge.sln -m /property:Configuration=%Configuration%
git add -A
git commit -a --allow-empty-message -m ''
git push