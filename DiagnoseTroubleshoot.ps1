# ===============================================
# Script لتشخيص وحل مشاكل نشر ASP.NET Core على IIS
# ===============================================

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "بدء تشخيص المشكلة..." -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# المسار الافتراضي للمشروع
$projectPath = "C:\inetpub\wwwroot\ClinicManagment"

# 1. التحقق من تثبيت .NET Runtime
Write-Host "1. التحقق من تثبيت .NET Runtime..." -ForegroundColor Yellow
try {
    $runtimes = dotnet --list-runtimes
    Write-Host "✓ .NET Runtimes المثبتة:" -ForegroundColor Green
    $runtimes | Where-Object { $_ -like "*Microsoft.AspNetCore.App*" -or $_ -like "*Microsoft.NETCore.App*" }

    $hasNet8 = $runtimes | Where-Object { $_ -like "*8.0.*" }
    if (-not $hasNet8) {
        Write-Host "⚠ تحذير: لا يوجد .NET 8.0 Runtime مثبت!" -ForegroundColor Red
        Write-Host "  قم بتحميله من: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    }
} catch {
    Write-Host "✗ خطأ: .NET Runtime غير مثبت!" -ForegroundColor Red
    Write-Host "  قم بتحميل وتثبيت .NET 8.0 Hosting Bundle" -ForegroundColor Yellow
}
Write-Host ""

# 2. التحقق من وجود الملفات المطلوبة
Write-Host "2. التحقق من وجود ملفات المشروع..." -ForegroundColor Yellow
$requiredFiles = @(
    "ClinicManagementSystem.dll",
    "appsettings.json",
    "web.config"
)

foreach ($file in $requiredFiles) {
    $filePath = Join-Path $projectPath $file
    if (Test-Path $filePath) {
        Write-Host "✓ $file موجود" -ForegroundColor Green
    } else {
        Write-Host "✗ $file مفقود!" -ForegroundColor Red
    }
}
Write-Host ""

# 3. إنشاء مجلد Logs إذا لم يكن موجوداً
Write-Host "3. التحقق من مجلد السجلات..." -ForegroundColor Yellow
$logsPath = Join-Path $projectPath "logs"
if (-not (Test-Path $logsPath)) {
    Write-Host "  إنشاء مجلد logs..." -ForegroundColor Yellow
    New-Item -Path $logsPath -ItemType Directory -Force | Out-Null
    Write-Host "✓ تم إنشاء مجلد logs" -ForegroundColor Green
} else {
    Write-Host "✓ مجلد logs موجود" -ForegroundColor Green
}
Write-Host ""

# 4. التحقق من الأذونات
Write-Host "4. التحقق من أذونات المجلد..." -ForegroundColor Yellow
try {
    $acl = Get-Acl $projectPath
    $hasIISPermissions = $acl.Access | Where-Object {
        $_.IdentityReference -like "*IIS_IUSRS*" -or
        $_.IdentityReference -like "*IUSR*"
    }

    if ($hasIISPermissions) {
        Write-Host "✓ الأذونات موجودة لـ IIS" -ForegroundColor Green
    } else {
        Write-Host "⚠ تحذير: قد تكون الأذونات ناقصة" -ForegroundColor Yellow
        Write-Host "  يمكنك إضافتها يدوياً من Properties → Security" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠ لم يتمكن من فحص الأذونات" -ForegroundColor Yellow
}
Write-Host ""

# 5. اختبار الاتصال بقاعدة البيانات
Write-Host "5. اختبار الاتصال بقاعدة البيانات..." -ForegroundColor Yellow
$dbServer = "doctordb.c3eoy02guueq.me-south-1.rds.amazonaws.com"
$dbPort = 1433

try {
    $tcpClient = New-Object System.Net.Sockets.TcpClient
    $connect = $tcpClient.BeginConnect($dbServer, $dbPort, $null, $null)
    $wait = $connect.AsyncWaitHandle.WaitOne(3000, $false)

    if ($wait) {
        $tcpClient.EndConnect($connect)
        Write-Host "✓ الاتصال بقاعدة البيانات يعمل!" -ForegroundColor Green
        $tcpClient.Close()
    } else {
        Write-Host "✗ فشل الاتصال بقاعدة البيانات!" -ForegroundColor Red
        Write-Host "  تحقق من:" -ForegroundColor Yellow
        Write-Host "    - Security Group في AWS RDS يسمح بالمنفذ 1433" -ForegroundColor Yellow
        Write-Host "    - Network connectivity من Windows Server إلى AWS" -ForegroundColor Yellow
    }
} catch {
    Write-Host "✗ فشل الاتصال بقاعدة البيانات!" -ForegroundColor Red
    Write-Host "  الخطأ: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# 6. التحقق من IIS Application Pool
Write-Host "6. فحص IIS Application Pools..." -ForegroundColor Yellow
try {
    Import-Module WebAdministration -ErrorAction Stop

    $pools = Get-ChildItem IIS:\AppPools
    if ($pools) {
        Write-Host "✓ Application Pools الموجودة:" -ForegroundColor Green
        foreach ($pool in $pools) {
            $managedRuntime = $pool.managedRuntimeVersion
            if ($managedRuntime -eq "") {
                $runtime = "No Managed Code (صحيح لـ .NET Core) ✓"
                $color = "Green"
            } else {
                $runtime = "$managedRuntime (يجب تغييره إلى No Managed Code!)"
                $color = "Yellow"
            }
            Write-Host "  - $($pool.name): $runtime" -ForegroundColor $color
        }
    }
} catch {
    Write-Host "⚠ لا يمكن الوصول لـ IIS - تأكد من تشغيل PowerShell كـ Administrator" -ForegroundColor Yellow
}
Write-Host ""

# 7. عرض آخر سجلات الأخطاء إن وجدت
Write-Host "7. فحص سجلات الأخطاء..." -ForegroundColor Yellow
$logFiles = Get-ChildItem -Path $logsPath -Filter "stdout*.log" -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1

if ($logFiles) {
    Write-Host "✓ آخر ملف سجل: $($logFiles.Name)" -ForegroundColor Green
    Write-Host "  آخر تعديل: $($logFiles.LastWriteTime)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "محتوى آخر 30 سطر من السجل:" -ForegroundColor Cyan
    Write-Host "----------------------------------------" -ForegroundColor Gray
    Get-Content $logFiles.FullName -Tail 30 -ErrorAction SilentlyContinue
    Write-Host "----------------------------------------" -ForegroundColor Gray
} else {
    Write-Host "⚠ لا توجد سجلات بعد. جرب تشغيل الموقع أولاً." -ForegroundColor Yellow
}
Write-Host ""

# 8. فحص Event Viewer للأخطاء
Write-Host "8. فحص آخر أخطاء IIS من Event Viewer..." -ForegroundColor Yellow
try {
    $errors = Get-EventLog -LogName Application -Source "*IIS*" -Newest 5 -EntryType Error -ErrorAction SilentlyContinue
    if ($errors) {
        Write-Host "✓ آخر 5 أخطاء من IIS:" -ForegroundColor Yellow
        foreach ($error in $errors) {
            Write-Host "  [$($error.TimeGenerated)] $($error.Message.Substring(0, [Math]::Min(100, $error.Message.Length)))..." -ForegroundColor Red
        }
    } else {
        Write-Host "✓ لا توجد أخطاء حديثة في Event Viewer" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠ لم يتمكن من قراءة Event Viewer" -ForegroundColor Yellow
}
Write-Host ""

# 9. الحلول المقترحة
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "الحلول المقترحة:" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. تأكد من تثبيت .NET 8.0 Hosting Bundle وإعادة تشغيل الخادم" -ForegroundColor White
Write-Host "2. تأكد من أن Application Pool يستخدم 'No Managed Code'" -ForegroundColor White
Write-Host "3. تأكد من السماح بالاتصال بـ AWS RDS (Security Group)" -ForegroundColor White
Write-Host "4. افحص ملف السجل في: $logsPath" -ForegroundColor White
Write-Host "5. أعد تشغيل IIS بالأمر: iisreset" -ForegroundColor White
Write-Host "6. اقرأ الدليل الكامل في: DEPLOYMENT_GUIDE_AR.md" -ForegroundColor White
Write-Host ""

# إعادة تشغيل IIS (اختياري)
Write-Host "هل تريد إعادة تشغيل IIS الآن؟ (y/n): " -ForegroundColor Yellow -NoNewline
$restart = Read-Host
if ($restart -eq "y" -or $restart -eq "Y") {
    Write-Host "إعادة تشغيل IIS..." -ForegroundColor Yellow
    iisreset
    Write-Host "✓ تم إعادة تشغيل IIS" -ForegroundColor Green
}

Write-Host ""
Write-Host "انتهى التشخيص!" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
