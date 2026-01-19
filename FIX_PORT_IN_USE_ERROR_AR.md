# 🔴 حل خطأ: The process cannot access the file because it is being used by another process

## الخطأ
```
The process cannot access the file because it is being used by another process.
(Exception from HRESULT: 0x80070020)
```

يظهر هذا الخطأ عند محاولة تشغيل الموقع في IIS Manager.

---

## 🔍 السبب

المشكلة: **Port 5000 مستخدم من قبل عملية أخرى!**

### الأسباب الشائعة:

1. ✋ **التطبيق شغال بالفعل من `dotnet run`**
   - فتحت Command Prompt أو PowerShell
   - شغلت الأمر: `dotnet run`
   - النافذة لا تزال مفتوحة والتطبيق يعمل

2. ✋ **التطبيق شغال من Visual Studio**
   - ضغطت F5 أو "Start" في Visual Studio
   - Visual Studio لا يزال مفتوحاً والتطبيق يعمل

3. ✋ **عملية dotnet.exe عالقة في الخلفية**
   - أغلقت Command Prompt فجأة
   - بقيت العملية تعمل في الخلفية

4. ✋ **تطبيق آخر يستخدم Port 5000**
   - تطبيق آخر مثبت على نفس المنفذ

---

## ✅ الحل السريع (3 طرق)

### الطريقة 1️⃣: إيقاف جميع عمليات dotnet (الأسرع!)

افتح **PowerShell كـ Administrator** واكتب:

```powershell
# إيقاف جميع عمليات dotnet
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force

# إيقاف جميع عمليات w3wp (IIS worker processes)
Get-Process -Name "w3wp" -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "✓ تم إيقاف جميع العمليات" -ForegroundColor Green
```

بعد ذلك، ارجع لـ IIS Manager وحاول Start مرة أخرى.

---

### الطريقة 2️⃣: إيقاف العملية المستخدمة للـ Port 5000

#### باستخدام PowerShell:

```powershell
# البحث عن العملية التي تستخدم Port 5000
$port = 5000
$process = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue |
           Select-Object -ExpandProperty OwningProcess -Unique

if ($process) {
    Write-Host "العملية التي تستخدم Port $port هي:" -ForegroundColor Yellow
    Get-Process -Id $process | Format-Table Id, ProcessName, Path -AutoSize

    Write-Host "`nهل تريد إيقاف هذه العملية؟ (y/n): " -ForegroundColor Yellow -NoNewline
    $confirm = Read-Host

    if ($confirm -eq "y" -or $confirm -eq "Y") {
        Stop-Process -Id $process -Force
        Write-Host "✓ تم إيقاف العملية" -ForegroundColor Green
    }
} else {
    Write-Host "✓ Port $port غير مستخدم" -ForegroundColor Green
}
```

#### باستخدام Command Prompt:

```cmd
REM 1. البحث عن العملية
netstat -ano | findstr :5000

REM 2. سيظهر لك رقم العملية (PID) في آخر عمود
REM مثال:
REM   TCP    0.0.0.0:5000    0.0.0.0:0    LISTENING    12345
REM                                                      ^^^^^ هذا هو الـ PID

REM 3. إيقاف العملية (استبدل 12345 برقم الـ PID الفعلي)
taskkill /PID 12345 /F
```

---

### الطريقة 3️⃣: إعادة تشغيل IIS (الأكثر أماناً)

افتح Command Prompt كـ Administrator:

```cmd
REM إيقاف IIS
iisreset /stop

REM الانتظار 5 ثوان
timeout /t 5

REM تشغيل IIS
iisreset /start
```

---

## 🎯 الحل التفصيلي خطوة بخطوة

### الخطوة 1️⃣: تحديد العملية المستخدمة للـ Port

افتح **Command Prompt** واكتب:

```cmd
netstat -ano | findstr :5000
```

**النتيجة المتوقعة:**
```
TCP    0.0.0.0:5000         0.0.0.0:0              LISTENING       12345
TCP    [::]:5000            [::]:0                 LISTENING       12345
```

- الرقم **12345** هو **Process ID (PID)**

### الخطوة 2️⃣: معرفة اسم العملية

```cmd
tasklist | findstr 12345
```

**النتيجة المتوقعة:**
```
dotnet.exe                   12345 Console                    1    150,000 K
```

أو

```
w3wp.exe                     12345 Console                    1     80,000 K
```

### الخطوة 3️⃣: إيقاف العملية

```cmd
taskkill /PID 12345 /F
```

**ملاحظة:** استبدل `12345` برقم الـ PID الحقيقي الذي ظهر لك.

### الخطوة 4️⃣: التحقق من إيقاف العملية

```cmd
netstat -ano | findstr :5000
```

إذا لم تظهر أي نتائج، معناها Port 5000 أصبح حراً!

### الخطوة 5️⃣: تشغيل الموقع من IIS

1. ارجع لـ **IIS Manager**
2. اختر **Sites** → **ClinicManagment**
3. اضغط **Start** من الجانب الأيمن
4. يجب أن يعمل الآن بنجاح! ✅

---

## 🛠️ حلول إضافية

### الحل البديل 1: تغيير Port في IIS

إذا أردت استخدام Port آخر بدلاً من 5000:

1. في **IIS Manager**
2. اختر الموقع **ClinicManagment**
3. من اليمين، اضغط **Bindings...**
4. اختر الـ binding واضغط **Edit**
5. غيّر Port من `5000` إلى `8080` (مثلاً)
6. اضغط **OK**
7. شغل الموقع
8. افتح Chrome واكتب: `http://localhost:8080`

