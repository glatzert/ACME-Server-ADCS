param (
    [string]$SourceISOPath,
    [string]$DestinationISOPath,
    [string]$Language = "en-US",
    [string]$KeyboardLayout = "0407",  # German Keyboard Layout
    [string]$TimeZone = "W. Europe Standard Time"
)

try {
    $parent = [System.IO.Path]::GetTempPath()
    $name = [System.IO.Path]::GetRandomFileName()
    $OutputFolder = New-Item -ItemType Directory -Path (Join-Path $parent $name);

    Write-Information "Created $OutputFolder as temporary folder for ISO extraction."

    # Mount the source ISO
    Write-Information "Mounting $SourceISOPath as Virtual DVD ..."
    $mountResult = Mount-DiskImage -ImagePath $SourceISOPath -PassThru
    $driveLetter = ($mountResult | Get-Volume).DriveLetter
    Write-Information "Mounted $SourceISOPath to $driveLetter ..."

    Write-Information "Use robocopy to copy ISO contents to temporary folder ..."
    # Copy all files from the mounted ISO to the output folder
    Robocopy "$($driveLetter):\" $OutputFolder /E /COPYALL /XJ | Out-Null
    Write-Information "Copied data from ISO to temporary folder."

    $isoLabel = (Get-Volume -DriveLetter $driveLetter).FileSystemLabel
    Dismount-DiskImage $driveLetter;

    # Create the Autounattend.xml file
    $autounattendContent = @"
<?xml version="1.0" encoding="utf-8"?>
<unattend xmlns="urn:schemas-microsoft-com:unattend">
    <settings pass="windowsPE">
        <component name="Microsoft-Windows-International-Core-WinPE" processorArchitecture="amd64" publicKeyToken="31bf3856ad364e35" language="neutral" versionScope="nonSxS" xmlns:wcm="http://schemas.microsoft.com/WMIConfig/2002/State" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
            <SetupUILanguage>
                <UILanguage>$Language</UILanguage>
            </SetupUILanguage>
            <InputLocale>$KeyboardLayout</InputLocale>
            <UserLocale>$Language</UserLocale>
            <SystemLocale>$Language</SystemLocale>
            <UILanguageFallback>$Language</UILanguageFallback>
        </component>
    </settings>
    <settings pass="oobeSystem">
        <component name="Microsoft-Windows-Shell-Setup" processorArchitecture="amd64" publicKeyToken="31bf3856ad364e35" language="neutral" versionScope="nonSxS" xmlns:wcm="http://schemas.microsoft.com/WMIConfig/2002/State" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
            <OOBE>
                <HideEULAPage>true</HideEULAPage>
                <ProtectYourPC>1</ProtectYourPC>
                <SkipMachineOOBE>true</SkipMachineOOBE>
                <SkipUserOOBE>true</SkipUserOOBE>
            </OOBE>
            <UserAccounts>
                <LocalAccounts>
                    <LocalAccount wcm:action="add">
                        <Password>
                            <Value>UABAc3N3b3JkITE=</Value> <!-- Base64 encoded password for P@ssword!1 -->
                            <PlainText>false</PlainText>
                        </Password>
                        <Name>Administrator</Name>
                        <Group>Administrators</Group>
                    </LocalAccount>
                </LocalAccounts>
            </UserAccounts>
            <TimeZone>$TimeZone</TimeZone>
            <RegisteredOwner>Your Name</RegisteredOwner>
            <RegisteredOrganization>Your Organization</RegisteredOrganization>
        </component>
        <component name="Microsoft-Windows-International-Core" processorArchitecture="amd64" publicKeyToken="31bf3856ad364e35" language="neutral" versionScope="nonSxS" xmlns:wcm="http://schemas.microsoft.com/WMIConfig/2002/State" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
            <InputLocale>$KeyboardLayout</InputLocale>
            <UserLocale>$Language</UserLocale>
            <SystemLocale>$Language</SystemLocale>
            <UILanguage>$Language</UILanguage>
            <UILanguageFallback>$Language</UILanguageFallback>
        </component>
    </settings>
</unattend>
"@

    # Save the Autounattend.xml file to the output folder
    $autounattendPath = Join-Path -Path $OutputFolder -ChildPath "Autounattend.xml"
    Set-Content -Path $autounattendPath -Value $autounattendContent

    # Create a new ISO image with the modified files
    $isoImage = New-IsoImage -Path $OutputFolder -BootFile "$OutputFolder\boot\etfsboot.com" -Media DVDPLUSR -BootSectorId "Microsoft Corporation/Windows" -PrepBoot -UdfVersion "1.02" -VolumeLabel $isoLabel

    # Export the new ISO image to the destination path
    $isoStream = [IO.File]::OpenWrite($DestinationISOPath)
    $isoImage | ForEach-Object { $isoStream.Write($_, 0, $_.Length) }
    $isoStream.Close()

    # Dismount the source ISO
    Dismount-DiskImage -ImagePath $SourceISOPath

    Write-Host "Automated ISO created at $DestinationISOPath"
}
finally {
    Remove-Item $OutputFolder -Recurse -Force
    Write-Information "Removed temporary Folder: $OutputFolder".
}