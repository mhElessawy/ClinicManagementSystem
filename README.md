# نظام إدارة العيادة الطبية - دليل المستخدم والمطور

## نظرة عامة
نظام شامل لإدارة العيادات الطبية مبني باستخدام ASP.NET Core 8 MVC مع SQL Server وتقارير Excel.

## المميزات الرئيسية
1. **إدارة الأطباء**: سجلات كاملة للأطباء مع الصور والتخصصات
2. **إدارة المرضى**: متابعة شاملة لبيانات المرضى
3. **التشخيصات الطبية**: تسجيل التشخيصات مع إمكانية إرفاق ملفات
4. **نظام التقارير**: تقارير Excel قابلة للتصدير
5. **إدارة المستخدمين**: نظام مصادقة وصلاحيات
6. **الأقسام والتخصصات**: تصنيف منظم للأطباء
7. **مساعدي الأطباء**: إدارة فريق العمل

## متطلبات النظام
- Visual Studio 2022 أو أحدث
- .NET 8.0 SDK
- SQL Server 2019 أو أحدث
- مساحة تخزين 100 MB على الأقل

## خطوات التثبيت

### 1. تحضير قاعدة البيانات
```bash
# في Package Manager Console في Visual Studio:
Add-Migration InitialCreate
Update-Database
```

هذا سيقوم بإنشاء:
- قاعدة بيانات باسم `ClinicManagementDB`
- جميع الجداول مع العلاقات
- بيانات أولية (أقسام، تخصصات، مستخدم admin)

### 2. تشغيل المشروع
```bash
dotnet run
```
أو اضغط F5 في Visual Studio

### 3. تسجيل الدخول
- اسم المستخدم: `admin`
- كلمة المرور: `Admin@123`

## بنية المشروع

### Models (نماذج البيانات)
```
Models/
├── DoctorInfo.cs          # معلومات الأطباء
├── Patient.cs             # معلومات المرضى
├── PatientDiagnosis.cs    # التشخيصات الطبية
├── UserInfo.cs            # مستخدمي النظام
├── Specialist.cs          # التخصصات الطبية
├── Department.cs          # الأقسام
└── DoctorAssist.cs        # مساعدي الأطباء
```

### Controllers (وحدات التحكم)
```
Controllers/
├── AccountController.cs           # المصادقة وتسجيل الدخول
├── HomeController.cs             # الصفحة الرئيسية
├── DepartmentsController.cs      # إدارة الأقسام
├── SpecialistsController.cs      # إدارة التخصصات
├── DoctorInfosController.cs      # إدارة الأطباء
├── PatientsController.cs         # إدارة المرضى
├── PatientDiagnosesController.cs # إدارة التشخيصات
├── DoctorAssistsController.cs    # إدارة المساعدين
├── UserInfosController.cs        # إدارة المستخدمين
└── ReportsController.cs          # التقارير
```

### Data (طبقة البيانات)
```
Data/
└── ApplicationDbContext.cs    # سياق قاعدة البيانات
```

## العلاقات بين الجداول

### 1. Department → Specialist (One-to-Many)
- كل قسم يحتوي على عدة تخصصات

### 2. Specialist → DoctorInfo (One-to-Many)
- كل تخصص يحتوي على عدة أطباء

### 3. UserInfo → DoctorInfo (One-to-Many)
- كل مستخدم يمكن أن يكون مرتبط بعدة أطباء

### 4. DoctorInfo → DoctorAssist (One-to-Many)
- كل طبيب يمكن أن يكون له عدة مساعدين

### 5. DoctorInfo → Patient (One-to-Many)
- كل طبيب يمكن أن يعالج عدة مرضى

### 6. Patient → PatientDiagnosis (One-to-Many)
- كل مريض يمكن أن يكون له عدة تشخيصات

### 7. DoctorInfo → PatientDiagnosis (One-to-Many)
- كل طبيب يمكن أن يقوم بعدة تشخيصات

## التقارير المتاحة

### 1. تقرير الأطباء (DoctorsReport)
- يمكن التصفية حسب: التخصص، الحالة (نشط/غير نشط)
- يحتوي على: الاسم، التخصص، الرقم المدني، الهواتف، العنوان

### 2. تقرير المرضى (PatientsReport)
- يمكن التصفية حسب: الطبيب المعالج
- يحتوي على: الاسم، الرقم المدني، الهواتف، العنوان، الطبيب

