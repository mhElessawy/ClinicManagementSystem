# دليل البدء السريع - نظام إدارة العيادة الطبية

## التثبيت والتشغيل السريع

### 1. افتح Visual Studio 2022

### 2. افتح Package Manager Console من:
```
Tools → NuGet Package Manager → Package Manager Console
```

### 3. قم بتشغيل الأوامر التالية:

```powershell
# استعادة الحزم
dotnet restore

# إنشاء قاعدة البيانات
Add-Migration InitialCreate
Update-Database

# تشغيل المشروع
dotnet run
```

أو اضغط F5 في Visual Studio

### 4. تسجيل الدخول
- عنوان URL: https://localhost:5001
- اسم المستخدم: `admin`
- كلمة المرور: `Admin@123`

---

## البنية السريعة

### Controllers المتوفرة:
1. **AccountController** - تسجيل الدخول والخروج
2. **HomeController** - الصفحة الرئيسية
3. **DepartmentsController** - إدارة الأقسام
4. **SpecialistsController** - إدارة التخصصات
5. **DoctorInfosController** - إدارة الأطباء
6. **PatientsController** - إدارة المرضى
7. **PatientDiagnosesController** - إدارة التشخيصات
8. **DoctorAssistsController** - إدارة المساعدين
9. **UserInfosController** - إدارة المستخدمين
10. **ReportsController** - التقارير

### Models الرئيسية:
- `DoctorInfo` - معلومات الأطباء
- `Patient` - معلومات المرضى
- `PatientDiagnosis` - التشخيصات
- `UserInfo` - المستخدمين
- `Department` - الأقسام
- `Specialist` - التخصصات
- `DoctorAssist` - المساعدين

---

## إنشاء Views بسرعة

### طريقة 1: استخدام Visual Studio Scaffolding

1. انقر بزر الماوس الأيمن على مجلد `Views`
2. اختر `Add` → `New Scaffolded Item`
3. اختر `MVC View`
4. اختر Template (Index, Create, Edit, Delete, Details)
5. حدد Model Class
6. حدد Data Context: `ApplicationDbContext`
7. اضغط `Add`

### طريقة 2: استخدام Package Manager Console

```powershell
# إنشاء Views للأطباء
Scaffold-Controller -name DoctorInfosController -m DoctorInfo -dc ApplicationDbContext -udl -outDir Controllers

# إنشاء Views للمرضى
Scaffold-Controller -name PatientsController -m Patient -dc ApplicationDbContext -udl -outDir Controllers

# إنشاء Views للتشخيصات
Scaffold-Controller -name PatientDiagnosesController -m PatientDiagnosis -dc ApplicationDbContext -udl -outDir Controllers
```

---

## قالب سريع لـ Index View

```cshtml
@model IEnumerable<YourModel>

@{
    ViewData["Title"] = "العنوان";
}

<div class="row mb-3">
    <div class="col">
        <h2><i class="fas fa-icon"></i> @ViewData["Title"]</h2>
    </div>
    <div class="col text-end">
        <a asp-action="Create" class="btn btn-success">
            <i class="fas fa-plus"></i> إضافة جديد
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
                        <!-- Add your columns here -->
                        <th>الإجراءات</th>
                    </tr>
                </thead>
                <tbody>
                    @{ int counter = 1; }
                    @foreach (var item in Model)
                    {
                        <tr>
                            <td>@counter</td>
                            <!-- Display your data here -->
                            <td>
                                <div class="btn-group-actions">
                                    <a asp-action="Details" asp-route-id="@item.Id" class="btn btn-sm btn-info">
                                        <i class="fas fa-eye"></i>
                                    </a>
                                    <a asp-action="Edit" asp-route-id="@item.Id" class="btn btn-sm btn-warning">
                                        <i class="fas fa-edit"></i>
                                    </a>
                                    <a asp-action="Delete" asp-route-id="@item.Id" class="btn btn-sm btn-danger">
                                        <i class="fas fa-trash"></i>
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

---

## قالب سريع لـ Create View

```cshtml
@model YourModel

@{
    ViewData["Title"] = "إضافة جديد";
}

<div class="row">
    <div class="col-md-8 offset-md-2">
        <div class="card">
            <div class="card-header bg-success text-white">
                <h4 class="mb-0"><i class="fas fa-plus"></i> @ViewData["Title"]</h4>
            </div>
            <div class="card-body">
                <form asp-action="Create" method="post" enctype="multipart/form-data">
                    @Html.AntiForgeryToken()
                    <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>
                    
                    <!-- Add your form fields here -->
                    <div class="mb-3">
                        <label asp-for="PropertyName" class="form-label"></label>
                        <input asp-for="PropertyName" class="form-control" />
                        <span asp-validation-for="PropertyName" class="text-danger"></span>
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
    </div>
