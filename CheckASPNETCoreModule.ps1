# ========================================================
# Script للتحقق من تثبيت ASP.NET Core Module وحل خطأ 500.19
# Check ASP.NET Core Module Installation and Fix 500.19 Error
# ========================================================

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "   تشخيص خطأ HTTP 500.19" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# ===== 1. فحص .NET Runtime =====
Write-Host "1. فحص .NET Runtime..." -ForegroundColor Yellow
Write-Host "   ----------------------------------------" -ForegroundColor Gray

$dotnetInstalled = $false
$hasNet8 = $false

try {
    $dotnetVersion = dotnet --version 2>$null
    if ($dotnetVersion) {
        Write-Host "   ✓ .NET SDK/Runtime مثبت - الإصدار: $dotnetVersion" -ForegroundColor Green
        $dotnetInstalled = $true

        Write-Host "`n   Runtimes المثبتة:" -ForegroundColor Cyan
        $runtimes = dotnet --list-runtimes 2>$null

        if ($runtimes) {
            # عرض .NET 8.0 Runtimes فقط
            $net8Runtimes = $runtimes | Where-Object { $_ -like "*8.0*" }

            if ($net8Runtimes) {
                $net8Runtimes | ForEach-Object {
                    Write-Host "   ✓ $_" -ForegroundColor Green
                }
                $hasNet8 = $true
            } else {
                Write-Host "   ⚠ لا يوجد .NET 8.0 Runtime مثبت" -ForegroundColor Yellow
                Write-Host "   Runtimes الموجودة:" -ForegroundColor Gray
                $runtimes | ForEach-Object {
                    Write-Host "     $_" -ForegroundColor Gray
                }
            }

            # التحقق من ASP.NET Core Runtime تحديداً
            $hasAspNetCore8 = $runtimes | Where-Object { $_ -like "*Microsoft.AspNetCore.App*8.0*" }
            if (-not $hasAspNetCore8) {
                Write-Host "`n   ✗ Microsoft.AspNetCore.App 8.0 مفقود!" -ForegroundColor Red
                Write-Host "   هذا هو السبب الرئيسي لخطأ 500.19" -ForegroundColor Yellow
            }
        }
    }
} catch {
    Write-Host "   ✗ dotnet command غير موجود" -ForegroundColor Red
    Write-Host "   السبب: .NET غير مثبت أو PATH غير صحيح" -ForegroundColor Yellow
}

Write-Host ""

# ===== 2. فحص ASP.NET Core Module =====
Write-Host "2. فحص ASP.NET Core Module..." -ForegroundColor Yellow
Write-Host "   ----------------------------------------" -ForegroundColor Gray

$moduleV2Path = "C:\Program Files\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"
$moduleV1Path = "C:\Windows\System32\inetsrv\aspnetcore.dll"

$hasModuleV2 = Test-Path $moduleV2Path
$hasModuleV1 = Test-Path $moduleV1Path

if ($hasModuleV2) {
    Write-Host "   ✓ AspNetCoreModuleV2 موجود" -ForegroundColor Green
    Write-Host "     المسار: $moduleV2Path" -ForegroundColor Gray

    # الحصول على معلومات الملف
    $fileInfo = Get-Item $moduleV2Path
    Write-Host "     الإصدار: $($fileInfo.VersionInfo.FileVersion)" -ForegroundColor Gray
    Write-Host "     تاريخ الإنشاء: $($fileInfo.CreationTime)" -ForegroundColor Gray
} else {
    Write-Host "   ✗ AspNetCoreModuleV2 غير موجود!" -ForegroundColor Red
    Write-Host "     المسار المتوقع: $moduleV2Path" -ForegroundColor Gray
}

if ($hasModuleV1) {
    Write-Host "`n   ℹ AspNetCoreModule (V1) موجود" -ForegroundColor Cyan
    Write-Host "     المسار: $moduleV1Path" -ForegroundColor Gray
    Write-Host "     (V1 قديم، يُفضل استخدام V2)" -ForegroundColor Yellow
}

Write-Host ""

# ===== 3. فحص تسجيل Module في IIS =====
Write-Host "3. فحص تسجيل Module في IIS..." -ForegroundColor Yellow
Write-Host "   ----------------------------------------" -ForegroundColor Gray

