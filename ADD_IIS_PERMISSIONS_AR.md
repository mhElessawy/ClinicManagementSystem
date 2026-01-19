# 🔐 دليل إضافة صلاحيات IIS للمجلد

## المشكلة
عند فحص صلاحيات مجلد `C:\inetpub\wwwroot\ClinicManagment`، لا يوجد المستخدمان:
- **IIS_IUSRS**
- **IUSR**

هذا يمنع IIS من قراءة ملفات التطبيق ويسبب خطأ HTTP 500.

---

## ✅ الحل: إضافة الصلاحيات يدوياً

### الطريقة الأولى: عن طريق واجهة Windows (GUI)

#### الخطوة 1️⃣: افتح خصائص المجلد
1. افتح Windows Explorer
2. اذهب إلى: `C:\inetpub\wwwroot\ClinicManagment`
3. **كليك يمين** على المجلد `ClinicManagment`
4. اختر **Properties** (خصائص)

#### الخطوة 2️⃣: افتح إعدادات الأمان (Security)
1. اختر تبويب **Security**
2. اضغط على زر **Edit...** (تحرير)

```
┌─────────────────────────────────────────┐
│  ClinicManagment Properties             │
│  ─────────────────────────────────────  │
│  General | Security | Previous Versions │
│                                         │
│  Group or user names:                   │
│  ┌─────────────────────────────────┐   │
│  │ SYSTEM                          │   │
│  │ Administrators                  │   │
│  │ Users                           │   │
│  └─────────────────────────────────┘   │
│                                         │
│  [  Edit...  ]  [ Advanced ]           │
└─────────────────────────────────────────┘
```

#### الخطوة 3️⃣: إضافة مستخدم جديد
1. في النافذة الجديدة، اضغط **Add...** (إضافة)
2. اضغط على زر **Advanced...** (متقدم)

```
┌─────────────────────────────────────────┐
│  Select Users or Groups                 │
│  ─────────────────────────────────────  │
│  Select this object type:               │
│  Users, Groups, or Built-in...  [Types]│
│                                         │
│  From this location:                    │
│  YOUR-SERVER-NAME              [Locations]│
│                                         │
│  Enter object name:                     │
│  ┌─────────────────────────────────┐   │
│  │                                 │   │
│  └─────────────────────────────────┘   │
│                                         │
│  [ Advanced... ]  [ Check Names ]      │
│  [    OK    ]     [   Cancel   ]       │
└─────────────────────────────────────────┘
```

#### الخطوة 4️⃣: البحث عن IIS_IUSRS
1. اضغط **Advanced...**
2. اضغط **Find Now** (ابحث الآن)
3. ستظهر قائمة بجميع المستخدمين والمجموعات
4. **ابحث عن:** `IIS_IUSRS` في القائمة
5. **اضغط عليه مرة واحدة** لتحديده
6. اضغط **OK**
7. اضغط **OK** مرة أخرى

```
┌───────────────────────────────────────────────────┐
│  Select Users or Groups                           │
│  ───────────────────────────────────────────────  │
│  Find Now Results:                                │
│  ┌───────────────────────────────────────────┐   │
│  │ Name                    │ Folder            │   │
│  │ ────────────────────────┼──────────────────│   │
│  │ Administrators          │ Builtin           │   │
│  │ Backup Operators        │ Builtin           │   │
│  │ Guests                  │ Builtin           │   │
│  │ IIS_IUSRS              │ Builtin  ← اختر   │   │  ✓✓✓
│  │ IUSR                    │ NT AUTHORITY      │   │
│  │ Network Service         │ NT AUTHORITY      │   │
│  │ Performance Monitor... │ Builtin           │   │
│  │ Users                   │ Builtin           │   │
│  └───────────────────────────────────────────┘   │
│                                                   │
│  [    OK    ]     [   Cancel   ]                 │
└───────────────────────────────────────────────────┘
```

#### الخطوة 5️⃣: تحديد الصلاحيات لـ IIS_IUSRS
1. بعد إضافة `IIS_IUSRS`، سيظهر في القائمة
2. **اختر IIS_IUSRS** من القائمة
3. في الجزء السفلي "Permissions for IIS_IUSRS"
4. ضع علامة ✓ على:
   - ✅ **Read & execute** (قراءة وتنفيذ)
   - ✅ **List folder contents** (عرض محتويات المجلد)
   - ✅ **Read** (قراءة)

