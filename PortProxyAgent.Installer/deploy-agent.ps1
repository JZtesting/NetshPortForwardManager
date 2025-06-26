# PortProxy Agent Deployment Script
param(
    [Parameter(Mandatory=$true)]
    [string[]]$TargetServers,
    
    [string]$MsiPath = ".\bin\Release\PortProxyAgent.msi",
    [string]$SecretKey = "",
    [string]$CentralManagerUrl = "",
    [string]$Environment = "Production",
    [string]$SiloId = "",
    [int]$Port = 8080,
    [bool]$AutoRegister = $false,
    
    [PSCredential]$Credential = $null
)

Write-Host "Deploying PortProxy Agent to $($TargetServers.Count) servers..." -ForegroundColor Green

if (-not (Test-Path $MsiPath)) {
    Write-Error "Installer not found: $MsiPath"
    exit 1
}

# Generate secret key if not provided
if ([string]::IsNullOrEmpty($SecretKey)) {
    $SecretKey = [System.Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
    Write-Host "Generated secret key: $SecretKey" -ForegroundColor Yellow
}

foreach ($server in $TargetServers) {
    Write-Host "`nDeploying to $server..." -ForegroundColor Cyan
    
    try {
        # Copy installer to target server
        $remotePath = "\\$server\c$\temp\PortProxyAgent.msi"
        Write-Host "Copying installer to $server..."
        Copy-Item $MsiPath $remotePath -Force
        
        # Build installation command
        $installArgs = @(
            "/i `"C:\temp\PortProxyAgent.msi`"",
            "/quiet",
            "AGENT_NAME=`"$server`"",
            "AGENT_PORT=`"$Port`"",
            "SECRET_KEY=`"$SecretKey`"",
            "ENVIRONMENT=`"$Environment`""
        )
        
        if (-not [string]::IsNullOrEmpty($CentralManagerUrl)) {
            $installArgs += "CENTRAL_MANAGER_URL=`"$CentralManagerUrl`""
        }
        
        if (-not [string]::IsNullOrEmpty($SiloId)) {
            $installArgs += "SILO_ID=`"$SiloId`""
        }
        
        if ($AutoRegister) {
            $installArgs += "AUTO_REGISTER=`"true`""
        }
        
        $installCommand = "msiexec " + ($installArgs -join " ")
        
        # Execute installation remotely
        Write-Host "Installing on $server..."
        if ($Credential) {
            $result = Invoke-Command -ComputerName $server -Credential $Credential -ScriptBlock {
                param($cmd)
                Invoke-Expression $cmd
                return $LASTEXITCODE
            } -ArgumentList $installCommand
        } else {
            $result = Invoke-Command -ComputerName $server -ScriptBlock {
                param($cmd)
                Invoke-Expression $cmd
                return $LASTEXITCODE
            } -ArgumentList $installCommand
        }
        
        if ($result -eq 0) {
            Write-Host "Successfully installed on $server" -ForegroundColor Green
            
            # Verify service is running
            $serviceStatus = Invoke-Command -ComputerName $server -ScriptBlock {
                Get-Service -Name "PortProxyAgent" -ErrorAction SilentlyContinue
            }
            
            if ($serviceStatus -and $serviceStatus.Status -eq "Running") {
                Write-Host "Service is running on $server" -ForegroundColor Green
                
                # Test health endpoint
                try {
                    $healthUrl = "http://${server}:${Port}/health"
                    $response = Invoke-RestMethod -Uri $healthUrl -TimeoutSec 10
                    Write-Host "Health check passed: $($response.Status)" -ForegroundColor Green
                } catch {
                    Write-Warning "Health check failed for $server : $_"
                }
            } else {
                Write-Warning "Service not running on $server"
            }
        } else {
            Write-Error "Installation failed on $server (exit code: $result)"
        }
        
        # Clean up installer file
        Remove-Item $remotePath -Force -ErrorAction SilentlyContinue
        
    } catch {
        Write-Error "Failed to deploy to $server : $_"
    }
}

Write-Host "`nDeployment completed!" -ForegroundColor Green