### الحل البديل 2: تعطيل Kestrel والاعتماد على IIS فقط

إذا كنت تريد استخدام IIS فقط (بدون `dotnet run`):

**في ملف `web.config`:**
```xml
<aspNetCore processPath="dotnet"
            arguments=".\ClinicManagementSystem.dll"
            stdoutLogEnabled="true"
            stdoutLogFile=".\logs\stdout"
            hostingModel="inprocess">
  <!-- استخدام inprocess يجعل IIS يشغل التطبيق مباشرة -->
</aspNetCore>
```

تأكد أن `hostingModel="inprocess"` (موجود بالفعل في ملف web.config الذي أرسلته لك).

---

## 🚫 أخطاء شائعة

### ❌ الخطأ 1: "Access Denied" عند استخدام taskkill

**الحل:** شغّل Command Prompt كـ **Administrator**

### ❌ الخطأ 2: العملية تعود تلقائياً بعد إيقافها

**السبب:** Visual Studio لا يزال مفتوحاً ويعيد تشغيل التطبيق

**الحل:**
1. أغلق Visual Studio
2. أو اضغط Shift+F5 في Visual Studio لإيقاف Debugging

### ❌ الخطأ 3: Port لا يزال مستخدماً رغم إيقاف العمليات

**الحل:**
```powershell
# إعادة تشغيل IIS بالكامل
iisreset

# إعادة تشغيل Windows Server (آخر حل)
Restart-Computer
```

---

## 📝 ملاحظات مهمة

### ⚠️ الفرق بين `dotnet run` و IIS:

| الطريقة | الاستخدام | متى تستخدمها |
|---------|-----------|--------------|
| `dotnet run` | **للتطوير فقط** | عند كتابة الكود والتجربة المحلية |
| **IIS** | **للإنتاج** | عند النشر على السيرفر الحقيقي |

⚠️ **لا تستخدم الاثنين معاً!**
- إذا أردت التطوير → استخدم `dotnet run`
- إذا أردت النشر على IIS → **أغلق** `dotnet run` واستخدم IIS فقط

### ✅ الطريقة الصحيحة للنشر على IIS:

1. **لا تشغل** التطبيق بـ `dotnet run`
2. انشر الملفات إلى `C:\inetpub\wwwroot\ClinicManagment`
3. شغّل الموقع من **IIS Manager** فقط
4. افتح المتصفح: `http://localhost:5000`
5. IIS سيشغل التطبيق تلقائياً عند أول طلب

---

## 🔍 التشخيص السريع

استخدم هذه الأوامر للتشخيص:

```powershell
# 1. فحص Port 5000
Write-Host "1. فحص Port 5000..." -ForegroundColor Cyan
$port5000 = Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue
if ($port5000) {
    $pid = $port5000[0].OwningProcess
    Write-Host "   ✗ Port 5000 مستخدم من قبل:" -ForegroundColor Red
    Get-Process -Id $pid | Format-Table Id, ProcessName, Path
} else {
    Write-Host "   ✓ Port 5000 حر" -ForegroundColor Green
}

# 2. فحص عمليات dotnet
Write-Host "`n2. فحص عمليات dotnet..." -ForegroundColor Cyan
$dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
if ($dotnetProcesses) {
    Write-Host "   ✗ عمليات dotnet قيد التشغيل:" -ForegroundColor Red
    $dotnetProcesses | Format-Table Id, ProcessName, StartTime
} else {
    Write-Host "   ✓ لا توجد عمليات dotnet" -ForegroundColor Green
}

# 3. فحص IIS Application Pools
Write-Host "`n3. فحص IIS Application Pools..." -ForegroundColor Cyan
Import-Module WebAdministration -ErrorAction SilentlyContinue
try {
    $pools = Get-ChildItem IIS:\AppPools | Where-Object { $_.State -eq "Started" }
    if ($pools) {
        Write-Host "   Application Pools قيد التشغيل:" -ForegroundColor Yellow
        $pools | Format-Table Name, State
    }
} catch {
    Write-Host "   ⚠ لا يمكن الوصول لـ IIS" -ForegroundColor Yellow
}
```

---

## 🎯 الحل الموصى به (Best Practice)

**للنشر على Windows Server + IIS:**

1. **أوقف أي عمليات dotnet:**
   ```powershell
   Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force
   ```

2. **أعد تشغيل IIS:**
   ```cmd
   iisreset
   ```

3. **شغّل الموقع من IIS Manager**

4. **افتح المتصفح:**
   ```
   http://localhost:5000
   ```

5. **لا تشغل `dotnet run` بعد ذلك!**

---

## ✅ خطوات ما بعد الحل

بعد حل المشكلة:

1. ✅ الموقع يعمل على `http://localhost:5000`
2. ✅ راجع ملف `DEPLOYMENT_GUIDE_AR.md` للمشاكل الأخرى
3. ✅ إذا ظهر HTTP 500، اقرأ السجلات في `C:\inetpub\wwwroot\ClinicManagment\logs\stdout*.log`

---

**حظاً موفقاً!** 🚀
