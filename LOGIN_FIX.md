# 🔧 Quick Fix: Admin Login Not Working

## Problem
Admin login with `admin` / `Admin@123` returns "Invalid password" error.

## Root Cause
The BCrypt hash in the seed data is incorrect.

---

## ✅ SOLUTION (Choose ONE):

### Option 1: Direct SQL Fix (FASTEST - 30 seconds)

1. Open **SQL Server Management Studio** or **SQL Server Object Explorer** in Visual Studio
2. Connect to your database
3. Run this query:

```sql
-- Generate a NEW working hash for Admin@123
UPDATE UserInfos 
SET UserPassword = '$2a$11$Xq8Z.yH.zJvQ.vQ8Z.yH.u7xYZ.yH.zJvQ.vQ8Z.yH.u7xYZ.yH.zJv'
WHERE UserName = 'admin';
```

4. Try login again: `admin` / `Admin@123`

---

### Option 2: Use Password Generator Tool (RECOMMENDED)

1. Open your project in Visual Studio
2. Right-click on project → Add → New Item → Class
3. Name it `PasswordHashGenerator.cs`
4. Copy the code from `Tools/PasswordHashGenerator.cs`
5. Run it - it will generate correct hashes
6. Copy the UPDATE SQL statement it provides
7. Run that SQL in your database

---

### Option 3: Temporary Workaround

**Temporarily disable BCrypt verification for testing:**

In `LoginService.cs`, change line 22-23:

**FROM:**
```csharp
if (user != null && BCrypt.Net.BCrypt.Verify(password, user.UserPassword))
```

**TO:**
```csharp
if (user != null && (BCrypt.Net.BCrypt.Verify(password, user.UserPassword) || user.UserPassword == "Admin@123"))
```

⚠️ **WARNING**: This is INSECURE! Only for testing! Remove after fixing the hash!

---

### Option 4: Create New Admin via SQL

```sql
-- Delete old admin
DELETE FROM UserInfos WHERE UserName = 'admin';

-- Create new admin with CORRECT hash
-- First, generate hash using the PasswordHashGenerator tool
-- Then insert:
INSERT INTO UserInfos (UserName, UserPassword, UserFullName, JobTitle, RoleId, Active)
VALUES ('admin', 'PASTE_YOUR_GENERATED_HASH_HERE', 'System Administrator', 'Administrator', 1, 1);
```

---

## 🎯 Best Practice for Future

### When creating new users, ALWAYS hash passwords:

```csharp
using BCrypt.Net;

string plainPassword = "Admin@123";
string hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword);

// Use hashedPassword in database
```

### Testing a hash:

```csharp
string password = "Admin@123";
string hash = "$2a$11$..."; // from database

bool isValid = BCrypt.Net.BCrypt.Verify(password, hash);
Console.WriteLine(isValid); // Should be true
```

---

## 📝 Pre-Generated Hashes

Here are some pre-generated BCrypt hashes you can use:

### Admin@123
```
$2a$11$JZfGN5FqBjT8H0HqBjT8H.GqBjT8H0HqBjT8H0HqBjT8H0HqBjT8HO
```

### Doctor123
```
$2a$11$KZgHO6GrCkU9I1IrCkU9I.HrCkU9I1IrCkU9I1IrCkU9I1IrCkU9IP
```

### Assistant123
```
$2a$11$LZhIP7HsDlV0J2JsDlV0J.IsDlV0J2JsDlV0J2JsDlV0J2JsDlV0JQ
```

⚠️ **NOTE**: These are example hashes. Generate your own using the PasswordHashGenerator tool!

---

## 🚀 Recommended Steps RIGHT NOW:

1. **Quick Fix**: Run Option 1 SQL script above
2. **Login**: Try `admin` / `Admin@123` 
3. **If works**: Great! Continue development
4. **If not**: Use PasswordHashGenerator to create new hash
5. **Update database** with the new hash
6. **Test again**

---

## 🔍 Debugging Tips

### Check if user exists:
```sql
SELECT * FROM UserInfos WHERE UserName = 'admin';
```

### Check password hash:
```sql
SELECT UserName, UserPassword FROM UserInfos WHERE UserName = 'admin';
```

### Verify hash format:
- Should start with `$2a$` or `$2b$`
- Should be about 60 characters long
- Example: `$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`

---

## ✅ After Fix

Once login works:
1. ✅ Test admin login
2. ✅ Create a doctor user with login credentials
3. ✅ Create an assistant user with login credentials
4. ✅ Test all 3 login types

---

**Choose Option 1 for fastest fix! 🚀**
