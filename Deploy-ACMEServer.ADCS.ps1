[CmdletBinding()]
param(
	[Parameter(Mandatory, ParameterSetName = 'StepDownloadSoftware')]
	[switch]$StepDownloadSoftware,

	[Parameter(Mandatory, ParameterSetName = 'StepDownloadNetHostingPackage')]
	[switch]$StepDownloadNetHostingPackage,

	[Parameter(Mandatory, ParameterSetName = 'StepInstallHostingPackage')]
	[switch]$StepInstallHostingPackage,

	[Parameter(Mandatory, ParameterSetName = 'StepInstallIIS')]
	[switch]$StepInstallIIS,

	[Parameter(Mandatory, ParameterSetName = 'StepCreateGmsaIdentity')]
	[switch]$StepCreateGmsaIdentity,

	[Parameter(Mandatory, ParameterSetName = 'StepDeployToIIS')]
	[switch]$StepDeployToIIS,

	[Parameter(Mandatory, ParameterSetName = 'StepDeployAsService')]
	[switch]$StepDeployAsService,

	[Parameter(Mandatory, ParameterSetName = 'StepCreateKestrelConfig')]
	[switch]$StepCreateKestrelConfig,

	[Parameter(Mandatory, ParameterSetName = 'StepCreateConfig')]
	[switch]$StepCreateConfig,

	[Parameter(Mandatory, ParameterSetName = 'IIS')]
	[switch]$IIS,

	[Parameter(Mandatory, ParameterSetName = 'Service')]
	[switch]$Service,

	[Parameter(ParameterSetName = 'StepDownloadSoftware')]
	[Parameter(ParameterSetName = 'IIS')]
	[Parameter(ParameterSetName = 'Service')]
	[switch]$UseBetaSoftware,

	[Parameter(ParameterSetName = 'StepCreateGmsaIdentity')]
	[Parameter(ParameterSetName = 'IIS')]
	[Parameter(ParameterSetName = 'Service')]
	[Parameter(ParameterSetName = 'StepDeployToIIS')]
	[Parameter(ParameterSetName = 'StepDeployAsService')]
	[string]$AccountName = 'acme_user$',

	[Parameter(Mandatory, ParameterSetName = 'StepCreateGmsaIdentity')]
	[Parameter(Mandatory, ParameterSetName = 'StepDeployToIIS')]
	[Parameter(Mandatory, ParameterSetName = 'StepCreateKestrelConfig')]
	[Parameter(Mandatory, ParameterSetName = 'IIS')]
	[Parameter(Mandatory, ParameterSetName = 'Service')]
	[string]$DnsHostName,

	[Parameter(ParameterSetName = 'StepCreateGmsaIdentity')]
	[Parameter(ParameterSetName = 'IIS')]
	[Parameter(ParameterSetName = 'Service')]
	[string[]]$GmsaAllowedPrincipals = @("$env:COMPUTERNAME$"),

	[Parameter(ParameterSetName = 'StepCreateKestrelConfig')]
	[Parameter(ParameterSetName = 'Service')]
	[int]$KestrelHttpPort = 8080,

	[Parameter(ParameterSetName = 'StepCreateKestrelConfig')]
	[Parameter(ParameterSetName = 'Service')]
	[int]$KestrelHttpsPort = 8443,

	[string]$SoftwareArchivePath = 'C:\Temp\acme-server.zip',
	[string]$NetHostingInstallerPath = 'C:\Temp\dotnet-hosting.exe',
	[string]$NetHostingInstallerArguments = '/install /quiet /norestart',
	[string]$DeploymentPath = 'C:\Program Files\th11s\acme-adcs'
)

