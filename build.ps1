dotnet restore ./src/ACMEServer.ADCS/ACMEServer.ADCS.csproj

@("8","10") | % {
	dotnet build ./src/ACMEServer.ADCS/ACMEServer.ADCS.csproj --no-restore --no-incremental -c Release --framework net$_.0-windows
	dotnet publish ./src/ACMEServer.ADCS/ACMEServer.ADCS.csproj --no-build --framework net$_.0-windows -c Release -o ./artifacts/net$_.0

	Compress-Archive -Path ./artifacts/net$_.0/* -DestinationPath ./artifacts/ACMEServer.ADCS-Vx-net$_.0.zip -Force
	Write-Host "Created ./artifacts/ACMEServer.ADCS-Vx-net$_.0.zip"
}
