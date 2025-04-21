param(
    [Parameter()]
    [string]$SwitchName = "ADLabSwitch",
    
    [Parameter()]
    [string]$NetAdapterName = (Get-NetAdapter | Where-Object { $_.Status -eq 'Up' } | Select-Object -First 1).Name,

    [Parameter(Mandatory=$true)]
    [FileInfo]$ISOPath,

    [Parameter(Mandatory=$true)]
    [DirectoryInfo]$VMPath,

    [Parameter(Mandatory=$true)]
    [DirectoryInfo]$VHDPath,

    [string]$DomainName = "AcmeLab.test"
)






function Create-LabSwitch {
    param (
        [string]$SwitchName = "ADLabSwitch",
        [string]$NetAdapterName = (Get-NetAdapter | Where-Object { $_.Status -eq 'Up' } | Select-Object -First 1).Name
    )

    # Überprüfen, ob der Switch bereits vorhanden ist
    $existingSwitch = Get-VMSwitch -Name $SwitchName -ErrorAction SilentlyContinue

    if ($existingSwitch) {
        Write-Host "Der Switch '$SwitchName' existiert bereits."

        # Überprüfen, ob der Switch die richtigen Eigenschaften hat
        if ($existingSwitch.SwitchType -eq 'External' -and $existingSwitch.NetAdapterInterfaceDescription -eq (Get-NetAdapter -Name $NetAdapterName).InterfaceDescription) {
            Write-Host "Der Switch '$SwitchName' hat die richtigen Eigenschaften."
            return $existingSwitch
        } else {
            Write-Host "Der Switch '$SwitchName' hat nicht die richtigen Eigenschaften. Er wird neu konfiguriert."
            Remove-VMSwitch -Name $SwitchName -Force
        }
    }

    # Erstellen des neuen Switches
    try {
        $newSwitch = New-VMSwitch -Name $SwitchName -NetAdapterName $NetAdapterName -AllowManagementOS $true
        Write-Host "Der Switch '$SwitchName' wurde erfolgreich erstellt."
        return $newSwitch
    } catch {
        Write-Error "Fehler beim Erstellen des Switches: $_"
        return $null
    }
}

function Create-LabVM {
    param (
        [Parameter(Mandatory=$true)]
        [string]$VMName,
        
        [Parameter(Mandatory=$true)]
        [string]$VHDPath,

        [Parameter(Mandatory=$true)]
        [string]$SwitchName,

        [int]$CPUCount = 2
    )


    if (-not (Get-VM -Name $vmName -ErrorAction SilentlyContinue)) {
        New-VM -Name $VMName -MemoryStartupBytes 2GB -NewVHDPath "$VHDPath\$VMName.vhdx" -NewVHDSizeBytes 100GB -Generation 2 -SwitchName $SwitchName
        Set-VM -Name $VMName -ProcessorCount $CPUCount
        Add-VMDvdDrive -VMName $VMName -Path $ISOPath

        Write-Output "VM '$vmName' wurde erfolgreich erstellt und die ISO-Datei angehängt."
    } else {
        Write-Output "VM '$vmName' existiert bereits."
    }

    Start-VM -Name $VMName
}


# Funktion zum Konfigurieren des Domain Controllers
function Configure-LabServer {
    param (
        [string]$VMName,
        [string]$DomainName,

        [Parameter(Mandatory=$true)]
        [ValidateSet('ADDS','ADCS','Web')]
        [string]$Role
    )

    if($Role -eq "ADDS") {
        Invoke-Command -VMName $VMName -ScriptBlock {
            # Warte bis der Server neu gestartet wurde
            while (-not (Test-Connection -ComputerName localhost -Quiet)) {
                Start-Sleep -Seconds 5
            }

            # Installiere Active Directory Domain Services
            Install-WindowsFeature -Name AD-Domain-Services -IncludeManagementTools -Restart

            # Warte bis der Server neu gestartet wurde
            while (-not (Test-Connection -ComputerName localhost -Quiet)) {
                Start-Sleep -Seconds 5
            }

            # Konfiguriere den Domain Controller
            Import-Module ADDSDeployment
            Install-ADDSForest -CreateDnsDelegation:$false -DatabasePath "C:\Windows\NTDS" -DomainMode "WinThreshold" -DomainName $using:DomainName -DomainNetbiosName "ADCSTEST" -ForestMode "WinThreshold" -InstallDns:$true -LogPath "C:\Windows\NTDS" -NoRebootOnCompletion:$false -SysvolPath "C:\Windows\SYSVOL" -Force:$true
        }
    }

    else if ($Role -eq "ADCS") {
        Invoke-Command -VMName $VMName -ScriptBlock {
            # Warte bis der Server neu gestartet wurde
            while (-not (Test-Connection -ComputerName localhost -Quiet)) {
                Start-Sleep -Seconds 5
            }

            # Join der VM zur Domäne
            Add-Computer -DomainName $using:DomainName -Restart -Credential (Get-Credential -Message "Geben Sie die Anmeldeinformationen eines Domänenadministrators ein.")

            # Warte bis der Server neu gestartet wurde
            while (-not (Test-Connection -ComputerName localhost -Quiet)) {
                Start-Sleep -Seconds 5
            }

            # Installiere Active Directory Certificate Services
            Install-WindowsFeature -Name ADCS-Cert-Authority -IncludeManagementTools -Restart

            # Warte bis der Server neu gestartet wurde
            while (-not (Test-Connection -ComputerName localhost -Quiet)) {
                Start-Sleep -Seconds 5
            }

            # Konfiguriere ADCS im Enterprise-Modus
            Install-AdcsCertificationAuthority -CAType EnterpriseRootCA -KeyLength 2048 -HashAlgorithmName SHA256 -ValidityPeriod Years -ValidityPeriodUnits 10 -Force
        }
    }

    else if ($Role -eq "Web") {
        Invoke-Command -VMName $VMName -ScriptBlock {
            # Warte bis der Server neu gestartet wurde
            while (-not (Test-Connection -ComputerName localhost -Quiet)) {
                Start-Sleep -Seconds 5
            }    
        
            # Join der VM zur Domäne
            Add-Computer -DomainName $using:DomainName -Restart -Credential (Get-Credential -Message "Geben Sie die Anmeldeinformationen eines Domänenadministrators ein.")

            # Warte bis der Server neu gestartet wurde
            while (-not (Test-Connection -ComputerName localhost -Quiet)) {
                Start-Sleep -Seconds 5
            }

            # Installiere Internet Information Services
            Install-WindowsFeature -Name Web-Server -IncludeManagementTools -Restart
        }
    }
}



$switch = Create-LabSwitch -SwitchName $SwitchName -NetAdapterName $NetAdapterName

Create-LabVM -VMName "acme-adds" -VHDPath $VHDPath -SwitchName $SwitchName -CPUCount 2
Configure-LabServer -VMName "acme-adds" -Domain "" -Role "ADDS"

Create-LabVM -VMName "acme-adcs" -VHDPath $VHDPath -SwitchName $SwitchName -CPUCount 2
Configure-LabServer -VMName "acme-adds" -Domain "" -Role "ADCS"

Create-LabVM -VMName "acme-web" -VHDPath $VHDPath -SwitchName $SwitchName -CPUCount 2
Configure-LabServer -VMName "acme-adds" -Domain "" -Role "Web"