### 3. تقرير التشخيصات (DiagnosesReport)
- يمكن التصفية حسب: التاريخ (من - إلى)، الطبيب، المريض
- يحتوي على: التاريخ، المريض، الطبيب، التفاصيل

### 4. تقرير الإحصائيات (StatisticsReport)
- إحصائيات عامة للنظام
- توزيع الأطباء حسب التخصصات
- قابل للتصدير إلى Excel

## إنشاء Views للـ CRUD

نظراً لأن المشروع يحتوي على 7 جداول وكل جدول يحتاج إلى 5 صفحات (Index, Create, Edit, Delete, Details)، إليك قالب يمكن استخدامه:

### مثال: Departments Index View

```cshtml
@model IEnumerable<ClinicManagementSystem.Models.Department>

@{
    ViewData["Title"] = "الأقسام";
}

<div class="row mb-3">
    <div class="col">
        <h2><i class="fas fa-building"></i> @ViewData["Title"]</h2>
    </div>
    <div class="col text-end">
        <a asp-action="Create" class="btn btn-success">
            <i class="fas fa-plus"></i> إضافة قسم جديد
        </a>
    </div>
</div>

<div class="card">
    <div class="card-body">
        <div class="table-responsive">
            <table class="table table-striped table-hover">
                <thead class="table-primary">
                    <tr>
                        <th>م</th>
                        <th>@Html.DisplayNameFor(model => model.DepartmentName)</th>
                        <th>عدد التخصصات</th>
                        <th>الإجراءات</th>
                    </tr>
                </thead>
                <tbody>
                    @{ int counter = 1; }
                    @foreach (var item in Model)
                    {
                        <tr>
                            <td>@counter</td>
                            <td>@Html.DisplayFor(modelItem => item.DepartmentName)</td>
                            <td>@item.Specialists.Count</td>
                            <td>
                                <div class="btn-group-actions">
                                    <a asp-action="Details" asp-route-id="@item.Id" class="btn btn-sm btn-info">
                                        <i class="fas fa-eye"></i> عرض
                                    </a>
                                    <a asp-action="Edit" asp-route-id="@item.Id" class="btn btn-sm btn-warning">
                                        <i class="fas fa-edit"></i> تعديل
                                    </a>
                                    <a asp-action="Delete" asp-route-id="@item.Id" class="btn btn-sm btn-danger">
                                        <i class="fas fa-trash"></i> حذف
                                    </a>
                                </div>
                            </td>
                        </tr>
                        counter++;
                    }
                </tbody>
            </table>
        </div>
    </div>
</div>
```

### مثال: Create View

```cshtml
@model ClinicManagementSystem.Models.Department

@{
    ViewData["Title"] = "إضافة قسم جديد";
}

<h2><i class="fas fa-plus"></i> @ViewData["Title"]</h2>

<div class="card">
    <div class="card-body">
        <form asp-action="Create" method="post">
            @Html.AntiForgeryToken()
            <div asp-validation-summary="ModelOnly" class="text-danger"></div>
            
            <div class="mb-3">
                <label asp-for="DepartmentName" class="form-label"></label>
                <input asp-for="DepartmentName" class="form-control" />
                <span asp-validation-for="DepartmentName" class="text-danger"></span>
            </div>

            <div class="mt-4">
                <button type="submit" class="btn btn-success">
                    <i class="fas fa-save"></i> حفظ
                </button>
                <a asp-action="Index" class="btn btn-secondary">
                    <i class="fas fa-arrow-left"></i> رجوع
                </a>
            </div>
        </form>
    </div>
</div>

@section Scripts {
    @{await Html.RenderPartialAsync("_ValidationScriptsPartial");}
}
```

## إنشاء Views المتبقية

لإنشاء Views لكل controller، اتبع هذه الخطوات:

1. **في Visual Studio**:
   - انقر بزر الماوس الأيمن على Controller
   - اختر "Add" → "View"
   - اختر نوع الـ View (Index, Create, Edit, etc.)
   - حدد الـ Model المناسب
   - اختر Template المناسب

2. **أو استخدم Scaffolding**:
```bash
# في Package Manager Console:
# مثال لإنشاء Views للـ Departments
Scaffold-DbContext "Server=.;Database=ClinicManagementDB;..." -Provider Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models
```

## الملفات المهمة للتعديل

