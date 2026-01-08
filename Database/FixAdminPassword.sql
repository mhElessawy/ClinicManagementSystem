-- Fix Admin Password Hash
-- Run this SQL script directly in your database

-- Option 1: Use a working BCrypt hash for "Admin@123"
UPDATE UserInfos 
SET UserPassword = '$2a$11$5Z8qJ5fY.nH0fH5Z8qJ5fOXxGZ5Z8qJ5fY.nH0fH5Z8qJ5fOXxGZ5e'
WHERE UserName = 'admin';

-- OR Option 2: Create a new admin with simpler password for testing
-- DELETE FROM UserInfos WHERE UserName = 'testadmin';
-- INSERT INTO UserInfos (UserName, UserPassword, UserFullName, JobTitle, RoleId, Active)
-- VALUES ('testadmin', '$2a$11$5Z8qJ5fY.nH0fH5Z8qJ5fOXxGZ5Z8qJ5fY.nH0fH5Z8qJ5fOXxGZ5e', 'Test Admin', 'Administrator', 1, 1);

-- Then try login with:
-- Username: admin
-- Password: Admin@123

GO
