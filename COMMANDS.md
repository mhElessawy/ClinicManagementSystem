# أوامر مفيدة - نظام إدارة العيادة

## Package Manager Console Commands

### إدارة قاعدة البيانات (Migrations)

```powershell
# إنشاء Migration جديد
Add-Migration MigrationName

# تطبيق Migration على قاعدة البيانات
Update-Database

# التراجع إلى Migration محدد
Update-Database -Migration MigrationName

# عرض جميع Migrations
Get-Migration

# حذف آخر Migration (إذا لم يتم تطبيقه بعد)
Remove-Migration

# إنشاء SQL Script من Migration
Script-Migration

# تحديث قاعدة البيانات لأحدث Migration
Update-Database -Verbose

# إنشاء قاعدة البيانات من الصفر
Drop-Database
Add-Migration InitialCreate
Update-Database
```

### استعادة وتنظيف الحزم

```powershell
# استعادة جميع الحزم
dotnet restore

# تنظيف المشروع
dotnet clean

# بناء المشروع
dotnet build

# تشغيل المشروع
dotnet run

# نشر المشروع
dotnet publish -c Release
```

### إنشاء Controllers و Views

```powershell
# إنشاء Controller مع Views
Add-Controller -name ControllerName -m ModelName -dc ApplicationDbContext -outDir Controllers -udl

# إنشاء API Controller
Add-Controller -name ApiController -api -m ModelName -dc ApplicationDbContext -outDir Controllers

# مثال: إنشاء Controller للأطباء
Add-Controller -name DoctorInfosController -m DoctorInfo -dc ApplicationDbContext -outDir Controllers -udl
```

## SQL Server Commands

### الاتصال بقاعدة البيانات

```sql
-- الاتصال بالسيرفر المحلي
sqlcmd -S localhost -E

-- الاتصال بسيرفر محدد
sqlcmd -S ServerName -U username -P password
```

### عمليات قاعدة البيانات

```sql
-- عرض جميع قواعد البيانات
SELECT name FROM sys.databases;

-- استخدام قاعدة بيانات
USE ClinicManagementDB;

-- عرض جميع الجداول
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';

-- عرض بنية جدول
sp_help 'DoctorInfos';

-- حذف قاعدة البيانات
DROP DATABASE ClinicManagementDB;

-- نسخ احتياطي لقاعدة البيانات
BACKUP DATABASE ClinicManagementDB TO DISK = 'C:\Backup\ClinicDB.bak';

-- استعادة قاعدة البيانات
RESTORE DATABASE ClinicManagementDB FROM DISK = 'C:\Backup\ClinicDB.bak';
```

### استعلامات مفيدة

```sql
-- عدد السجلات في كل جدول
SELECT 
    t.NAME AS TableName,
    p.rows AS RowCounts
FROM sys.tables t
INNER JOIN sys.partitions p ON t.object_id = p.OBJECT_ID
WHERE t.is_ms_shipped = 0 AND p.index_id < 2
ORDER BY t.NAME;

-- حجم قاعدة البيانات
EXEC sp_spaceused;

-- البحث في جميع الجداول عن قيمة
DECLARE @SearchStr nvarchar(100) = 'SearchValue'
-- Add your search logic here

-- إعادة بناء الـ Indexes
ALTER INDEX ALL ON DoctorInfos REBUILD;

-- تحديث الإحصائيات
UPDATE STATISTICS DoctorInfos;
```

## Git Commands (إذا كنت تستخدم Git)

```bash
# تهيئة Git
git init

# إضافة جميع الملفات
git add .

# عمل Commit
git commit -m "Initial commit"

# إضافة Remote Repository
git remote add origin https://github.com/username/repo.git

# رفع الملفات
git push -u origin main

# سحب التحديثات
git pull origin main

# عرض الحالة
git status

# عرض السجل
git log
```

## dotnet CLI Commands

```bash
# إنشاء مشروع MVC جديد
dotnet new mvc -n ProjectName

# إنشاء مشروع Web API
dotnet new webapi -n ApiName

# إنشاء مشروع Class Library
dotnet new classlib -n LibraryName

# إضافة Package
dotnet add package PackageName

# إزالة Package
dotnet remove package PackageName

# عرض جميع Packages
dotnet list package

# تحديث Package
dotnet add package PackageName --version VersionNumber

# إنشاء Solution
dotnet new sln -n SolutionName

# إضافة مشروع إلى Solution
dotnet sln add ProjectPath
```

## Visual Studio Shortcuts