### 1. تخصيص سلسلة الاتصال (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=ClinicManagementDB;..."
  }
}
```

### 2. تخصيص القائمة (_Layout.cshtml)
- أضف أو احذف عناصر القائمة حسب الحاجة
- غير الألوان والتنسيق

### 3. تخصيص صفحة تسجيل الدخول (Login.cshtml)
- غير الشعار
- أضف حقول إضافية
- غير التصميم

## استكشاف الأخطاء

### خطأ في الاتصال بقاعدة البيانات
```
تأكد من:
1. SQL Server يعمل
2. سلسلة الاتصال صحيحة
3. قاعدة البيانات موجودة
```

### خطأ في Migration
```bash
# احذف مجلد Migrations وأعد إنشائه:
Remove-Migration
Add-Migration InitialCreate
Update-Database
```

### خطأ في الصلاحيات
```
تأكد من تسجيل الدخول قبل الوصول للصفحات المحمية
```

## التطوير المستقبلي

### إضافات مقترحة:
1. **تقارير متقدمة**: إضافة Charts و Graphs
2. **المواعيد**: نظام حجز مواعيد
3. **الفواتير**: إدارة الفواتير والمدفوعات
4. **الإشعارات**: نظام تنبيهات
5. **الطباعة**: تصدير التقارير إلى PDF
6. **البحث المتقدم**: إضافة خاصية البحث في كل صفحة
7. **Audit Log**: تسجيل العمليات
8. **API**: إضافة Web API

## الأمان

### نصائح أمنية مهمة:
1. **لا تخزن كلمات المرور بشكل نصي**: استخدم Password Hashing
2. **استخدم HTTPS**: في الإنتاج
3. **قم بتفعيل Authorization**: حسب الصلاحيات
4. **قم بتحديث الـ packages**: بانتظام
5. **استخدم parameterized queries**: لمنع SQL Injection
6. **قم بتفعيل CORS**: بشكل صحيح

## إنشاء Views بسرعة

يمكنك استخدام الأمر التالي لإنشاء جميع Views تلقائياً:

```bash
# في Package Manager Console:
Scaffold-DbContext "Name=ConnectionStrings:DefaultConnection" Microsoft.EntityFrameworkCore.SqlServer -Context ApplicationDbContext -OutputDir Models -Force

# لإنشاء Controller مع Views:
Add-Controller -name DepartmentsController -m Department -dc ApplicationDbContext -outDir Controllers -udl
```

## الدعم الفني

للحصول على المساعدة:
1. راجع هذا الملف
2. راجع تعليقات الكود
3. راجع وثائق ASP.NET Core

## الترخيص
هذا المشروع مفتوح المصدر ومتاح للاستخدام التعليمي والتجاري.

---

## ملاحظات مهمة

### التعامل مع الصور
```csharp
// حفظ صورة:
if (file != null && file.Length > 0)
{
    using (var ms = new MemoryStream())
    {
        await file.CopyToAsync(ms);
        model.Picture = ms.ToArray();
    }
}

// عرض صورة:
<img src="@Url.Action("GetImage", new { id = Model.Id })" />
```

### التعامل مع التواريخ
```csharp
// في العرض:
@Model.Date.ToString("yyyy-MM-dd")

// في الحفظ:
model.Date = DateTime.Now;
```

### التعامل مع القوائم المنسدلة
```csharp
// في Controller:
ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "DepartmentName");

// في View:
<select asp-for="DepartmentId" asp-items="ViewBag.DepartmentId" class="form-control">
    <option value="">-- اختر القسم --</option>
</select>
```

## SQL Scripts مفيدة

### إنشاء قاعدة البيانات يدوياً
```sql
CREATE DATABASE ClinicManagementDB;
GO

USE ClinicManagementDB;
GO
```

### استعلامات إحصائية
```sql
-- عدد الأطباء
SELECT COUNT(*) FROM DoctorInfos;

-- عدد المرضى
SELECT COUNT(*) FROM Patients;

-- التشخيصات اليوم
SELECT COUNT(*) FROM PatientDiagnoses 
WHERE CAST(DiagnosisDate AS DATE) = CAST(GETDATE() AS DATE);
```

---

## الخلاصة
هذا النظام يوفر حلاً متكاملاً لإدارة العيادات الطبية مع إمكانية التوسع والتخصيص حسب الاحتياجات.

للمزيد من المعلومات، راجع الكود المصدري والتعليقات المضمنة فيه.
