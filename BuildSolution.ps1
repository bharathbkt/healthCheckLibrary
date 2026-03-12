$slnName = "HealthCheckPOC"
$baseDir = "d:\HealthCheckPOC"

cd $baseDir

# Create Projects
$projects = @(
    "src\HealthCheckPOC.Domain.Shared",
    "src\HealthCheckPOC.Domain",
    "src\HealthCheckPOC.Application.Contracts",
    "src\HealthCheckPOC.Application",
    "src\HealthCheckPOC.EntityFrameworkCore",
    "src\HealthCheckPOC.HttpApi.Host"
)

foreach ($proj in $projects) {
    if ($proj -like "*.Host") {
        dotnet new webapi -n $($proj.Split('\')[-1]) -o $proj --force
    } else {
        dotnet new classlib -n $($proj.Split('\')[-1]) -o $proj --force
    }
    
    # Fix TargetFramework to net6.0
    $csprojPath = "$proj\$($proj.Split('\')[-1]).csproj"
    $content = Get-Content $csprojPath
    $content = $content -replace "<TargetFramework>.*</TargetFramework>", "<TargetFramework>net6.0</TargetFramework>"
    Set-Content $csprojPath $content

    dotnet sln $slnName.sln add $csprojPath
    # Remove default class/controller
    if (Test-Path "$proj\Class1.cs") { Remove-Item "$proj\Class1.cs" }
    if (Test-Path "$proj\Controllers\WeatherForecastController.cs") { Remove-Item "$proj\Controllers\WeatherForecastController.cs" -Force }
    if (Test-Path "$proj\WeatherForecast.cs") { Remove-Item "$proj\WeatherForecast.cs" -Force }
}

# Add references
dotnet add src\HealthCheckPOC.Domain\HealthCheckPOC.Domain.csproj reference src\HealthCheckPOC.Domain.Shared\HealthCheckPOC.Domain.Shared.csproj

dotnet add src\HealthCheckPOC.Application.Contracts\HealthCheckPOC.Application.Contracts.csproj reference src\HealthCheckPOC.Domain.Shared\HealthCheckPOC.Domain.Shared.csproj

dotnet add src\HealthCheckPOC.Application\HealthCheckPOC.Application.csproj reference src\HealthCheckPOC.Domain\HealthCheckPOC.Domain.csproj
dotnet add src\HealthCheckPOC.Application\HealthCheckPOC.Application.csproj reference src\HealthCheckPOC.Application.Contracts\HealthCheckPOC.Application.Contracts.csproj

dotnet add src\HealthCheckPOC.EntityFrameworkCore\HealthCheckPOC.EntityFrameworkCore.csproj reference src\HealthCheckPOC.Domain\HealthCheckPOC.Domain.csproj

dotnet add src\HealthCheckPOC.HttpApi.Host\HealthCheckPOC.HttpApi.Host.csproj reference src\HealthCheckPOC.Application\HealthCheckPOC.Application.csproj
dotnet add src\HealthCheckPOC.HttpApi.Host\HealthCheckPOC.HttpApi.Host.csproj reference src\HealthCheckPOC.EntityFrameworkCore\HealthCheckPOC.EntityFrameworkCore.csproj
dotnet add src\HealthCheckPOC.HttpApi.Host\HealthCheckPOC.HttpApi.Host.csproj reference src\HealthMonitoringModule\HealthMonitoringModule.csproj
dotnet sln $slnName.sln add src\HealthMonitoringModule\HealthMonitoringModule.csproj

# Add ABP Packages
dotnet add src\HealthCheckPOC.Domain.Shared\HealthCheckPOC.Domain.Shared.csproj package Volo.Abp.Core -v 6.0.0
dotnet add src\HealthCheckPOC.Domain\HealthCheckPOC.Domain.csproj package Volo.Abp.Ddd.Domain -v 6.0.0
dotnet add src\HealthCheckPOC.Application.Contracts\HealthCheckPOC.Application.Contracts.csproj package Volo.Abp.Ddd.Application.Contracts -v 6.0.0
dotnet add src\HealthCheckPOC.Application\HealthCheckPOC.Application.csproj package Volo.Abp.Ddd.Application -v 6.0.0
dotnet add src\HealthCheckPOC.EntityFrameworkCore\HealthCheckPOC.EntityFrameworkCore.csproj package Volo.Abp.EntityFrameworkCore -v 6.0.0
dotnet add src\HealthCheckPOC.HttpApi.Host\HealthCheckPOC.HttpApi.Host.csproj package Volo.Abp.AspNetCore.Mvc -v 6.0.0
dotnet add src\HealthCheckPOC.HttpApi.Host\HealthCheckPOC.HttpApi.Host.csproj package Volo.Abp.Autofac -v 6.0.0
