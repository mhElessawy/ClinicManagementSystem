# ========================================================
# Script لحل مشكلة Port in Use - إيقاف العمليات المتعارضة
# Fix Port 5000 Conflict - Stop Conflicting Processes
# ========================================================

# التحقق من تشغيل PowerShell كـ Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "❌ يجب تشغيل PowerShell كـ Administrator!" -ForegroundColor Red
    Write-Host ""
    Write-Host "الخطوات:" -ForegroundColor Yellow
    Write-Host "1. اضغط زر Windows" -ForegroundColor White
    Write-Host "2. اكتب: PowerShell" -ForegroundColor White
    Write-Host "3. كليك يمين على 'Windows PowerShell'" -ForegroundColor White
    Write-Host "4. اختر 'Run as administrator'" -ForegroundColor White
    Write-Host "5. شغّل السكريبت مرة أخرى" -ForegroundColor White
    Write-Host ""
    Read-Host "اضغط Enter للخروج"
    exit
}

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "   حل مشكلة Port مستخدم" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# المنفذ المطلوب فحصه
$targetPort = 5000

# 1. فحص Port 5000
Write-Host "1. فحص Port $targetPort..." -ForegroundColor Yellow
$connections = Get-NetTCPConnection -LocalPort $targetPort -ErrorAction SilentlyContinue