</div>

@section Scripts {
    @{await Html.RenderPartialAsync("_ValidationScriptsPartial");}
}
```

---

## إضافة عناصر للقائمة

في ملف `_Layout.cshtml`، أضف عناصر جديدة للقائمة:

```cshtml
<li class="nav-item">
    <a class="nav-link" asp-controller="YourController" asp-action="Index">
        <i class="fas fa-your-icon"></i> عنوان القائمة
    </a>
</li>
```

---

## التعامل مع الصور

### في Create Action:
```csharp
if (imageFile != null && imageFile.Length > 0)
{
    using (var ms = new MemoryStream())
    {
        await imageFile.CopyToAsync(ms);
        model.Image = ms.ToArray();
    }
}
```

### عرض الصورة:
```csharp
// في Controller:
public IActionResult GetImage(int id)
{
    var item = _context.Items.Find(id);
    if (item?.Image != null)
        return File(item.Image, "image/jpeg");
    return NotFound();
}

// في View:
<img src="@Url.Action("GetImage", new { id = Model.Id })" class="img-fluid" />
```

---

## التعامل مع القوائم المنسدلة

### في Controller (Create/Edit):
```csharp
ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
```

### في View:
```cshtml
<div class="mb-3">
    <label asp-for="CategoryId" class="form-label"></label>
    <select asp-for="CategoryId" asp-items="ViewBag.CategoryId" class="form-control">
        <option value="">-- اختر الفئة --</option>
    </select>
    <span asp-validation-for="CategoryId" class="text-danger"></span>
</div>
```

---

## التعامل مع التواريخ

### في Model:
```csharp
[DataType(DataType.Date)]
[DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
public DateTime Date { get; set; }
```

### في View:
```cshtml
<input asp-for="Date" type="date" class="form-control" />
```

---

## إنشاء Migration جديد

عند إضافة أو تعديل Model:

```powershell
# إضافة Migration
Add-Migration DescriptionOfChange

# تحديث قاعدة البيانات
Update-Database

# التراجع عن آخر Migration
Update-Database -Migration PreviousMigrationName

# حذف آخر Migration (إذا لم يتم تطبيقه)
Remove-Migration
```

---

## حل المشاكل الشائعة

### مشكلة: قاعدة البيانات غير موجودة
```powershell
# احذف مجلد Migrations
# ثم:
Add-Migration InitialCreate
Update-Database
```

### مشكلة: خطأ في Scaffold
```powershell
# تأكد من تثبيت الحزم:
Install-Package Microsoft.VisualStudio.Web.CodeGeneration.Design
Install-Package Microsoft.EntityFrameworkCore.Tools
```

### مشكلة: الصفحة فارغة بعد Login
تأكد من وجود Session في Program.cs:
```csharp
builder.Services.AddSession();
app.UseSession();
```

---

## نصائح سريعة

1. **استخدم Hot Reload**: اضغط Ctrl+F5 للتشغيل بدون Debug
2. **استخدم Browser Link**: لتحديث المتصفح تلقائياً
3. **افحص الأخطاء**: في Output Window و Error List
4. **استخدم IntelliSense**: اضغط Ctrl+Space للمساعدة
5. **احفظ بانتظام**: اضغط Ctrl+S

---

## الخطوات التالية

1. ✅ قم بإنشاء قاعدة البيانات
2. ✅ اختبر تسجيل الدخول
3. ✅ أضف قسم واحد من الإعدادات
4. ✅ أضف طبيب واحد
5. ✅ أضف مريض واحد
6. ✅ أضف تشخيص واحد
7. ✅ اختبر التقارير
8. 📝 أضف Views المتبقية
9. 🎨 خصص التصميم حسب الحاجة

---

## روابط مفيدة

- [ASP.NET Core Docs](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [Bootstrap RTL](https://getbootstrap.com)
- [Font Awesome Icons](https://fontawesome.com)

---

**ملاحظة**: هذا الدليل يحتوي على الأساسيات. راجع README.md للتفاصيل الكاملة.

---

تم إنشاؤه بواسطة: نظام إدارة العيادة الطبية
التاريخ: 2024
