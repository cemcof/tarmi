$ErrorActionPreference = "Stop";

$Env:CI='1'
$Env:APP_VERSION='1.4.55'
$Env:CONFIGURATION='Release'

$version=$Env:APP_VERSION
$configuration=$Env:CONFIGURATION
$rid = "win-x64"
$outputDir = "${PSScriptRoot}\publish\${configuration}"

function Ensure-Directory([String] $path)
{
    [System.IO.Directory]::CreateDirectory($path)
}

dotnet tool install --global wix --version 6.0.0
wix extension -g add WixToolset.Util.wixext
wix extension -g add WixToolset.UI.wixext

$projects = @(
    ('src\App\installer\installer.csproj', 'installer'),
    ('src\App\CFLMnavi\CFLMnavi.csproj', 'CFLMnavi')
)

dotnet restore --configfile .\nuget.config -r "${rid}" .\CFLMnavi.sln

Ensure-Directory($outputDir)
foreach ($projectDef in $projects) {
    $projectName = $projectDef[1]
    $publishDir = "${outputDir}\${projectName}"
    Ensure-Directory($publishDir)

    dotnet publish --configuration "${configuration}" --no-restore --output "${publishDir}" -r "${rid}" -bl:"${outputDir}\${projectName}.binlog" $projectDef[0]
}

# build msi and bundle
& "${outputDir}\installer\installer.exe" bundle build -v="${version}" -o="${outputDir}" -r=8 -i="${outputDir}\CFLMnavi"
#& "${outputDir}\installer\installer.exe" bundle build -v="${version}" -o="${outputDir}" -d="${outputDir}\windowsdesktop-runtime-win-x64.exe" -i="${outputDir}\CFLMnavi"

$configuration='Debug'
$outputDir = "${PSScriptRoot}\publish\${configuration}"

Ensure-Directory($outputDir)
foreach ($projectDef in $projects) {
    $projectName = $projectDef[1]
    $publishDir = "${outputDir}\${projectName}"
    Ensure-Directory($publishDir)

    dotnet publish --configuration "${configuration}" --no-restore --output "${publishDir}" -r "${rid}" -bl:"${outputDir}\${projectName}.binlog" $projectDef[0]
}

# build msi and bundle
& "${outputDir}\installer\installer.exe" bundle build -v="${version}" -o="${outputDir}" -r=8 -i="${outputDir}\CFLMnavi"
#& "${outputDir}\installer\installer.exe" bundle build -v="${version}" -o="${outputDir}" -d="${outputDir}\windowsdesktop-runtime-win-x64.exe" -i="${outputDir}\CFLMnavi"
