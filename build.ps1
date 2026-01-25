param(
	[string]$Version = ""
)

if($Version -eq "") {
	$Version = Read-Host "Enter version number (e.g. 1.2.3)"
}

dotnet restore ./src/ACMEServer.ADCS/ACMEServer.ADCS.csproj

@("8","10") | % {
	dotnet build ./src/ACMEServer.ADCS/ACMEServer.ADCS.csproj -c Release --framework net$_.0-windows --no-restore --no-incremental 
	dotnet publish ./src/ACMEServer.ADCS/ACMEServer.ADCS.csproj  --framework net$_.0-windows -c Release -o ./artifacts/net$_.0 --no-build

	Compress-Archive -Path ./artifacts/net$_.0/* -DestinationPath ./artifacts/ACMEServer.ADCS-V$($Version)-net$_.0.zip -Force
	Write-Host "Created ./artifacts/ACMEServer.ADCS-V$($Version)-net$_.0.zip"
}