try {
    Import-Module WebAdministration -ErrorAction Stop

    # فحص Global Modules
    $moduleV2Registered = Get-WebGlobalModule -Name "AspNetCoreModuleV2" -ErrorAction SilentlyContinue
    $moduleV1Registered = Get-WebGlobalModule -Name "AspNetCoreModule" -ErrorAction SilentlyContinue

    if ($moduleV2Registered) {
        Write-Host "   ✓ AspNetCoreModuleV2 مسجل في IIS" -ForegroundColor Green
        Write-Host "     Image: $($moduleV2Registered.Image)" -ForegroundColor Gray
    } else {
        Write-Host "   ✗ AspNetCoreModuleV2 غير مسجل في IIS" -ForegroundColor Red
    }

    if ($moduleV1Registered) {
        Write-Host "`n   ℹ AspNetCoreModule (V1) مسجل في IIS" -ForegroundColor Cyan
    }

    # عرض جميع Modules
    Write-Host "`n   Modules المسجلة في IIS:" -ForegroundColor Cyan
    Get-WebGlobalModule | Where-Object { $_.Name -like "*AspNet*" } | ForEach-Object {
        Write-Host "     - $($_.Name)" -ForegroundColor Gray
    }

} catch {
    Write-Host "   ⚠ لا يمكن فحص IIS Modules" -ForegroundColor Yellow
    Write-Host "     السبب: $($_.Exception.Message)" -ForegroundColor Gray
    Write-Host "     تأكد من تشغيل PowerShell كـ Administrator" -ForegroundColor Yellow
}

Write-Host ""

# ===== 4. فحص web.config =====
Write-Host "4. فحص web.config..." -ForegroundColor Yellow
Write-Host "   ----------------------------------------" -ForegroundColor Gray

$webConfigPath = "C:\inetpub\wwwroot\ClinicManagment\web.config"

if (Test-Path $webConfigPath) {
    Write-Host "   ✓ ملف web.config موجود" -ForegroundColor Green
    Write-Host "     المسار: $webConfigPath" -ForegroundColor Gray

    try {
        # قراءة وفحص XML
        [xml]$config = Get-Content $webConfigPath -ErrorAction Stop
        Write-Host "   ✓ XML syntax صحيح" -ForegroundColor Green

        # فحص handlers
        $handlers = $config.configuration.'system.webServer'.handlers.add
        if ($handlers) {
            $moduleName = $handlers.modules
            Write-Host "   ✓ Handler مُعرّف - Module: $moduleName" -ForegroundColor Green

            if ($moduleName -eq "AspNetCoreModuleV2") {
                Write-Host "     يستخدم AspNetCoreModuleV2 (صحيح)" -ForegroundColor Green
            } elseif ($moduleName -eq "AspNetCoreModule") {
                Write-Host "     يستخدم AspNetCoreModule V1 (قديم)" -ForegroundColor Yellow
            } else {
                Write-Host "     ⚠ Module غير معروف: $moduleName" -ForegroundColor Yellow
            }
        } else {
            Write-Host "   ⚠ لا يوجد handler مُعرّف" -ForegroundColor Yellow
        }

        # فحص aspNetCore
        $aspNetCore = $config.configuration.'system.webServer'.aspNetCore
        if ($aspNetCore) {
            Write-Host "`n   إعدادات aspNetCore:" -ForegroundColor Cyan
            Write-Host "     - processPath: $($aspNetCore.processPath)" -ForegroundColor Gray
            Write-Host "     - arguments: $($aspNetCore.arguments)" -ForegroundColor Gray
            Write-Host "     - hostingModel: $($aspNetCore.hostingModel)" -ForegroundColor Gray

            # التحقق من وجود DLL
            $dllName = $aspNetCore.arguments -replace '\.\\', ''
            $dllPath = Join-Path (Split-Path $webConfigPath) $dllName
            if (Test-Path $dllPath) {
                Write-Host "   ✓ ملف DLL موجود: $dllName" -ForegroundColor Green
            } else {
                Write-Host "   ✗ ملف DLL مفقود: $dllName" -ForegroundColor Red
            }
        }

    } catch {
        Write-Host "   ✗ خطأ في قراءة web.config" -ForegroundColor Red
        Write-Host "     $($_.Exception.Message)" -ForegroundColor Gray
    }
} else {
    Write-Host "   ✗ ملف web.config مفقود!" -ForegroundColor Red
    Write-Host "     المسار المتوقع: $webConfigPath" -ForegroundColor Gray
}

Write-Host ""

# ===== 5. فحص ملفات التطبيق =====
Write-Host "5. فحص ملفات التطبيق..." -ForegroundColor Yellow
Write-Host "   ----------------------------------------" -ForegroundColor Gray

$projectPath = "C:\inetpub\wwwroot\ClinicManagment"