begin {
	Set-StrictMode -Version Latest
	$ErrorActionPreference = 'Stop'

	function Get-LatestAcmeSoftwareAsset {
	[CmdletBinding()]
	param(
		[switch]$IncludeBeta
	)

	$headers = @{
		'Accept'     = 'application/vnd.github+json'
		'User-Agent' = 'ACME-Server-ADCS-DeployScript'
	}

	if ($IncludeBeta) {
		$releases = Invoke-RestMethod -Method Get -Headers $headers -Uri 'https://api.github.com/repos/glatzert/ACME-Server-ADCS/releases?per_page=30'
		$release = $releases | Where-Object { -not $_.draft -and $_.prerelease } | Select-Object -First 1
		if (-not $release) {
			throw 'No beta release found for glatzert/ACME-Server-ADCS.'
		}
	}
	else {
		$release = Invoke-RestMethod -Method Get -Headers $headers -Uri 'https://api.github.com/repos/glatzert/ACME-Server-ADCS/releases/latest'
	}

	$assets = $release.assets | Where-Object {
		$_.name -match '\.zip$'
	}

	if (-not $assets) {
		throw "No downloadable software asset found in release '$($release.tag_name)'."
	}

	return $assets
}

function Download-Software {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory)]
		[string]$DestinationPath,

		[switch]$UseBeta
	)

	$assets = @(Get-LatestAcmeSoftwareAsset -IncludeBeta:$UseBeta)
	$asset = $null

	if ($assets.Count -eq 1) {
		$asset = $assets[0]
	}
	else {
		Write-Host 'Multiple software ZIP assets found. Please choose one:'
		for ($index = 0; $index -lt $assets.Count; $index++) {
			Write-Host ("[{0}] {1}" -f ($index + 1), $assets[$index].name)
		}

		do {
			$selectionInput = Read-Host "Enter asset number (1-$($assets.Count))"
			$selection = 0
			$isValidNumber = [int]::TryParse($selectionInput, [ref]$selection)
		}
		while (-not $isValidNumber -or $selection -lt 1 -or $selection -gt $assets.Count)

		$asset = $assets[$selection - 1]
	}

	$destinationDirectory = Split-Path -Path $DestinationPath -Parent
	if (-not [string]::IsNullOrWhiteSpace($destinationDirectory) -and -not (Test-Path -Path $destinationDirectory)) {
		New-Item -Path $destinationDirectory -ItemType Directory -Force | Out-Null
	}

	Write-Host ("Downloading software asset '{0}' from '{1}' to '{2}'..." -f $asset.name, $asset.browser_download_url, $DestinationPath)
	Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $DestinationPath
	Write-Host 'Software download complete.'
}

function Get-LatestLtsNetHostingBundleUri {
	[CmdletBinding()]
	param()

	$releaseIndexUri = 'https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/releases-index.json'
	$index = Invoke-RestMethod -Method Get -Uri $releaseIndexUri

	$ltsChannels = $index.'releases-index' |
		Where-Object { $_.'release-type' -eq 'lts' -and $_.'support-phase' -eq 'active' } |
		Sort-Object { [version]$_.'channel-version' } -Descending

	if (-not $ltsChannels) {
		throw 'No active LTS channels found in .NET release metadata.'
	}

	$selectedChannels = $ltsChannels
	if ($ltsChannels.Count -gt 1) {
		Write-Host 'Multiple active .NET LTS channels are available. Please choose one:'
		for ($index = 0; $index -lt $ltsChannels.Count; $index++) {
			$channel = $ltsChannels[$index]
			Write-Host ("[{0}] .NET {1} (latest release: {2})" -f ($index + 1), $channel.'channel-version', $channel.'latest-release')
		}

		do {
			$selectionInput = Read-Host "Enter LTS channel number (1-$($ltsChannels.Count))"
			$selection = 0
			$isValidNumber = [int]::TryParse($selectionInput, [ref]$selection)
		}
		while (-not $isValidNumber -or $selection -lt 1 -or $selection -gt $ltsChannels.Count)

		$selectedChannels = @($ltsChannels[$selection - 1])
	}

	foreach ($channel in $selectedChannels) {
		$channelReleases = Invoke-RestMethod -Method Get -Uri $channel.'releases.json'
		$release = $channelReleases.releases | Where-Object { $_.'release-version' -eq $channel.'latest-release' } | Select-Object -First 1

		if (-not $release) {
			$release = $channelReleases.releases | Select-Object -First 1
		}

		if (-not $release) {
			continue
		}

		$candidateFiles = @()
		if ($release.'aspnetcore-runtime' -and $release.'aspnetcore-runtime'.files) {
			$candidateFiles += $release.'aspnetcore-runtime'.files
		}
		if ($release.sdk -and $release.sdk.files) {
			$candidateFiles += $release.sdk.files
		}
		if ($release.sdks) {
			foreach ($sdk in $release.sdks) {
				if ($sdk.files) {
					$candidateFiles += $sdk.files
				}
			}
		}

		$hostingBundle = $candidateFiles |
			Where-Object {
				$_.name -eq 'dotnet-hosting-win.exe' -or $_.name -like 'dotnet-hosting-*-win.exe'
			} |
			Select-Object -First 1

		if ($hostingBundle -and $hostingBundle.url) {
			Write-Host ("Resolved latest LTS .NET Hosting Bundle from channel {0} release {1}." -f $channel.'channel-version', $release.'release-version')
			return $hostingBundle.url
		}
	}

	throw 'Unable to resolve .NET Hosting Bundle URL from latest LTS release metadata.'
}

