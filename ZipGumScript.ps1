# Define the folder and zip file paths using TeamCity variables
$folderToZip = Join-Path -Path "%teamcity.build.checkoutDir%" -ChildPath "Gum/bin/Debug/"
$zipFilePath = Join-Path -Path "%teamcity.build.checkoutDir%" -ChildPath "Gum.zip"

# Check if the folder exists
if (Test-Path $folderToZip) {
    # Attempt to zip the folder
    Compress-Archive -Path "$folderToZip\*" -DestinationPath $zipFilePath
    
    # Check if the zip file exists
    if (Test-Path $zipFilePath) {
        # Output the path and size of the zip file
        $zipFileSize = (Get-Item $zipFilePath).Length
        Write-Output "Zipping successful. File path: $zipFilePath"
        Write-Output "File size: $($zipFileSize / 1MB) MB"
    } else {
        Write-Output "Zipping failed. File not found at $zipFilePath"
    }
} else {
    Write-Output "Folder not found at $folderToZip"
}