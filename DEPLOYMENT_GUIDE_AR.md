# دليل حل مشكلة HTTP ERROR 500 على Windows Server + IIS

## المشكلة
عند تسجيل الدخول للموقع على `localhost:5000` تظهر رسالة:
```
تعذّر على هذه الصفحة العمل
يتعذر على localhost معالجة هذا الطلب حاليًا.
HTTP ERROR 500
```

---

## الحلول المحتملة (بالترتيب)

### ✅ الحل 1: تفعيل عرض الأخطاء التفصيلية (الأهم!)

قبل أي شيء، يجب معرفة الخطأ الحقيقي. افتح ملف `appsettings.json` وتأكد من إضافة هذا:

**في مجلد المشروع `C:\inetpub\wwwroot\ClinicManagment`**، افتح ملف `appsettings.json` وأضف:

```json
{
  "DetailedErrors": true,
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Debug",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

**ثم قم بإعادة تشغيل التطبيق** وحاول تسجيل الدخول مرة أخرى. سيظهر لك تفاصيل الخطأ بدلاً من رسالة 500 العامة.

---

### ✅ الحل 2: التأكد من تثبيت .NET 8.0 Runtime

المشروع يعمل بـ **.NET 8.0** ويجب تثبيت Runtime على Windows Server:

1. **حمّل وثبّت .NET 8.0 Hosting Bundle** من الرابط:
   ```
   https://dotnet.microsoft.com/download/dotnet/8.0
   ```

2. اختر: **"ASP.NET Core Runtime 8.0.x - Windows Hosting Bundle"**

3. بعد التثبيت، **أعد تشغيل Windows Server** (ضروري!)

4. تأكد من التثبيت بفتح **Command Prompt** وكتابة:
   ```cmd
   dotnet --list-runtimes
   ```
   يجب أن يظهر:
   ```
   Microsoft.AspNetCore.App 8.0.x
   Microsoft.NETCore.App 8.0.x
   ```

---

### ✅ الحل 3: التأكد من الاتصال بقاعدة البيانات

المشروع يتصل بقاعدة بيانات AWS RDS:
```
Server: doctordb.c3eoy02guueq.me-south-1.rds.amazonaws.com
Database: DoctorInfo
```

#### ✔️ اختبار الاتصال:

1. **افتح SQL Server Management Studio (SSMS)** أو **Azure Data Studio** على Windows Server

2. جرب الاتصال بهذه البيانات:
   - **Server name:** `doctordb.c3eoy02guueq.me-south-1.rds.amazonaws.com`
   - **Authentication:** SQL Server Authentication
   - **Username:** `admin`
   - **Password:** `Mmrsz12345`

#### ❌ إذا فشل الاتصال:

**السبب الأول: Security Group في AWS RDS**
- افتح AWS Console → RDS → اختر قاعدة البيانات `doctordb`
- اذهب إلى **Security Groups**
- تأكد من إضافة **Inbound Rule**:
  - **Type:** MSSQL (Port 1433)
  - **Source:** إما IP الخاص بـ Windows Server أو `0.0.0.0/0` (للسماح للجميع - للتجربة فقط)

**السبب الثاني: Network ACLs و VPC**
- تأكد أن Windows Server يمكنه الوصول لنفس VPC أو أن RDS متاح publicly

**اختبار الاتصال من Command Prompt:**
```cmd
telnet doctordb.c3eoy02guueq.me-south-1.rds.amazonaws.com 1433
```
إذا ظهر شاشة سوداء فارغة، فالاتصال يعمل. إذا ظهر خطأ، فالمشكلة في Network.

---

### ✅ الحل 4: إنشاء قاعدة البيانات والجداول

إذا قاعدة البيانات موجودة لكن الجداول مفقودة:

1. **في Windows Server، افتح Command Prompt**

2. اذهب لمجلد المشروع:
   ```cmd
   cd C:\inetpub\wwwroot\ClinicManagment
   ```

3. قم بتطبيق Migration لإنشاء الجداول:
   ```cmd
   dotnet ef database update
   ```

إذا ظهرت رسالة نجاح، معناها الجداول تم إنشاؤها بنجاح.

---

### ✅ الحل 5: التأكد من إعدادات IIS

#### 1. Application Pool Settings:

- افتح **IIS Manager**
- اذهب إلى **Application Pools**
- اختر الـ Pool الخاص بالموقع
- اضغط **Basic Settings**:
  - **.NET CLR Version:** اختر **No Managed Code** (مهم لـ .NET Core!)
  - **Managed Pipeline Mode:** Integrated

#### 2. Site Bindings:

- اختر الموقع من **Sites**
- اضغط **Bindings**
- تأكد من وجود:
  - **http** على Port **5000**
  - يمكنك إضافة **https** على Port **5001** إذا أردت

#### 3. الأذونات (Permissions):

- اذهب لمجلد `C:\inetpub\wwwroot\ClinicManagment`
- كليك يمين → **Properties** → **Security**
- تأكد من أن **IIS_IUSRS** و **IUSR** لهم صلاحيات **Read & Execute**

---

### ✅ الحل 6: إنشاء ملف web.config الصحيح

تأكد من وجود ملف `web.config` في مجلد المشروع بهذا المحتوى:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet"
                  arguments=".\ClinicManagementSystem.dll"
                  stdoutLogEnabled="true"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Development" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
```