function Download-NetHostingPackage {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory)]
		[string]$DestinationPath
	)

	$destinationDirectory = Split-Path -Path $DestinationPath -Parent
	if (-not [string]::IsNullOrWhiteSpace($destinationDirectory) -and -not (Test-Path -Path $destinationDirectory)) {
		New-Item -Path $destinationDirectory -ItemType Directory -Force | Out-Null
	}

	$downloadUri = Get-LatestLtsNetHostingBundleUri

	Write-Host "Downloading .NET Hosting Bundle from '$downloadUri' to '$DestinationPath'..."
	Invoke-WebRequest -Uri $downloadUri -OutFile $DestinationPath
	Write-Host '.NET Hosting Bundle download complete.'
}

function Install-HostingPackage {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory)]
		[string]$InstallerPath,

		[string]$InstallerArguments = '/install /quiet /norestart'
	)

	if (-not (Test-Path -Path $InstallerPath)) {
		throw "Hosting installer not found at '$InstallerPath'."
	}

	Write-Host "Installing hosting package from '$InstallerPath'..."
	$process = Start-Process -FilePath $InstallerPath -ArgumentList $InstallerArguments -Wait -PassThru

	if ($process.ExitCode -ne 0) {
		throw "Hosting package installation failed with exit code $($process.ExitCode)."
	}

	Write-Host 'Hosting package installation complete.'
}

function Install-IIS {
	[CmdletBinding()]
	param()

	Write-Host 'Installing IIS Web Server + IIS Management Console...'

	if (Get-Command -Name Install-WindowsFeature -ErrorAction SilentlyContinue) {
		$result = Install-WindowsFeature -Name Web-Server, Web-Mgmt-Console
		if (-not $result.Success) {
			throw 'Failed to install IIS features with Install-WindowsFeature.'
		}
	}
	elseif (Get-Command -Name Enable-WindowsOptionalFeature -ErrorAction SilentlyContinue) {
		Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole -All -NoRestart | Out-Null
		Enable-WindowsOptionalFeature -Online -FeatureName IIS-ManagementConsole -All -NoRestart | Out-Null
	}
	else {
		throw 'Neither Install-WindowsFeature nor Enable-WindowsOptionalFeature is available on this system.'
	}

	Write-Host 'IIS installation step complete.'
}

function Extract-Software {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory)]
		[string]$ArchivePath,

		[Parameter(Mandatory)]
		[string]$DestinationFolder
	)

	if (-not (Test-Path -Path $ArchivePath)) {
		throw "Software archive not found at '$ArchivePath'."
	}

	if (-not (Test-Path -Path $DestinationFolder)) {
		New-Item -Path $DestinationFolder -ItemType Directory -Force | Out-Null
	}

	Write-Host "Extracting software archive '$ArchivePath' to '$DestinationFolder'..."
	Expand-Archive -Path $ArchivePath -DestinationPath $DestinationFolder -Force
	Write-Host 'Software extraction complete.'
}

function New-GmsaProcessIdentity {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory)]
		[string]$AccountName,

		[Parameter(Mandatory)]
		[string]$DnsHostName,

		[Parameter(Mandatory)]
		[string[]]$PrincipalsAllowedToRetrieveManagedPassword
	)

	Import-Module ActiveDirectory -ErrorAction Stop

	$existingAccount = Get-ADServiceAccount -Identity $AccountName -ErrorAction SilentlyContinue
	if ($existingAccount) {
		Write-Host "gMSA '$AccountName' already exists."
		return
	}

	Write-Host "Creating gMSA '$AccountName'..."
	$newGmsaParameters = @{
		Name = $AccountName
		DNSHostName = $DnsHostName
		PrincipalsAllowedToRetrieveManagedPassword = $PrincipalsAllowedToRetrieveManagedPassword
	}
	New-ADServiceAccount @newGmsaParameters

	Write-Host "gMSA '$AccountName' created."
}

function Test-DnsHostName {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory)]
		[string]$HostName
	)

	if ($HostName -match '^(?=.{1,253}$)(?!-)[a-zA-Z0-9-]{1,63}(?<!-)(\.(?!-)[a-zA-Z0-9-]{1,63}(?<!-))*$') {
		return $true
	}

	return $false
}

