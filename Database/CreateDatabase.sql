-- ======================================================
-- نظام إدارة العيادة الطبية
-- SQL Database Creation Script
-- ======================================================

-- إنشاء قاعدة البيانات
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'ClinicManagementDB')
BEGIN
    CREATE DATABASE ClinicManagementDB;
END
GO

USE ClinicManagementDB;
GO

-- ======================================================
-- جدول المستخدمين (UserInfos)
-- ======================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserInfos')
BEGIN
    CREATE TABLE UserInfos (
        Id INT PRIMARY KEY IDENTITY(1,1),
        UserName NVARCHAR(50) NOT NULL UNIQUE,
        UserPassword NVARCHAR(100) NOT NULL,
        UserFullName NVARCHAR(100),
        UserTel NVARCHAR(20),
        JobTitle NVARCHAR(50),
        Active BIT NOT NULL DEFAULT 1
    );
END
GO

-- ======================================================
-- جدول الأقسام (Departments)
-- ======================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Departments')
BEGIN
    CREATE TABLE Departments (
        Id INT PRIMARY KEY IDENTITY(1,1),
        DepartmentName NVARCHAR(100) NOT NULL
    );
END
GO

-- ======================================================
-- جدول التخصصات (Specialists)
-- ======================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Specialists')
BEGIN
    CREATE TABLE Specialists (
        Id INT PRIMARY KEY IDENTITY(1,1),
        DepartmentId INT,
        SpecialistName NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500),
        FOREIGN KEY (DepartmentId) REFERENCES Departments(Id) ON DELETE SET NULL
    );
END
GO

-- ======================================================
-- جدول الأطباء (DoctorInfos)
-- ======================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DoctorInfos')
BEGIN
    CREATE TABLE DoctorInfos (
        Id INT PRIMARY KEY IDENTITY(1,1),
        DoctorName NVARCHAR(100) NOT NULL,
        DoctorAddress NVARCHAR(200),
        DoctorTitle NVARCHAR(50),
        DoctorId INT,
        SpecialistId INT,
        Active BIT NOT NULL DEFAULT 1,
        DoctorCivilId NVARCHAR(50),
        DoctorTel1 NVARCHAR(20),
        DoctorTel2 NVARCHAR(20),
        UserId INT,
        Gender NVARCHAR(10),
        DoctorDetails NVARCHAR(500),
        RegDate DATETIME,
        DoctorPicture VARBINARY(MAX),
        FOREIGN KEY (UserId) REFERENCES UserInfos(Id) ON DELETE SET NULL,
        FOREIGN KEY (SpecialistId) REFERENCES Specialists(Id) ON DELETE SET NULL
    );
END
GO

-- ======================================================
-- جدول مساعدي الأطباء (DoctorAssists)
-- ======================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DoctorAssists')
BEGIN
    CREATE TABLE DoctorAssists (
        Id INT PRIMARY KEY IDENTITY(1,1),
        DoctorId INT NOT NULL,
        AssistName NVARCHAR(100) NOT NULL,
        AssistTel1 NVARCHAR(20),
        AssistTel2 NVARCHAR(20),
        AssistAddress NVARCHAR(200),
        Active BIT NOT NULL DEFAULT 1,
        FOREIGN KEY (DoctorId) REFERENCES DoctorInfos(Id) ON DELETE CASCADE
    );
END
GO

-- ======================================================
-- جدول المرضى (Patients)
-- ======================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Patients')
BEGIN
    CREATE TABLE Patients (
        Id INT PRIMARY KEY IDENTITY(1,1),
        PatientCivilID NVARCHAR(50) NOT NULL,
        PatientName NVARCHAR(100) NOT NULL,
        PatientTel1 NVARCHAR(20),
        PatientTel2 NVARCHAR(20),
        PatientAddress NVARCHAR(200),
        DoctorId INT,
        FOREIGN KEY (DoctorId) REFERENCES DoctorInfos(Id) ON DELETE SET NULL
    );
END
GO