```
┌─────────────────────────────────────────┐
│  Permissions for ClinicManagment        │
│  ─────────────────────────────────────  │
│  Group or user names:                   │
│  ┌─────────────────────────────────┐   │
│  │ SYSTEM                          │   │
│  │ Administrators                  │   │
│  │ IIS_IUSRS                      │ ← │
│  │ Users                           │   │
│  └─────────────────────────────────┘   │
│                                         │
│  Permissions for IIS_IUSRS:             │
│                           Allow | Deny  │
│  ┌─────────────────────────────────┐   │
│  │ ☑ Full control          │   │   │   │
│  │ ☐ Modify                │   │   │   │
│  │ ☑ Read & execute       │   │   │ ← │ ✓✓✓
│  │ ☑ List folder contents │   │   │   │ ✓✓✓
│  │ ☑ Read                 │   │   │ ← │ ✓✓✓
│  │ ☐ Write                 │   │   │   │
│  └─────────────────────────────────┘   │
│                                         │
│  [    OK    ]     [   Cancel   ]       │
└─────────────────────────────────────────┘
```

5. اضغط **Apply** ثم **OK**

#### الخطوة 6️⃣: كرر نفس العملية لإضافة IUSR
1. اضغط **Add...** مرة أخرى
2. اضغط **Advanced...**
3. اضغط **Find Now**
4. ابحث عن **IUSR** في القائمة
5. اختره واضغط **OK**
6. حدد نفس الصلاحيات:
   - ✅ Read & execute
   - ✅ List folder contents
   - ✅ Read
7. اضغط **Apply** ثم **OK**

---

### الطريقة الثانية: عن طريق PowerShell (أسرع!)

افتح **PowerShell كـ Administrator** واكتب:

```powershell
# تحديد مسار المجلد
$path = "C:\inetpub\wwwroot\ClinicManagment"

# إضافة صلاحيات IIS_IUSRS
$acl = Get-Acl $path
$permission = "BUILTIN\IIS_IUSRS", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
$acl.SetAccessRule($accessRule)
Set-Acl $path $acl

Write-Host "✓ تمت إضافة صلاحيات IIS_IUSRS" -ForegroundColor Green

# إضافة صلاحيات IUSR
$acl = Get-Acl $path
$permission = "NT AUTHORITY\IUSR", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
$acl.SetAccessRule($accessRule)
Set-Acl $path $acl

Write-Host "✓ تمت إضافة صلاحيات IUSR" -ForegroundColor Green

# عرض الصلاحيات الحالية
Write-Host "`nالصلاحيات الحالية للمجلد:" -ForegroundColor Cyan
(Get-Acl $path).Access | Where-Object {
    $_.IdentityReference -like "*IIS_IUSRS*" -or
    $_.IdentityReference -like "*IUSR*"
} | Format-Table IdentityReference, FileSystemRights, AccessControlType
```

**انسخ والصق هذا الكود بالكامل وسيتم كل شيء تلقائياً!**

---

### الطريقة الثالثة: عن طريق Command Prompt

افتح **Command Prompt كـ Administrator** واكتب:

```cmd
REM إضافة صلاحيات IIS_IUSRS
icacls "C:\inetpub\wwwroot\ClinicManagment" /grant "BUILTIN\IIS_IUSRS:(OI)(CI)RX" /T

REM إضافة صلاحيات IUSR
icacls "C:\inetpub\wwwroot\ClinicManagment" /grant "NT AUTHORITY\IUSR:(OI)(CI)RX" /T