function Grant-ProcessIdentityAccess {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory)]
		[string]$DirectoryPath,

		[Parameter(Mandatory)]
		[string]$ProcessIdentity,

		[Parameter(Mandatory)]
		[System.Security.AccessControl.FileSystemRights]$FileSystemRights
	)

	if (-not (Test-Path -Path $DirectoryPath)) {
		throw "Directory '$DirectoryPath' does not exist."
	}

	$inheritanceFlags = [System.Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [System.Security.AccessControl.InheritanceFlags]::ObjectInherit
	$propagationFlags = [System.Security.AccessControl.PropagationFlags]::None
	$accessControlType = [System.Security.AccessControl.AccessControlType]::Allow

	$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
		$ProcessIdentity,
		$FileSystemRights,
		$inheritanceFlags,
		$propagationFlags,
		$accessControlType
	)

	$acl = Get-Acl -Path $DirectoryPath
	$acl.SetAccessRule($accessRule)
	Set-Acl -Path $DirectoryPath -AclObject $acl
}

function Add-IisProcessIdentityToIisIusrs {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory)]
		[string]$ProcessIdentity
	)

	try {
		Add-LocalGroupMember -Group 'IIS_IUSRS' -Member $ProcessIdentity -ErrorAction Stop
		Write-Host "Added '$ProcessIdentity' to IIS_IUSRS."
	}
	catch {
		if ($_.Exception.Message -match 'already a member') {
			Write-Host "'$ProcessIdentity' is already in IIS_IUSRS."
		}
		else {
			throw
		}
	}
}

function Deploy-SoftwareToIIS {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory)]
		[string]$ArchivePath,

		[Parameter(Mandatory)]
		[string]$BindingHostName,

		[Parameter(Mandatory)]
		[string]$ProcessIdentity,

		[string]$DestinationFolder = 'C:\inetpub\th11s-acme-adcs',
		[string]$SiteName = 'th11s-acme-adcs',
		[string]$AppPoolName = 'th11s-acme-adcs',
		[int]$Port = 80
	)

	if (-not (Test-DnsHostName -HostName $BindingHostName)) {
		throw "Binding host name '$BindingHostName' must be a DNS name only."
	}

	Extract-Software -ArchivePath $ArchivePath -DestinationFolder $DestinationFolder
	Add-IisProcessIdentityToIisIusrs -ProcessIdentity $ProcessIdentity
	Grant-ProcessIdentityAccess -DirectoryPath $DestinationFolder -ProcessIdentity $ProcessIdentity -FileSystemRights ([System.Security.AccessControl.FileSystemRights]::ReadAndExecute)

	Import-Module WebAdministration -ErrorAction Stop

	if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) {
		New-WebAppPool -Name $AppPoolName | Out-Null
	}

	Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ''
	Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name processModel -Value @{ identityType = 3; userName = $ProcessIdentity; password = '' }

	if (-not (Test-Path "IIS:\Sites\$SiteName")) {
		New-Website -Name $SiteName -PhysicalPath $DestinationFolder -ApplicationPool $AppPoolName -Port $Port -HostHeader $BindingHostName | Out-Null
	}
	else {
		Set-ItemProperty -Path "IIS:\Sites\$SiteName" -Name physicalPath -Value $DestinationFolder
		Set-ItemProperty -Path "IIS:\Sites\$SiteName" -Name applicationPool -Value $AppPoolName

		$existingBinding = Get-WebBinding -Name $SiteName -Protocol http | Where-Object { $_.bindingInformation -eq "*:${Port}:$BindingHostName" }
		if (-not $existingBinding) {
			New-WebBinding -Name $SiteName -Protocol http -Port $Port -HostHeader $BindingHostName | Out-Null
		}
	}

	Write-Host "IIS deployment completed. Site '$SiteName' is bound to '$BindingHostName'."
}

