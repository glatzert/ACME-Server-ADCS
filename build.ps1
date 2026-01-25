param(
	[string]$Version = ""
)

if($Version -eq "") {
	$Version = Read-Host "Enter version number (e.g. 1.2.3)"
}

$projectFile = Resolve-Path "./src/ACMEServer.ADCS/ACMEServer.ADCS.csproj"
dotnet restore $projectFile

@("8","10") | % {
	$outPath = [IO.Path]::Combine((Resolve-Path ./artifacts/), "net$_.0")

	dotnet build $projectFile -c Release --framework net$_.0-windows --no-restore --no-incremental 
	dotnet publish $projectFile  --framework net$_.0-windows -c Release -o $outPath --no-build

	$archivePath = [IO.Path]::Combine((Resolve-Path ./artifacts/), "ACMEServer.ADCS-V$Version-net$_.0.zip")
	Compress-Archive -Path $outPath/* -DestinationPath $archivePath -Force

	Write-Host "Created $archivePath"
}