REM عرض الصلاحيات
icacls "C:\inetpub\wwwroot\ClinicManagment"
```

**معنى الرموز:**
- `(OI)` = Object Inherit - تطبق على الملفات
- `(CI)` = Container Inherit - تطبق على المجلدات الفرعية
- `RX` = Read & Execute - قراءة وتنفيذ
- `/T` = تطبيق على جميع المجلدات والملفات الفرعية

---

## ✅ التحقق من نجاح الإضافة

### الطريقة 1: من Windows Explorer
1. اذهب إلى `C:\inetpub\wwwroot\ClinicManagment`
2. كليك يمين → Properties → Security
3. يجب أن تشاهد الآن:
   - ✅ **IIS_IUSRS**
   - ✅ **IUSR**

### الطريقة 2: من PowerShell
```powershell
(Get-Acl "C:\inetpub\wwwroot\ClinicManagment").Access |
    Where-Object { $_.IdentityReference -like "*IIS*" -or $_.IdentityReference -like "*IUSR*" } |
    Format-Table -AutoSize
```

يجب أن تظهر النتيجة:
```
IdentityReference      FileSystemRights  AccessControlType
─────────────────      ────────────────  ─────────────────
BUILTIN\IIS_IUSRS     ReadAndExecute    Allow
NT AUTHORITY\IUSR     ReadAndExecute    Allow
```

---

## 🔄 بعد إضافة الصلاحيات

**1. أعد تشغيل IIS:**
```cmd
iisreset
```

**2. أعد تشغيل Application Pool:**
- افتح IIS Manager
- اذهب إلى Application Pools
- اختر الـ Pool الخاص بالموقع
- كليك يمين → **Recycle**

**3. جرب الموقع مرة أخرى:**
- افتح Chrome
- اذهب إلى `http://localhost:5000`
- حاول تسجيل الدخول

---

## ⚠️ إذا ظهر خطأ "IIS_IUSRS not found" في PowerShell

هذا يعني أن IIS غير مثبت بشكل صحيح. استخدم هذا الكود:

```powershell
# التحقق من تثبيت IIS
$iisFeature = Get-WindowsFeature -Name Web-Server
if ($iisFeature.Installed) {
    Write-Host "✓ IIS مثبت" -ForegroundColor Green
} else {
    Write-Host "✗ IIS غير مثبت!" -ForegroundColor Red
    Write-Host "يجب تثبيت IIS أولاً" -ForegroundColor Yellow
}

# إذا كان IIS مثبت لكن IIS_IUSRS غير موجود، استخدم هذا:
$path = "C:\inetpub\wwwroot\ClinicManagment"

# جرب بدون BUILTIN
icacls $path /grant "IIS_IUSRS:(OI)(CI)RX" /T
icacls $path /grant "IUSR:(OI)(CI)RX" /T
```

---

## 🎯 ملخص سريع (Quick Reference)

**أسرع طريقة (PowerShell):**
```powershell
# انسخ والصق هذا السطرين
icacls "C:\inetpub\wwwroot\ClinicManagment" /grant "BUILTIN\IIS_IUSRS:(OI)(CI)RX" /T
icacls "C:\inetpub\wwwroot\ClinicManagment" /grant "NT AUTHORITY\IUSR:(OI)(CI)RX" /T
iisreset
```

**تم!** 🎉

---

## 📞 مشاكل شائعة

### المشكلة 1: "Access Denied" عند تنفيذ الأوامر
**الحل:** شغّل PowerShell أو CMD كـ **Administrator**

### المشكلة 2: لا يمكن إيجاد IIS_IUSRS في قائمة البحث
**الحل:**
1. تأكد من تثبيت IIS بشكل كامل
2. جرب البحث عن: `IIS_IUSRS` بدون مسافات
3. تأكد من اختيار "Locations" → اختر اسم السيرفر المحلي

### المشكلة 3: الصلاحيات موجودة لكن الموقع لا يعمل
**الحل:**
1. تأكد من إعادة تشغيل IIS بعد الإضافة
2. تأكد من إضافة الصلاحيات لمجلد `logs` أيضاً:
```powershell
icacls "C:\inetpub\wwwroot\ClinicManagment\logs" /grant "IIS_IUSRS:(OI)(CI)M" /T
```
3. راجع الأسباب الأخرى في ملف `DEPLOYMENT_GUIDE_AR.md`

---

## 🔗 ملفات ذات صلة

- **DEPLOYMENT_GUIDE_AR.md** - دليل الحلول الكامل
- **DiagnoseTroubleshoot.ps1** - سكريبت التشخيص التلقائي
- **web.config** - ملف التكوين لـ IIS

---

**حظاً موفقاً!** 🚀
