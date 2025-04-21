$InformationPreference = 'Continue'
$ErrorPreference = 'Stop'

$LabDir = "E:\Lab";

$OriginalIso = "E:\Lab\Windows2025.iso";
$ModifiedIso = "E:\Lab\Windows2025-Unattended.iso";

if(!(Test-Path $ModifiedIso)) {
	& .\Create-UnattendedIso.ps1 -SourceISOPath $OriginalIso -DestinationISOPath $ModifiedIso
}