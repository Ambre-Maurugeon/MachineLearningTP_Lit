param(
    [Parameter(Mandatory = $true)]
	[string]$config,
	
    [string]$runid,
    [string]$initializefrom,
    [string]$copyto,
	
	[switch]$force,
    [switch]$resume
)

# ============================== CONFIG ==============================
$model  = "PinballFlipper.onnx"


# ========================= COMMAND BUILDER ==========================
$a = @($config)	
if(-not [string]::IsNullOrWhiteSpace($runid)) { $a += "--run-id=$runid" }
if(-not [string]::IsNullOrWhiteSpace($initializefrom)) { $a += "--initialize-from=$initializefrom" }
if ($resume) { $a += "--resume" }
if ($force)  { $a += "--force" }

# ============================== RUN =================================
Write-Host "Executing: mlagents-learn $($a -join ' ')"
mlagents-learn @a

# ============================== COPY ================================
if (-not [string]::IsNullOrWhiteSpace($copyto) -and $LASTEXITCODE -eq 0) {
    New-Item -ItemType Directory -Force $copyto | Out-Null
    Copy-Item "results\$runid\$model" $copyto
}