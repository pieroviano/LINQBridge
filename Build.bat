MSBuild.exe Net30.LinqBridge.sln -t:restore -p:RestorePackagesConfig=true
del ..\WhenTheVersion\Packages\Net30.LinqBridge.*
MSBuild.exe Net30.LinqBridge.sln -m /property:Configuration=%Configuration% 
IF DEFINED Package (
	cd Packages
	FOR %%i IN (Net30.LinqBridge.*.nupkg) DO dotnet nuget push %%i --source NuGet --api-key %ApiKey% -t 1000
	cd ..
)
git add -A
git commit -a --allow-empty-message -m ''
git push