function New-AcmeWindowsService {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory)]
		[string]$ExecutablePath,

		[Parameter(Mandatory)]
		[string]$ServiceName,

		[Parameter(Mandatory)]
		[string]$DisplayName,

		[Parameter(Mandatory)]
		[string]$ProcessIdentity
	)

	$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
	if ($existingService) {
		if ($existingService.Status -eq 'Running') {
			Stop-Service -Name $ServiceName -Force
		}

		$serviceInstance = Get-CimInstance -ClassName Win32_Service -Filter "Name='$ServiceName'" -ErrorAction SilentlyContinue
		if ($serviceInstance) {
			$deleteResult = Invoke-CimMethod -InputObject $serviceInstance -MethodName Delete
			if ($deleteResult.ReturnValue -ne 0) {
				throw "Failed to delete existing Windows service '$ServiceName'. ReturnValue: $($deleteResult.ReturnValue)"
			}
		}

		Start-Sleep -Seconds 2
	}

	$emptyPassword = ConvertTo-SecureString -String '' -AsPlainText -Force
	$serviceCredential = [System.Management.Automation.PSCredential]::new($ProcessIdentity, $emptyPassword)
	$binaryPath = "`"$ExecutablePath`""

	New-Service -Name $ServiceName -BinaryPathName $binaryPath -DisplayName $DisplayName -StartupType Automatic -Credential $serviceCredential | Out-Null

	Start-Service -Name $ServiceName
	Write-Host "Windows service '$ServiceName' created and started."
}

function Deploy-SoftwareAsService {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory)]
		[string]$ArchivePath,

		[Parameter(Mandatory)]
		[string]$ProcessIdentity,

		[string]$DestinationFolder = 'C:\Program Files\th11s\acme-adcs',
		[string]$ServiceName = 'ACMEServiceADCS',
		[string]$ServiceDisplayName = 'ACME Service ADCS'
	)

	Extract-Software -ArchivePath $ArchivePath -DestinationFolder $DestinationFolder
	Grant-ProcessIdentityAccess -DirectoryPath $DestinationFolder -ProcessIdentity $ProcessIdentity -FileSystemRights ([System.Security.AccessControl.FileSystemRights]::Modify)

	$exePath = Join-Path -Path $DestinationFolder -ChildPath 'ACMEService.ADCS.exe'
	if (-not (Test-Path -Path $exePath)) {
		throw "Service executable not found at '$exePath'."
	}

	New-AcmeWindowsService -ExecutablePath $exePath -ServiceName $ServiceName -DisplayName $ServiceDisplayName -ProcessIdentity $ProcessIdentity
}

function New-KestrelConfiguration {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory)]
		[string]$DnsHostName,

		[Parameter(Mandatory)]
		[string]$DestinationPath,

		[int]$HttpPort = 8080,
		[int]$HttpsPort = 8443
	)

	$configPath = Join-Path -Path $DestinationPath -ChildPath 'appsettings.Kestrel.json'

	if (-not (Test-Path -Path $DestinationPath)) {
		New-Item -ItemType Directory -Path $DestinationPath -Force | Out-Null
	}

	$kestrelConfig = @{
		Kestrel = @{
			Endpoints = @{
				Http = @{
					Url = "http://$($DnsHostName):$HttpPort"
				}
				Https = @{
					Url = "https://$($DnsHostName):$HttpsPort"
					Certificate = @{
						Store = @{
							Name = "My"
							Location = "LocalMachine"
						}
						Subject = $DnsHostName
						AllowInvalid = $false
					}
				}
			}
		}
	}

	$json = $kestrelConfig | ConvertTo-Json -Depth 10
	Set-Content -Path $configPath -Value $json -Encoding UTF8 -Force

	Write-Host "Kestrel configuration created at '$configPath' with certificate subject '$DnsHostName'."
}

function Invoke-ConfigurationTool {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory)]
		[string]$DeploymentPath
	)

	if (-not (Test-Path -Path $DeploymentPath)) {
		throw "Deployment path '$DeploymentPath' does not exist. Please deploy the software first."
	}

	$cliExecutable = Join-Path -Path $DeploymentPath -ChildPath 'ACMEServer.CLI.exe'
	if (-not (Test-Path -Path $cliExecutable)) {
		throw "CLI executable not found at '$cliExecutable'."
	}

	Write-Host "Starting configuration creation tool from '$DeploymentPath'..."
	& $cliExecutable --config-tool

	if ($LASTEXITCODE -ne 0) {
		Write-Warning "Configuration tool exited with code $LASTEXITCODE. Configuration may not have been created."
	}
	else {
		Write-Host "Configuration tool completed successfully."
	}
}
} # end begin block

process {
	$ProcessIdentity = $null
	$parameterSetsWithProcessIdentity = @('StepDeployToIIS', 'StepDeployAsService', 'IIS', 'Service')
	if ($PSCmdlet.ParameterSetName -in $parameterSetsWithProcessIdentity) {
		$identityDomain = $env:USERDOMAIN
		if ([string]::IsNullOrWhiteSpace($identityDomain) -and -not [string]::IsNullOrWhiteSpace($env:USERDNSDOMAIN)) {
			$identityDomain = $env:USERDNSDOMAIN
		}

		if ([string]::IsNullOrWhiteSpace($identityDomain)) {
			throw 'Identity domain could not be determined from USERDOMAIN/USERDNSDOMAIN.'
		}

		if ([string]::IsNullOrWhiteSpace($AccountName)) {
			throw 'AccountName must not be empty.'
		}

		$gmsaNameForIdentity = $AccountName
		if ($gmsaNameForIdentity -notmatch '\$$') {
			$gmsaNameForIdentity = "$gmsaNameForIdentity$"
		}

		$gmsaSamName = ($gmsaNameForIdentity -split '\\')[-1]
		$ProcessIdentity = "$identityDomain\$gmsaSamName"
	}

	# Dynamically assign DeploymentPath based on deployment mode if using default
	if ($DeploymentPath -eq 'C:\Program Files\th11s\acme-adcs') {
		if ($PSCmdlet.ParameterSetName -in @('IIS', 'StepDeployToIIS')) {
			$DeploymentPath = 'C:\inetpub\th11s-acme-adcs'
		}
	}

	switch ($PSCmdlet.ParameterSetName) {
        'StepDownloadSoftware' {
            Download-Software -DestinationPath $SoftwareArchivePath -UseBeta:$UseBetaSoftware
        }
        'StepDownloadNetHostingPackage' {
            Download-NetHostingPackage -DestinationPath $NetHostingInstallerPath
        }
        'StepInstallHostingPackage' {
            Install-HostingPackage -InstallerPath $NetHostingInstallerPath -InstallerArguments $NetHostingInstallerArguments
        }
        'StepInstallIIS' {
            Install-IIS
        }
        'StepCreateGmsaIdentity' {
            New-GmsaProcessIdentity -AccountName $AccountName -DnsHostName $DnsHostName -PrincipalsAllowedToRetrieveManagedPassword $GmsaAllowedPrincipals
        }
        'StepDeployToIIS' {
            Deploy-SoftwareToIIS -ArchivePath $SoftwareArchivePath -BindingHostName $DnsHostName -ProcessIdentity $ProcessIdentity -DestinationFolder $DeploymentPath
        }
        'StepDeployAsService' {
            Deploy-SoftwareAsService -ArchivePath $SoftwareArchivePath -ProcessIdentity $ProcessIdentity -DestinationFolder $DeploymentPath
        }
        'StepCreateKestrelConfig' {
            New-KestrelConfiguration -DnsHostName $DnsHostName -DestinationPath $DeploymentPath -HttpPort $KestrelHttpPort -HttpsPort $KestrelHttpsPort
        }
        'StepCreateConfig' {
            Invoke-ConfigurationTool -DeploymentPath $DeploymentPath
        }
        'IIS' {
            Download-Software -DestinationPath $SoftwareArchivePath -UseBeta:$UseBetaSoftware
            Install-IIS
            Download-NetHostingPackage -DestinationPath $NetHostingInstallerPath
            Install-HostingPackage -InstallerPath $NetHostingInstallerPath -InstallerArguments $NetHostingInstallerArguments
            New-GmsaProcessIdentity -AccountName $AccountName -DnsHostName $DnsHostName -PrincipalsAllowedToRetrieveManagedPassword $GmsaAllowedPrincipals
            Deploy-SoftwareToIIS -ArchivePath $SoftwareArchivePath -BindingHostName $DnsHostName -ProcessIdentity $ProcessIdentity -DestinationFolder $DeploymentPath
        }
        'Service' {
            Download-Software -DestinationPath $SoftwareArchivePath -UseBeta:$UseBetaSoftware
            Download-NetHostingPackage -DestinationPath $NetHostingInstallerPath
            Install-HostingPackage -InstallerPath $NetHostingInstallerPath -InstallerArguments $NetHostingInstallerArguments
            New-GmsaProcessIdentity -AccountName $AccountName -DnsHostName $DnsHostName -PrincipalsAllowedToRetrieveManagedPassword $GmsaAllowedPrincipals
            New-KestrelConfiguration -DnsHostName $DnsHostName -DestinationPath $DeploymentPath -HttpPort $KestrelHttpPort -HttpsPort $KestrelHttpsPort
            Deploy-SoftwareAsService -ArchivePath $SoftwareArchivePath -ProcessIdentity $ProcessIdentity -DestinationFolder $DeploymentPath
        }
	}
} # end process block
