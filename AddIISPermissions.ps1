# ========================================================
# Script لإضافة صلاحيات IIS تلقائياً لمجلد التطبيق
# Add IIS Permissions Automatically
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
Write-Host "   إضافة صلاحيات IIS للتطبيق" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# مسار المجلد الافتراضي
$defaultPath = "C:\inetpub\wwwroot\ClinicManagment"

# اسأل المستخدم عن المسار
Write-Host "مسار مجلد التطبيق:" -ForegroundColor Yellow
Write-Host "الافتراضي: $defaultPath" -ForegroundColor Gray
Write-Host ""
$path = Read-Host "اضغط Enter للاستخدام الافتراضي أو اكتب المسار الجديد"

if ([string]::IsNullOrWhiteSpace($path)) {
    $path = $defaultPath
}

# التحقق من وجود المجلد
if (-not (Test-Path $path)) {
    Write-Host ""
    Write-Host "❌ المجلد غير موجود: $path" -ForegroundColor Red
    Write-Host ""
    Read-Host "اضغط Enter للخروج"
    exit
}

Write-Host ""
Write-Host "✓ المجلد موجود: $path" -ForegroundColor Green
Write-Host ""

# عرض الصلاحيات الحالية
Write-Host "الصلاحيات الحالية للمجلد:" -ForegroundColor Cyan
Write-Host "----------------------------------------" -ForegroundColor Gray
try {
    $currentAcl = Get-Acl $path
    $iisPermissions = $currentAcl.Access | Where-Object {
        $_.IdentityReference -like "*IIS*" -or
        $_.IdentityReference -like "*IUSR*"
    }

    if ($iisPermissions) {
        $iisPermissions | Format-Table IdentityReference, FileSystemRights, AccessControlType -AutoSize
    } else {
        Write-Host "  لا توجد صلاحيات IIS حالياً" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  لم يتمكن من قراءة الصلاحيات" -ForegroundColor Yellow
}
Write-Host ""

# السؤال عن المتابعة
Write-Host "هل تريد إضافة/تحديث صلاحيات IIS؟ (y/n): " -ForegroundColor Yellow -NoNewline
$continue = Read-Host
if ($continue -ne "y" -and $continue -ne "Y") {
    Write-Host "تم الإلغاء." -ForegroundColor Gray
    exit
}

Write-Host ""
Write-Host "جاري إضافة الصلاحيات..." -ForegroundColor Cyan
Write-Host ""

# إضافة صلاحيات IIS_IUSRS
Write-Host "1. إضافة صلاحيات IIS_IUSRS..." -ForegroundColor Yellow
try {
    $acl = Get-Acl $path
    $permission = "BUILTIN\IIS_IUSRS", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
    $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
    $acl.SetAccessRule($accessRule)
    Set-Acl $path $acl
    Write-Host "   ✓ تمت إضافة صلاحيات IIS_IUSRS" -ForegroundColor Green
} catch {
    Write-Host "   ⚠ خطأ في إضافة IIS_IUSRS: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   جرب الطريقة البديلة..." -ForegroundColor Yellow
    try {
        icacls $path /grant "IIS_IUSRS:(OI)(CI)RX" /T | Out-Null
        Write-Host "   ✓ تمت الإضافة بالطريقة البديلة" -ForegroundColor Green
    } catch {
        Write-Host "   ✗ فشلت الإضافة" -ForegroundColor Red
    }
}

# إضافة صلاحيات IUSR
Write-Host "2. إضافة صلاحيات IUSR..." -ForegroundColor Yellow
try {
    $acl = Get-Acl $path
    $permission = "NT AUTHORITY\IUSR", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
    $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
    $acl.SetAccessRule($accessRule)
    Set-Acl $path $acl
    Write-Host "   ✓ تمت إضافة صلاحيات IUSR" -ForegroundColor Green
} catch {
    Write-Host "   ⚠ خطأ في إضافة IUSR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   جرب الطريقة البديلة..." -ForegroundColor Yellow
    try {
        icacls $path /grant "IUSR:(OI)(CI)RX" /T | Out-Null
        Write-Host "   ✓ تمت الإضافة بالطريقة البديلة" -ForegroundColor Green
    } catch {
        Write-Host "   ✗ فشلت الإضافة" -ForegroundColor Red
    }
}

# إضافة صلاحيات إضافية لمجلد logs
$logsPath = Join-Path $path "logs"
if (Test-Path $logsPath) {
    Write-Host "3. إضافة صلاحيات الكتابة لمجلد logs..." -ForegroundColor Yellow
    try {
        # IIS_IUSRS يحتاج صلاحية Modify لكتابة السجلات
        $acl = Get-Acl $logsPath
        $permission = "BUILTIN\IIS_IUSRS", "Modify", "ContainerInherit,ObjectInherit", "None", "Allow"
        $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
        $acl.SetAccessRule($accessRule)
        Set-Acl $logsPath $acl
        Write-Host "   ✓ تمت إضافة صلاحيات الكتابة لمجلد logs" -ForegroundColor Green
    } catch {
        Write-Host "   ⚠ خطأ في إضافة صلاحيات logs: $($_.Exception.Message)" -ForegroundColor Red
        try {
            icacls $logsPath /grant "IIS_IUSRS:(OI)(CI)M" /T | Out-Null
            Write-Host "   ✓ تمت الإضافة بالطريقة البديلة" -ForegroundColor Green
        } catch {
            Write-Host "   ✗ فشلت الإضافة" -ForegroundColor Red
        }
    }
} else {
    Write-Host "3. إنشاء مجلد logs..." -ForegroundColor Yellow
    try {
        New-Item -Path $logsPath -ItemType Directory -Force | Out-Null
        Write-Host "   ✓ تم إنشاء مجلد logs" -ForegroundColor Green

        # إضافة صلاحيات الكتابة
        $acl = Get-Acl $logsPath
        $permission = "BUILTIN\IIS_IUSRS", "Modify", "ContainerInherit,ObjectInherit", "None", "Allow"
        $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
        $acl.SetAccessRule($accessRule)
        Set-Acl $logsPath $acl
        Write-Host "   ✓ تمت إضافة صلاحيات الكتابة" -ForegroundColor Green
    } catch {
        Write-Host "   ⚠ لم يتمكن من إنشاء مجلد logs" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "✓ تم الانتهاء!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# عرض الصلاحيات النهائية
Write-Host "الصلاحيات النهائية:" -ForegroundColor Cyan
Write-Host "----------------------------------------" -ForegroundColor Gray
try {
    $finalAcl = Get-Acl $path
    $finalPermissions = $finalAcl.Access | Where-Object {
        $_.IdentityReference -like "*IIS*" -or
        $_.IdentityReference -like "*IUSR*"
    }

    if ($finalPermissions) {
        $finalPermissions | Format-Table IdentityReference, FileSystemRights, AccessControlType -AutoSize
        Write-Host "✓ الصلاحيات تم تطبيقها بنجاح!" -ForegroundColor Green
    } else {
        Write-Host "⚠ تحذير: لا يمكن العثور على صلاحيات IIS" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠ لم يتمكن من قراءة الصلاحيات النهائية" -ForegroundColor Yellow
}

Write-Host ""

# السؤال عن إعادة تشغيل IIS
Write-Host "هل تريد إعادة تشغيل IIS الآن؟ (y/n): " -ForegroundColor Yellow -NoNewline
$restartIIS = Read-Host
if ($restartIIS -eq "y" -or $restartIIS -eq "Y") {
    Write-Host ""
    Write-Host "جاري إعادة تشغيل IIS..." -ForegroundColor Cyan
    try {
        iisreset
        Write-Host "✓ تم إعادة تشغيل IIS بنجاح" -ForegroundColor Green
    } catch {
        Write-Host "✗ فشل إعادة تشغيل IIS: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "الخطوات التالية:" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "1. افتح Chrome واذهب إلى localhost:5000" -ForegroundColor White
Write-Host "2. حاول تسجيل الدخول" -ForegroundColor White
Write-Host "3. إذا استمر الخطأ، اقرأ سجل الأخطاء:" -ForegroundColor White
Write-Host "   $logsPath\stdout*.log" -ForegroundColor Gray
Write-Host "4. راجع ملف DEPLOYMENT_GUIDE_AR.md للحلول الأخرى" -ForegroundColor White
Write-Host ""
Write-Host "حظاً موفقاً! 🚀" -ForegroundColor Green
Write-Host ""

Read-Host "اضغط Enter للخروج"
