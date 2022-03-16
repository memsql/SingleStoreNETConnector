param ($test_block, $target_framework)
$ErrorActionPreference = "Stop"

# Due to complications related to invoking executables within the powershell, this is the handy wrapper around the
# exe calls which exits if a non-zero code is returned (equivalent of 'set -e' in bash).
function Invoke-Executable {
    param (
        [scriptblock]$ScriptBlock,
        [string]$ErrorAction = $ErrorActionPreference
    )
    & @ScriptBlock
    if (($lastexitcode -ne 0) -and $ErrorAction -eq "Stop") {
        exit $lastexitcode
    }
}

cd tests\$test_block
Invoke-Executable -ScriptBlock {dotnet.exe test -f $target_framework -c Release --no-build} -ErrorAction Stop
cd ..\..
