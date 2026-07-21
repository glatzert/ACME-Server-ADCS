param(
	[string]$Version = ""
)

$projectFile = Resolve-Path "./src/ACMEServer.ADCS/ACMEServer.ADCS.csproj"
$projectFileXml = [xml](Get-Content $projectFile)
if ($Version -eq "") {
	$Version = $projectFileXml.Project.PropertyGroup.Version

	if ($Version -eq "") {
		$Version = Read-Host "Enter version number (e.g. 1.2.3)"
	}
}

$targetFrameworks = $projectFileXml.Project.PropertyGroup.TargetFrameworks -split ';'

dotnet restore $projectFile /p:GenerateSBOM=true

foreach ($targetFramework in $targetFrameworks) {
	$outPath = [IO.Path]::Combine((Resolve-Path ./artifacts/), $targetFramework)

	dotnet build $projectFile -c Release --framework $targetFramework --no-restore --no-incremental 
	dotnet publish $projectFile  --framework $targetFramework -c Release -o $outPath --no-build

	$archivePath = [IO.Path]::Combine((Resolve-Path ./artifacts/), "ACMEServer.ADCS-V$Version-$targetFramework.zip")
	Compress-Archive -Path $outPath/* -DestinationPath $archivePath -Force

	Write-Host "Created $archivePath"
}