**ملاحظات مهمة:**
- `stdoutLogEnabled="true"` لتفعيل سجل الأخطاء
- السجل سيُحفظ في `C:\inetpub\wwwroot\ClinicManagment\logs\stdout_xxx.log`
- **أنشئ مجلد `logs`** داخل مجلد المشروع

---

### ✅ الحل 7: فحص ملفات السجل (Logs)

بعد إضافة web.config السابق:

1. **أنشئ مجلد logs:**
   ```cmd
   cd C:\inetpub\wwwroot\ClinicManagment
   mkdir logs
   ```

2. **أعد تشغيل الموقع** في IIS (Stop → Start)

3. **حاول تسجيل الدخول** مرة أخرى

4. **افتح مجلد logs** واقرأ الملفات:
   ```cmd
   cd C:\inetpub\wwwroot\ClinicManagment\logs
   dir
   type stdout_xxx.log
   ```

السجل سيوضح لك **الخطأ الحقيقي**.

---

### ✅ الحل 8: التأكد من وجود ملف DLL

تأكد من وجود الملفات التالية في `C:\inetpub\wwwroot\ClinicManagment`:

```
ClinicManagementSystem.dll
ClinicManagementSystem.exe
appsettings.json
web.config
```

إذا كانت مفقودة، يجب عليك:

1. **نشر المشروع (Publish) من Visual Studio**:
   - في Visual Studio، كليك يمين على المشروع
   - اختر **Publish**
   - اختر **Folder**
   - الـ Target Location يكون: `C:\inetpub\wwwroot\ClinicManagment`
   - Configuration: **Release**
   - Target Framework: **net8.0**
   - Deployment Mode: **Framework-dependent**
   - اضغط **Publish**

أو **من Command Line**:
```cmd
cd [مسار المشروع الأصلي]
dotnet publish -c Release -o C:\inetpub\wwwroot\ClinicManagment
```

---

## 🔍 الخطوات التشخيصية السريعة

### 1. افتح Event Viewer على Windows:
   - ابحث عن **Event Viewer**
   - اذهب إلى: **Windows Logs** → **Application**
   - ابحث عن أخطاء من المصدر: **IIS AspNetCore Module**

### 2. فعّل تسجيل الأخطاء:
   - في `appsettings.json` اجعل `DetailedErrors: true`
   - في `web.config` اجعل `stdoutLogEnabled="true"`

### 3. اختبر الاتصال بقاعدة البيانات:
   ```cmd
   telnet doctordb.c3eoy02guueq.me-south-1.rds.amazonaws.com 1433
   ```

---

## 🎯 الحل الأسرع والمباشر

**قم بهذا فوراً:**

1. **أنشئ ملف `web.config`** (انظر الحل 6)
2. **أنشئ مجلد `logs`**
3. **غيّر في `appsettings.json`:**
   ```json
   "DetailedErrors": true
   ```
4. **أعد تشغيل IIS:**
   ```cmd
   iisreset
   ```
5. **جرب تسجيل الدخول**
6. **اقرأ ملف السجل:**
   ```cmd
   type C:\inetpub\wwwroot\ClinicManagment\logs\stdout*.log
   ```

**الملف سيوضح لك المشكلة الحقيقية!**

---

## 📞 معلومات إضافية

### بيانات الاتصال بقاعدة البيانات الحالية:
```
Server: doctordb.c3eoy02guueq.me-south-1.rds.amazonaws.com
Database: DoctorInfo
Username: admin
Password: Mmrsz12345
```

### أنواع المستخدمين في النظام:
- **Admin** - مسؤول النظام (جدول UserInfos)
- **Doctor** - طبيب (جدول DoctorInfos) - يحتاج اشتراك فعّال
- **Assistant** - مساعد (جدول DoctorAssists)

---

## ⚠️ تنبيهات أمنية

1. **لا تستخدم `0.0.0.0/0` في Security Group** في الإنتاج - استخدم IP محدد
2. **غيّر كلمة المرور الافتراضية** لقاعدة البيانات
3. **لا تترك `DetailedErrors: true`** في الإنتاج
4. **استخدم HTTPS** بدلاً من HTTP في الإنتاج

---

## 🆘 إذا استمرت المشكلة

**أرسل لي:**
1. محتوى ملف السجل: `C:\inetpub\wwwroot\ClinicManagment\logs\stdout*.log`
2. لقطة شاشة من Event Viewer
3. نتيجة الأمر: `dotnet --list-runtimes`
4. هل يمكن الاتصال بقاعدة البيانات من SSMS؟

---

**حظاً موفقاً!** 🚀