if ($connections) {
    Write-Host "   ✗ Port $targetPort مستخدم من قبل:" -ForegroundColor Red
    Write-Host ""

    $uniqueProcesses = $connections | Select-Object -ExpandProperty OwningProcess -Unique

    foreach ($pid in $uniqueProcesses) {
        try {
            $process = Get-Process -Id $pid -ErrorAction Stop
            Write-Host "   Process ID: $pid" -ForegroundColor Yellow
            Write-Host "   اسم العملية: $($process.ProcessName)" -ForegroundColor Yellow
            Write-Host "   المسار: $($process.Path)" -ForegroundColor Gray
            Write-Host "   بدأت في: $($process.StartTime)" -ForegroundColor Gray
            Write-Host ""
        } catch {
            Write-Host "   Process ID: $pid (لم يتمكن من الحصول على التفاصيل)" -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "   ✓ Port $targetPort حر (غير مستخدم)" -ForegroundColor Green
    Write-Host ""
    Write-Host "يمكنك الآن تشغيل الموقع من IIS Manager!" -ForegroundColor Green
    Write-Host ""
    Read-Host "اضغط Enter للخروج"
    exit
}

# 2. فحص عمليات dotnet
Write-Host "2. فحص عمليات dotnet..." -ForegroundColor Yellow
$dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue

if ($dotnetProcesses) {
    Write-Host "   ✗ يوجد $($dotnetProcesses.Count) عمليات dotnet قيد التشغيل:" -ForegroundColor Red
    $dotnetProcesses | Format-Table Id, ProcessName, StartTime, @{Name="Memory (MB)"; Expression={[math]::Round($_.WorkingSet64 / 1MB, 2)}} -AutoSize
} else {
    Write-Host "   ✓ لا توجد عمليات dotnet" -ForegroundColor Green
}
Write-Host ""

# 3. فحص IIS Application Pools
Write-Host "3. فحص IIS Application Pools..." -ForegroundColor Yellow
try {
    Import-Module WebAdministration -ErrorAction Stop
    $pools = Get-ChildItem IIS:\AppPools -ErrorAction Stop | Where-Object { $_.State -eq "Started" }

    if ($pools) {
        Write-Host "   Application Pools قيد التشغيل:" -ForegroundColor Yellow
        $pools | Format-Table Name, State, @{Name="ProcessModel"; Expression={$_.processModel.identityType}} -AutoSize
    } else {
        Write-Host "   ⚠ لا توجد Application Pools قيد التشغيل" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ⚠ لا يمكن الوصول لـ IIS (تأكد من تثبيت IIS)" -ForegroundColor Yellow
}
Write-Host ""

# عرض خيارات الحل
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "   خيارات الحل:" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "1. إيقاف العملية المستخدمة لـ Port $targetPort فقط (موصى به)" -ForegroundColor White
Write-Host "2. إيقاف جميع عمليات dotnet" -ForegroundColor White
Write-Host "3. إعادة تشغيل IIS بالكامل" -ForegroundColor White
Write-Host "4. إيقاف كل شيء (dotnet + IIS)" -ForegroundColor White
Write-Host "0. إلغاء" -ForegroundColor Gray
Write-Host ""
Write-Host -NoNewline "اختر (1-4 أو 0 للإلغاء): " -ForegroundColor Yellow
$choice = Read-Host

Write-Host ""

switch ($choice) {
    "1" {
        Write-Host "جاري إيقاف العملية المستخدمة لـ Port $targetPort..." -ForegroundColor Cyan

        $connections = Get-NetTCPConnection -LocalPort $targetPort -ErrorAction SilentlyContinue
        if ($connections) {
            $uniqueProcesses = $connections | Select-Object -ExpandProperty OwningProcess -Unique

            foreach ($pid in $uniqueProcesses) {
                try {
                    $process = Get-Process -Id $pid -ErrorAction Stop
                    Write-Host "   إيقاف: $($process.ProcessName) (PID: $pid)" -ForegroundColor Yellow
                    Stop-Process -Id $pid -Force
                    Write-Host "   ✓ تم إيقاف العملية" -ForegroundColor Green
                } catch {
                    Write-Host "   ✗ فشل إيقاف العملية: $($_.Exception.Message)" -ForegroundColor Red
                }
            }
        }
    }

    "2" {
        Write-Host "جاري إيقاف جميع عمليات dotnet..." -ForegroundColor Cyan

        $dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
        if ($dotnetProcesses) {
            foreach ($proc in $dotnetProcesses) {
                Write-Host "   إيقاف: dotnet.exe (PID: $($proc.Id))" -ForegroundColor Yellow
                Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
            }
            Write-Host "   ✓ تم إيقاف جميع عمليات dotnet" -ForegroundColor Green
        } else {
            Write-Host "   ⚠ لا توجد عمليات dotnet للإيقاف" -ForegroundColor Yellow
        }
    }

    "3" {
        Write-Host "جاري إعادة تشغيل IIS..." -ForegroundColor Cyan
        Write-Host ""

        Write-Host "   إيقاف IIS..." -ForegroundColor Yellow
        iisreset /stop | Out-Null
        Start-Sleep -Seconds 3

        Write-Host "   تشغيل IIS..." -ForegroundColor Yellow
        iisreset /start | Out-Null

        Write-Host "   ✓ تم إعادة تشغيل IIS" -ForegroundColor Green
    }

    "4" {
        Write-Host "جاري إيقاف كل شيء..." -ForegroundColor Cyan
        Write-Host ""

        # إيقاف dotnet
        Write-Host "   إيقاف عمليات dotnet..." -ForegroundColor Yellow
        Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force
        Write-Host "   ✓ تم إيقاف dotnet" -ForegroundColor Green

        # إعادة تشغيل IIS
        Write-Host "   إعادة تشغيل IIS..." -ForegroundColor Yellow
        iisreset /restart | Out-Null
        Write-Host "   ✓ تم إعادة تشغيل IIS" -ForegroundColor Green
    }

    "0" {
        Write-Host "تم الإلغاء." -ForegroundColor Gray
        Write-Host ""
        Read-Host "اضغط Enter للخروج"
        exit
    }

    default {
        Write-Host "اختيار غير صحيح!" -ForegroundColor Red
        Write-Host ""
        Read-Host "اضغط Enter للخروج"
        exit
    }
}

Write-Host ""
Start-Sleep -Seconds 2

# التحقق من النتيجة
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "   التحقق من النتيجة..." -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "فحص Port $targetPort..." -ForegroundColor Yellow
$finalCheck = Get-NetTCPConnection -LocalPort $targetPort -ErrorAction SilentlyContinue

if ($finalCheck) {
    Write-Host "✗ Port $targetPort لا يزال مستخدم!" -ForegroundColor Red
    Write-Host ""
    Write-Host "جرب الحلول التالية:" -ForegroundColor Yellow
    Write-Host "1. أغلق Visual Studio إذا كان مفتوحاً" -ForegroundColor White
    Write-Host "2. أغلق أي Command Prompt أو PowerShell تشغل dotnet run" -ForegroundColor White
    Write-Host "3. أعد تشغيل Windows Server" -ForegroundColor White
} else {
    Write-Host "✓ Port $targetPort أصبح حراً!" -ForegroundColor Green
    Write-Host ""
    Write-Host "=========================================" -ForegroundColor Green
    Write-Host "   ✓ تم حل المشكلة!" -ForegroundColor Green
    Write-Host "=========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "الخطوات التالية:" -ForegroundColor Cyan
    Write-Host "1. افتح IIS Manager" -ForegroundColor White
    Write-Host "2. اختر Sites → ClinicManagment" -ForegroundColor White
    Write-Host "3. اضغط 'Start' من الجانب الأيمن" -ForegroundColor White
    Write-Host "4. افتح Chrome واذهب إلى: http://localhost:5000" -ForegroundColor White
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan

# عرض معلومات إضافية
Write-Host ""
Write-Host "معلومات إضافية:" -ForegroundColor Cyan
Write-Host "----------------------------------------" -ForegroundColor Gray

# فحص ملفات المشروع
$projectPath = "C:\inetpub\wwwroot\ClinicManagment"
if (Test-Path $projectPath) {
    Write-Host "✓ مجلد المشروع موجود: $projectPath" -ForegroundColor Green

    $dllPath = Join-Path $projectPath "ClinicManagementSystem.dll"
    if (Test-Path $dllPath) {
        Write-Host "✓ ملف DLL موجود" -ForegroundColor Green
    } else {
        Write-Host "⚠ ملف DLL مفقود! يجب نشر المشروع أولاً" -ForegroundColor Yellow
    }

    $webConfigPath = Join-Path $projectPath "web.config"
    if (Test-Path $webConfigPath) {
        Write-Host "✓ ملف web.config موجود" -ForegroundColor Green
    } else {
        Write-Host "⚠ ملف web.config مفقود!" -ForegroundColor Yellow
    }
} else {
    Write-Host "⚠ مجلد المشروع غير موجود: $projectPath" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "للمزيد من المساعدة، راجع:" -ForegroundColor Cyan
Write-Host "- FIX_PORT_IN_USE_ERROR_AR.md" -ForegroundColor Gray
Write-Host "- DEPLOYMENT_GUIDE_AR.md" -ForegroundColor Gray
Write-Host ""

Read-Host "اضغط Enter للخروج"
