param([string]$ServiceName)

$ServiceName = $ServiceName.Trim()
$ServiceNameProper = (Get-Culture).TextInfo.ToTitleCase($ServiceName.ToLower())

$ScriptDir = $PSScriptRoot
if ([string]::IsNullOrEmpty($ScriptDir)) {
    $ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$SrcRoot = Join-Path $ScriptDir "src"
$TestsRoot = Join-Path $ScriptDir "tests"

if (-not (Test-Path $SrcRoot)) {
    New-Item -ItemType Directory -Path $SrcRoot -Force | Out-Null
}
if (-not (Test-Path $TestsRoot)) {
    New-Item -ItemType Directory -Path $TestsRoot -Force | Out-Null
}

Write-Host "Creating $ServiceNameProper Service..."

# Folder service chính
$ServiceRoot = Join-Path $SrcRoot "$ServiceNameProper.Service"
$UnitTestRoot = Join-Path $TestsRoot "$ServiceNameProper.Service.UnitTests"
$IntegrationTestRoot = Join-Path $TestsRoot "$ServiceNameProper.Service.IntegrationTests"

# Tạo thư mục chính
foreach ($dir in @($ServiceRoot, $UnitTestRoot, $IntegrationTestRoot)) {
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
}

# Tạo các subdirectories trong service
$subDirs = @(
    "Domain\Entities",
    "Domain\Interfaces",
    "Application\Interfaces",
    "Application\Services",
    "Infrastructure\Repositories",
    "Api\Controllers"
)

foreach ($subDir in $subDirs) {
    New-Item -ItemType Directory -Path (Join-Path $ServiceRoot $subDir) -Force | Out-Null
}

# ===== DOMAIN LAYER =====
$entityContent = "namespace ${ServiceNameProper}.Service.Domain.Entities;

public class ${ServiceNameProper}
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
"
$entityContent | Out-File -FilePath (Join-Path $ServiceRoot "Domain\Entities\$ServiceNameProper.cs") -Encoding utf8

$interfaceContent = "using ${ServiceNameProper}.Service.Domain.Entities;

namespace ${ServiceNameProper}.Service.Domain.Interfaces;

public interface I${ServiceNameProper}Repository
{
    Task<${ServiceNameProper}?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<${ServiceNameProper}>> GetAllAsync(CancellationToken ct = default);
    Task<${ServiceNameProper}> CreateAsync(${ServiceNameProper} entity, CancellationToken ct = default);
    Task UpdateAsync(${ServiceNameProper} entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
"
$interfaceContent | Out-File -FilePath (Join-Path $ServiceRoot "Domain\Interfaces\I$ServiceNameProper.cs") -Encoding utf8

@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
"@ | Out-File -FilePath (Join-Path $ServiceRoot "Domain\${ServiceNameProper}.Service.Domain.csproj") -Encoding utf8

# ===== APPLICATION LAYER =====
$appInterfaceContent = "using ${ServiceNameProper}.Service.Domain.Entities;

namespace ${ServiceNameProper}.Service.Application.Interfaces;

public interface I${ServiceNameProper}Service
{
    Task<${ServiceNameProper}?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<${ServiceNameProper}>> GetAllAsync(CancellationToken ct = default);
    Task<${ServiceNameProper}> CreateAsync(Create${ServiceNameProper}Request request, CancellationToken ct = default);
    Task UpdateAsync(Guid id, Update${ServiceNameProper}Request request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public record Create${ServiceNameProper}Request();
public record Update${ServiceNameProper}Request();
"
$appInterfaceContent | Out-File -FilePath (Join-Path $ServiceRoot "Application\Interfaces\I$ServiceNameProper.cs") -Encoding utf8

$serviceContent = "using ${ServiceNameProper}.Service.Application.Interfaces;
using ${ServiceNameProper}.Service.Domain.Entities;
using ${ServiceNameProper}.Service.Domain.Interfaces;

namespace ${ServiceNameProper}.Service.Application.Services;

public class ${ServiceNameProper}Service : I${ServiceNameProper}Service
{
    private readonly I${ServiceNameProper}Repository _repository;

    public ${ServiceNameProper}Service(I${ServiceNameProper}Repository repository)
    {
        _repository = repository;
    }

    public async Task<${ServiceNameProper}?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _repository.GetByIdAsync(id, ct);
    }

    public async Task<IEnumerable<${ServiceNameProper}>> GetAllAsync(CancellationToken ct = default)
    {
        return await _repository.GetAllAsync(ct);
    }

    public async Task<${ServiceNameProper}> CreateAsync(Create${ServiceNameProper}Request request, CancellationToken ct = default)
    {
        var entity = new ${ServiceNameProper} { };
        return await _repository.CreateAsync(entity, ct);
    }

    public async Task UpdateAsync(Guid id, Update${ServiceNameProper}Request request, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct);
        if (entity is null)
            throw new KeyNotFoundException();
        await _repository.UpdateAsync(entity, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _repository.DeleteAsync(id, ct);
    }
}
"
$serviceContent | Out-File -FilePath (Join-Path $ServiceRoot "Application\Services\$ServiceNameProper.cs") -Encoding utf8

@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Domain\${ServiceNameProper}.Service.Domain.csproj" />
  </ItemGroup>
</Project>
"@ | Out-File -FilePath (Join-Path $ServiceRoot "Application\${ServiceNameProper}.Service.Application.csproj") -Encoding utf8

# ===== INFRASTRUCTURE LAYER =====
$repoContent = "using ${ServiceNameProper}.Service.Domain.Entities;
using ${ServiceNameProper}.Service.Domain.Interfaces;

namespace ${ServiceNameProper}.Service.Infrastructure.Repositories;

public class ${ServiceNameProper}Repository : I${ServiceNameProper}Repository
{
    public async Task<${ServiceNameProper}?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return null;
    }

    public async Task<IEnumerable<${ServiceNameProper}>> GetAllAsync(CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return Enumerable.Empty<${ServiceNameProper}>();
    }

    public async Task<${ServiceNameProper}> CreateAsync(${ServiceNameProper} entity, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return entity;
    }

    public async Task UpdateAsync(${ServiceNameProper} entity, CancellationToken ct = default)
    {
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await Task.CompletedTask;
    }
}
"
$repoContent | Out-File -FilePath (Join-Path $ServiceRoot "Infrastructure\Repositories\$ServiceNameProper.cs") -Encoding utf8

@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Domain\${ServiceNameProper}.Service.Domain.csproj" />
  </ItemGroup>
</Project>
"@ | Out-File -FilePath (Join-Path $ServiceRoot "Infrastructure\${ServiceNameProper}.Service.Infrastructure.csproj") -Encoding utf8

# ===== API LAYER =====
$apiProgramContent = "using ${ServiceNameProper}.Service.Application.Interfaces;
using ${ServiceNameProper}.Service.Application.Services;
using ${ServiceNameProper}.Service.Domain.Interfaces;
using ${ServiceNameProper}.Service.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<I${ServiceNameProper}Service, ${ServiceNameProper}Service>();
builder.Services.AddScoped<I${ServiceNameProper}Repository, ${ServiceNameProper}Repository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
"
$apiProgramContent | Out-File -FilePath (Join-Path $ServiceRoot "Api\Program.cs") -Encoding utf8

$controllerContent = "using Microsoft.AspNetCore.Mvc;
using ${ServiceNameProper}.Service.Application.Interfaces;

namespace ${ServiceNameProper}.Service.Api.Controllers;

[ApiController]
[Route(""api/[controller]"")]
public class ${ServiceNameProper}Controller : ControllerBase
{
    private readonly I${ServiceNameProper}Service _service;

    public ${ServiceNameProper}Controller(I${ServiceNameProper}Service service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        return Ok(await _service.GetAllAsync(ct));
    }

    [HttpGet(""{id}"")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Create${ServiceNameProper}Request request, CancellationToken ct = default)
    {
        var result = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut(""{id}"")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Update${ServiceNameProper}Request request, CancellationToken ct = default)
    {
        await _service.UpdateAsync(id, request, ct);
        return NoContent();
    }

    [HttpDelete(""{id}"")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
"
$controllerContent | Out-File -FilePath (Join-Path $ServiceRoot "Api\Controllers\$($ServiceNameProper)Controller.cs") -Encoding utf8

@"
{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }
  },
  ""AllowedHosts"": ""*""
}
"@ | Out-File -FilePath (Join-Path $ServiceRoot "Api\appsettings.json") -Encoding utf8

@"
{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Debug"",
      ""Microsoft.AspNetCore"": ""Information""
    }
  }
}
"@ | Out-File -FilePath (Join-Path $ServiceRoot "Api\appsettings.Development.json") -Encoding utf8

@"
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Application\${ServiceNameProper}.Service.Application.csproj" />
    <ProjectReference Include="..\Infrastructure\${ServiceNameProper}.Service.Infrastructure.csproj" />
  </ItemGroup>
</Project>
"@ | Out-File -FilePath (Join-Path $ServiceRoot "Api\${ServiceNameProper}.Service.Api.csproj") -Encoding utf8

# ===== UNIT TESTS =====
$unitTestContent = "using ${ServiceNameProper}.Service.Application.Interfaces;
using ${ServiceNameProper}.Service.Application.Services;
using ${ServiceNameProper}.Service.Domain.Entities;
using ${ServiceNameProper}.Service.Domain.Interfaces;
using Moq;

namespace ${ServiceNameProper}.Service.UnitTests;

public class ${ServiceNameProper}ServiceTests
{
    private readonly Mock<I${ServiceNameProper}Repository> _mockRepo;
    private readonly ${ServiceNameProper}Service _service;

    public ${ServiceNameProper}ServiceTests()
    {
        _mockRepo = new Mock<I${ServiceNameProper}Repository>();
        _service = new ${ServiceNameProper}Service(_mockRepo.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsEntity_WhenExists()
    {
        var id = Guid.NewGuid();
        var expected = new ${ServiceNameProper} { Id = id };
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await _service.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        var id = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((${ServiceNameProper}?)null);

        var result = await _service.GetByIdAsync(id);

        Assert.Null(result);
    }
}
"
$unitTestContent | Out-File -FilePath (Join-Path $UnitTestRoot "$($ServiceNameProper)ServiceTests.cs") -Encoding utf8

@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""coverlet.collector"" Version=""6.0.2"">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <PackageReference Include=""Microsoft.NET.Test.Sdk"" Version=""17.12.0"" />
    <PackageReference Include=""Moq"" Version=""4.20.72"" />
    <PackageReference Include=""xunit"" Version=""2.9.2"" />
    <PackageReference Include=""xunit.runner.visualstudio"" Version=""2.8.2"">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""..\..\src\$ServiceNameProper\Service\Application\${ServiceNameProper}.Service.Application.csproj"" />
  </ItemGroup>
</Project>
"@ | Out-File -FilePath (Join-Path $UnitTestRoot "$ServiceNameProper.Service.UnitTests.csproj") -Encoding utf8

# ===== INTEGRATION TESTS =====
$integrationTestContent = "using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ${ServiceNameProper}.Service.IntegrationTests;

public class ${ServiceNameProper}IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ${ServiceNameProper}IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_ReturnsSuccessStatusCode()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(""api/${ServiceNameProper}"");
        response.EnsureSuccessStatusCode();
    }
}
"
$integrationTestContent | Out-File -FilePath (Join-Path $IntegrationTestRoot "$($ServiceNameProper)IntegrationTests.cs") -Encoding utf8

@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""coverlet.collector"" Version=""6.0.2"">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <PackageReference Include=""Microsoft.AspNetCore.Mvc.Testing"" Version=""10.0.0"" />
    <PackageReference Include=""Microsoft.NET.Test.Sdk"" Version=""17.12.0"" />
    <PackageReference Include=""xunit"" Version=""2.9.2"" />
    <PackageReference Include=""xunit.runner.visualstudio"" Version=""2.8.2"">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""..\..\src\$ServiceNameProper\Service\Api\${ServiceNameProper}.Service.Api.csproj"" />
  </ItemGroup>
</Project>
"@ | Out-File -FilePath (Join-Path $IntegrationTestRoot "$ServiceNameProper.Service.IntegrationTests.csproj") -Encoding utf8

Write-Host "Done! Service '$ServiceNameProper' created."
Write-Host ""
Write-Host "Structure:"
Write-Host "  src/"
Write-Host "    - $ServiceNameProper.Service/"
Write-Host "        - Domain/"
Write-Host "        - Application/"
Write-Host "        - Infrastructure/"
Write-Host "        - Api/"
Write-Host "  tests/"
Write-Host "    - $ServiceNameProper.Service.UnitTests"
Write-Host "    - $ServiceNameProper.Service.IntegrationTests"