if (Test-Path $projectPath) {
    Write-Host "   ✓ مجلد المشروع موجود" -ForegroundColor Green

    $requiredFiles = @(
        "ClinicManagementSystem.dll",
        "appsettings.json",
        "web.config"
    )

    foreach ($file in $requiredFiles) {
        $filePath = Join-Path $projectPath $file
        if (Test-Path $filePath) {
            Write-Host "   ✓ $file" -ForegroundColor Green
        } else {
            Write-Host "   ✗ $file مفقود" -ForegroundColor Red
        }
    }

    # فحص مجلد logs
    $logsPath = Join-Path $projectPath "logs"
    if (Test-Path $logsPath) {
        Write-Host "   ✓ مجلد logs موجود" -ForegroundColor Green
    } else {
        Write-Host "   ⚠ مجلد logs مفقود (سيتم إنشاؤه تلقائياً)" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ✗ مجلد المشروع غير موجود!" -ForegroundColor Red
    Write-Host "     المسار المتوقع: $projectPath" -ForegroundColor Gray
}

Write-Host ""

# ===== الخلاصة والتوصيات =====
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "   الخلاصة والتوصيات" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

$issues = @()
$recommendations = @()

# تحليل المشاكل
if (-not $dotnetInstalled) {
    $issues += "✗ .NET غير مثبت"
    $recommendations += "1. ثبّت .NET 8.0 Hosting Bundle من:`n   https://dotnet.microsoft.com/download/dotnet/8.0"
}
elseif (-not $hasNet8) {
    $issues += "✗ .NET 8.0 Runtime غير مثبت"
    $recommendations += "1. ثبّت .NET 8.0 Hosting Bundle من:`n   https://dotnet.microsoft.com/download/dotnet/8.0"
}

if (-not $hasModuleV2) {
    $issues += "✗ AspNetCoreModuleV2 غير مثبت"
    if (-not $hasNet8) {
        $recommendations += "2. بعد تثبيت Hosting Bundle، أعد تشغيل Windows Server (ضروري!)"
    } else {
        $recommendations += "1. أعد تشغيل Windows Server لإتمام تثبيت Module"
    }
}

if ($issues.Count -eq 0) {
    Write-Host "✓ جميع المكونات مثبتة بشكل صحيح!" -ForegroundColor Green
    Write-Host ""
    Write-Host "إذا كان الموقع لا يزال لا يعمل:" -ForegroundColor Yellow
    Write-Host "1. أعد تشغيل IIS: iisreset" -ForegroundColor White
    Write-Host "2. تحقق من سجل الأخطاء في:" -ForegroundColor White
    Write-Host "   C:\inetpub\wwwroot\ClinicManagment\logs\stdout*.log" -ForegroundColor Gray
    Write-Host "3. راجع ملف DEPLOYMENT_GUIDE_AR.md للحلول الأخرى" -ForegroundColor White
} else {
    Write-Host "المشاكل المكتشفة:" -ForegroundColor Red
    foreach ($issue in $issues) {
        Write-Host "  $issue" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "الحلول الموصى بها:" -ForegroundColor Yellow
    foreach ($rec in $recommendations) {
        Write-Host "  $rec" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan

# خيار تحميل Hosting Bundle
if (-not $hasNet8 -or -not $hasModuleV2) {
    Write-Host ""
    Write-Host "هل تريد تحميل .NET 8.0 Hosting Bundle الآن؟ (y/n): " -ForegroundColor Yellow -NoNewline
    $download = Read-Host

    if ($download -eq "y" -or $download -eq "Y") {
        Write-Host ""
        Write-Host "جاري تحميل .NET 8.0 Hosting Bundle..." -ForegroundColor Cyan

        $url = "https://download.visualstudio.microsoft.com/download/pr/3be3a8d3-9b4c-4fcb-80f0-f8eafbfb8e54/81edcc0fdc8e1f2f8a156dbb48db8d5c/dotnet-hosting-8.0.1-win.exe"
        $output = "$env:TEMP\dotnet-hosting-8.0-win.exe"

        try {
            Invoke-WebRequest -Uri $url -OutFile $output -UseBasicParsing
            Write-Host "✓ تم التحميل بنجاح!" -ForegroundColor Green
            Write-Host ""
            Write-Host "الملف محفوظ في: $output" -ForegroundColor Cyan
            Write-Host ""
            Write-Host "الخطوات التالية:" -ForegroundColor Yellow
            Write-Host "1. شغّل الملف لتثبيته" -ForegroundColor White
            Write-Host "2. أعد تشغيل Windows Server (ضروري!)" -ForegroundColor White
            Write-Host "3. شغّل هذا السكريبت مرة أخرى للتحقق" -ForegroundColor White
            Write-Host ""

            # فتح المجلد
            Start-Process explorer.exe -ArgumentList $env:TEMP
        } catch {
            Write-Host "✗ فشل التحميل: $($_.Exception.Message)" -ForegroundColor Red
            Write-Host "حمّله يدوياً من: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
        }
    }
}

Write-Host ""
Write-Host "للمزيد من المساعدة، راجع:" -ForegroundColor Cyan
Write-Host "- FIX_HTTP_500_19_ERROR_AR.md" -ForegroundColor Gray
Write-Host "- DEPLOYMENT_GUIDE_AR.md" -ForegroundColor Gray
Write-Host ""

Read-Host "اضغط Enter للخروج"