-- ======================================================
-- جدول التشخيصات (PatientDiagnoses)
-- ======================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PatientDiagnoses')
BEGIN
    CREATE TABLE PatientDiagnoses (
        Id INT PRIMARY KEY IDENTITY(1,1),
        PatientId INT NOT NULL,
        DoctorId INT,
        DiagnosisDate DATETIME NOT NULL DEFAULT GETDATE(),
        DiagnosisDetails NVARCHAR(1000),
        DiagnosisFile VARBINARY(MAX),
        Active BIT NOT NULL DEFAULT 1,
        FOREIGN KEY (PatientId) REFERENCES Patients(Id) ON DELETE CASCADE,
        FOREIGN KEY (DoctorId) REFERENCES DoctorInfos(Id) ON DELETE SET NULL
    );
END
GO

-- ======================================================
-- إدراج البيانات الأولية
-- ======================================================

-- إدراج مستخدم admin
-- كلمة المرور المشفرة بـ BCrypt: Admin@123
IF NOT EXISTS (SELECT * FROM UserInfos WHERE UserName = 'admin')
BEGIN
    INSERT INTO UserInfos (UserName, UserPassword, UserFullName, JobTitle, Active)
    VALUES ('admin', '$2a$11$xvDZ8qhqH5K5pXY7ZGHnS.yQqV3xLvHJ3QQv3d1KqD8Y0L7l5N9xG', N'مدير النظام', N'مدير', 1);
END
GO

-- إدراج الأقسام
IF NOT EXISTS (SELECT * FROM Departments WHERE Id = 1)
BEGIN
    SET IDENTITY_INSERT Departments ON;
    
    INSERT INTO Departments (Id, DepartmentName) VALUES
    (1, N'الباطنية'),
    (2, N'الجراحة'),
    (3, N'الأطفال'),
    (4, N'العظام'),
    (5, N'القلب');
    
    SET IDENTITY_INSERT Departments OFF;
END
GO

-- إدراج التخصصات
IF NOT EXISTS (SELECT * FROM Specialists WHERE Id = 1)
BEGIN
    SET IDENTITY_INSERT Specialists ON;
    
    INSERT INTO Specialists (Id, DepartmentId, SpecialistName, Description) VALUES
    (1, 1, N'أمراض القلب', N'تشخيص وعلاج أمراض القلب'),
    (2, 2, N'جراحة عامة', N'العمليات الجراحية العامة'),
    (3, 3, N'طب الأطفال', N'رعاية صحة الأطفال'),
    (4, 4, N'جراحة العظام', N'جراحة وعلاج العظام'),
    (5, 5, N'قلب وأوعية دموية', N'أمراض القلب والأوعية الدموية');
    
    SET IDENTITY_INSERT Specialists OFF;
END
GO

-- ======================================================
-- إنشاء Indexes لتحسين الأداء
-- ======================================================

-- Index على اسم المستخدم
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserInfos_UserName')
BEGIN
    CREATE INDEX IX_UserInfos_UserName ON UserInfos(UserName);
END
GO

-- Index على الرقم المدني للمريض
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Patients_CivilID')
BEGIN
    CREATE INDEX IX_Patients_CivilID ON Patients(PatientCivilID);
END
GO

-- Index على تاريخ التشخيص
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PatientDiagnoses_Date')
BEGIN
    CREATE INDEX IX_PatientDiagnoses_Date ON PatientDiagnoses(DiagnosisDate);
END
GO

-- ======================================================
-- استعلامات مفيدة للتحقق
-- ======================================================

-- عرض عدد السجلات في كل جدول
SELECT 'UserInfos' AS TableName, COUNT(*) AS RecordCount FROM UserInfos
UNION ALL
SELECT 'Departments', COUNT(*) FROM Departments
UNION ALL
SELECT 'Specialists', COUNT(*) FROM Specialists
UNION ALL
SELECT 'DoctorInfos', COUNT(*) FROM DoctorInfos
UNION ALL
SELECT 'Patients', COUNT(*) FROM Patients
UNION ALL
SELECT 'PatientDiagnoses', COUNT(*) FROM PatientDiagnoses
UNION ALL
SELECT 'DoctorAssists', COUNT(*) FROM DoctorAssists;
GO

PRINT N'تم إنشاء قاعدة البيانات بنجاح!';
GO