```
# عام
Ctrl + S         : حفظ
Ctrl + Shift + S : حفظ الكل
Ctrl + F5        : تشغيل بدون Debug
F5               : تشغيل مع Debug
Shift + F5       : إيقاف Debug

# التنقل
Ctrl + ,         : البحث عن ملف
Ctrl + T         : البحث في المشروع
F12              : الذهاب إلى Definition
Ctrl + -         : العودة للخلف
Ctrl + Shift + - : الذهاب للأمام

# التحرير
Ctrl + K, Ctrl + D : تنسيق الملف
Ctrl + K, Ctrl + C : تعليق الكود
Ctrl + K, Ctrl + U : إزالة التعليق
Ctrl + .         : Quick Actions
Alt + Enter      : Quick Actions

# البناء
Ctrl + Shift + B : بناء المشروع
Ctrl + Break     : إيقاف البناء
```

## Package Installation Commands

```powershell
# Entity Framework Core
Install-Package Microsoft.EntityFrameworkCore
Install-Package Microsoft.EntityFrameworkCore.SqlServer
Install-Package Microsoft.EntityFrameworkCore.Tools

# Code Generation
Install-Package Microsoft.VisualStudio.Web.CodeGeneration.Design

# Excel Export
Install-Package ClosedXML
Install-Package EPPlus

# Authentication
Install-Package Microsoft.AspNetCore.Authentication.JwtBearer

# AutoMapper
Install-Package AutoMapper.Extensions.Microsoft.DependencyInjection
```

## Troubleshooting Commands

```powershell
# إعادة بناء المشروع بالكامل
dotnet clean
dotnet restore
dotnet build

# حذف bin و obj
Remove-Item -Recurse -Force bin, obj

# إعادة إنشاء قاعدة البيانات
Drop-Database
Remove-Migration
Add-Migration InitialCreate
Update-Database

# فحص الأخطاء
dotnet build --no-incremental

# عرض معلومات مفصلة
dotnet build --verbosity detailed
```

## Testing Commands

```bash
# إنشاء مشروع Test
dotnet new xunit -n ProjectName.Tests

# تشغيل Tests
dotnet test

# تشغيل Tests مع Coverage
dotnet test /p:CollectCoverage=true

# تشغيل Test محدد
dotnet test --filter "FullyQualifiedName~TestName"
```

## Deployment Commands

```bash
# النشر للإنتاج
dotnet publish -c Release -o ./publish

# النشر إلى IIS
dotnet publish -c Release -o "C:\inetpub\wwwroot\ClinicApp"

# إنشاء Package للنشر
dotnet publish -c Release --self-contained true -r win-x64

# فحص التطبيق
dotnet run --environment Production
```

## Database Seeding

```csharp
// في Program.cs أو Startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
    
    // إضافة بيانات أولية
    if (!context.Departments.Any())
    {
        context.Departments.AddRange(
            new Department { DepartmentName = "الباطنية" },
            new Department { DepartmentName = "الجراحة" }
        );
        context.SaveChanges();
    }
}
```

## appsettings.json Configurations

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ClinicDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## Useful NuGet Packages

```
# Core Packages
- Microsoft.EntityFrameworkCore (8.0.0)
- Microsoft.EntityFrameworkCore.SqlServer (8.0.0)
- Microsoft.EntityFrameworkCore.Tools (8.0.0)
- Microsoft.VisualStudio.Web.CodeGeneration.Design (8.0.0)

# Reporting
- ClosedXML (0.102.1)
- iTextSharp (PDF Generation)

# Security
- Microsoft.AspNetCore.Authentication.JwtBearer
- BCrypt.Net-Next (Password Hashing)

# Utilities
- AutoMapper.Extensions.Microsoft.DependencyInjection
- Newtonsoft.Json
- Serilog (Logging)
```

---

## ملاحظات مهمة

1. **قبل تشغيل أي أمر Migration**، تأكد من:
   - سلسلة الاتصال صحيحة
   - SQL Server يعمل
   - لديك صلاحيات كافية

2. **عند حدوث خطأ في Migration**:
   - احذف مجلد Migrations
   - احذف قاعدة البيانات
   - أعد إنشاء Migration من جديد

3. **للحصول على مساعدة لأي أمر**:
   ```powershell
   Get-Help CommandName
   dotnet CommandName --help
   ```

4. **النسخ الاحتياطي**:
   - قم بعمل نسخة احتياطية قبل أي تغيير كبير
   - احفظ ملف appsettings.json في مكان آمن

---

هذا الملف يحتوي على معظم الأوامر التي ستحتاجها في التطوير اليومي.
