-- =========================================================================================
-- HỆ THỐNG QUẢN LÝ SỨC KHỎE GIA ĐÌNH VÀ NHẮC NHỞ CHĂM SÓC (HEALTHCARE MANAGEMENT SYSTEM)
-- SCRIPT KHỞI TẠO CƠ SỞ DỮ LIỆU ĐỒNG BỘ 8 MODULE
-- =========================================================================================

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'HealthCareDB')
BEGIN
    CREATE DATABASE HealthCareDB COLLATE Vietnamese_CI_AS;
END
GO

USE HealthCareDB;
GO

-- =========================================================================================
-- [MODULE 1] XÁC THỰC, TÀI KHOẢN, PHÂN QUYỀN & HỘ GIA ĐÌNH
-- =========================================================================================

-- 1.1 Bảng Vai trò (Roles)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Roles')
BEGIN
    CREATE TABLE Roles (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RoleName NVARCHAR(50) NOT NULL UNIQUE,     -- 'Admin', 'FamilyManager', 'Member'
        Description NVARCHAR(255) NULL
    );
END
GO

-- 1.2 Bảng Tài khoản người dùng (Users)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(50) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(255) NOT NULL,
        FullName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(100) NULL,
        PhoneNumber VARCHAR(15) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        IsActive BIT NOT NULL DEFAULT 1
    );
END
GO

-- 1.3 Bảng Phân quyền người dùng (UserRoles)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserRoles')
BEGIN
    CREATE TABLE UserRoles (
        UserId INT NOT NULL,
        RoleId INT NOT NULL,
        AssignedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
        CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
    );
END
GO

-- 1.4 Bảng Nhóm gia đình (FamilyGroups)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FamilyGroups')
BEGIN
    CREATE TABLE FamilyGroups (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        GroupName NVARCHAR(100) NOT NULL,
        CreatedByUserId INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_FamilyGroups_Users FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id)
    );
END
GO

-- =========================================================================================
-- [MODULE 2] THÀNH VIÊN & HỒ SƠ THỂ TRẠNG NỀN
-- =========================================================================================

-- 2.1 Bảng Thành viên gia đình (Members)
-- UserId có thể NULL để phục vụ người phụ thuộc (trẻ nhỏ, người cao tuổi không dùng smartphone)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Members')
BEGIN
    CREATE TABLE Members (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FamilyGroupId INT NOT NULL,
        UserId INT NULL UNIQUE,                     -- Liên kết tài khoản nếu có
        FullName NVARCHAR(100) NOT NULL,
        Relationship NVARCHAR(50) NOT NULL,        -- 'Chủ hộ', 'Vợ', 'Chồng', 'Con', 'Bố mẹ'
        DateOfBirth DATE NOT NULL,
        Gender NVARCHAR(10) NOT NULL,              -- 'Nam', 'Nữ', 'Khác'
        BloodGroup VARCHAR(5) NULL,                -- 'A+', 'B+', 'O+', 'AB-'...
        InsuranceNumber VARCHAR(30) NULL,
        EmergencyPhone VARCHAR(15) NULL,
        AvatarUrl NVARCHAR(255) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_Members_FamilyGroups FOREIGN KEY (FamilyGroupId) REFERENCES FamilyGroups(Id) ON DELETE CASCADE,
        CONSTRAINT FK_Members_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL
    );
END
GO

-- 2.2 Bảng Tiền sử dị ứng (MemberAllergies)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MemberAllergies')
BEGIN
    CREATE TABLE MemberAllergies (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        MemberId INT NOT NULL,
        AllergenType NVARCHAR(50) NOT NULL,        -- 'Thuốc', 'Thực phẩm', 'Thời tiết'
        AllergenName NVARCHAR(100) NOT NULL,       -- 'Penicillin', 'Hải sản'...
        SeverityLevel NVARCHAR(30) NOT NULL,       -- 'Nhẹ', 'Trung bình', 'Sốc phản vệ'
        Notes NVARCHAR(255) NULL,
        CONSTRAINT FK_MemberAllergies_Members FOREIGN KEY (MemberId) REFERENCES Members(Id) ON DELETE CASCADE
    );
END
GO

-- 2.3 Bảng Bệnh lý nền mạn tính (MemberChronicDiseases)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MemberChronicDiseases')
BEGIN
    CREATE TABLE MemberChronicDiseases (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        MemberId INT NOT NULL,
        DiseaseName NVARCHAR(150) NOT NULL,        -- 'Cao huyết áp', 'Tiểu đường type 2'
        DiagnosedYear INT NULL,
        CurrentStatus NVARCHAR(100) NULL,
        CONSTRAINT FK_MemberChronicDiseases_Members FOREIGN KEY (MemberId) REFERENCES Members(Id) ON DELETE CASCADE
    );
END
GO

-- =========================================================================================
-- [MODULE 3] HỒ SƠ BỆNH ÁN & QUẢN LÝ ĐIỀU TRỊ
-- =========================================================================================

-- 3.1 Bảng Đợt khám bệnh / Bệnh án (MedicalRecords)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MedicalRecords')
BEGIN
    CREATE TABLE MedicalRecords (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        MemberId INT NOT NULL,
        HospitalName NVARCHAR(150) NOT NULL,
        DoctorName NVARCHAR(100) NULL,
        ExamDate DATE NOT NULL,
        Diagnosis NVARCHAR(255) NOT NULL,          -- Chẩn đoán ban đầu
        Conclusion NVARCHAR(MAX) NULL,             -- Kết luận và dặn dò
        Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_MedicalRecords_Members FOREIGN KEY (MemberId) REFERENCES Members(Id) ON DELETE CASCADE
    );
END
GO

-- 3.2 Bảng Tệp đính kèm bệnh án (MedicalAttachments)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MedicalAttachments')
BEGIN
    CREATE TABLE MedicalAttachments (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        MedicalRecordId INT NOT NULL,
        FilePath NVARCHAR(300) NOT NULL,           -- Đường dẫn file vật lý trên server
        OriginalFileName NVARCHAR(200) NOT NULL,
        FileType NVARCHAR(50) NOT NULL,            -- 'image/jpeg', 'application/pdf'
        UploadedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_MedicalAttachments_MedicalRecords FOREIGN KEY (MedicalRecordId) REFERENCES MedicalRecords(Id) ON DELETE CASCADE
    );
END
GO

-- =========================================================================================
-- [MODULE 4] LẬP LỊCH UỐNG THUỐC THEO ĐƠN
-- =========================================================================================

-- 4.1 Bảng Đơn thuốc (Prescriptions)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Prescriptions')
BEGIN
    CREATE TABLE Prescriptions (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        MedicalRecordId INT NULL,                  -- Có thể gắn với bệnh án hoặc nhập độc lập
        MemberId INT NOT NULL,
        PrescriptionName NVARCHAR(150) NOT NULL,
        StartDate DATE NOT NULL,
        EndDate DATE NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_Prescriptions_MedicalRecords FOREIGN KEY (MedicalRecordId) REFERENCES MedicalRecords(Id) ON DELETE SET NULL,
        CONSTRAINT FK_Prescriptions_Members FOREIGN KEY (MemberId) REFERENCES Members(Id)
    );
END
GO

-- 4.2 Bảng Chi tiết danh mục thuốc trong đơn (PrescriptionItems)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PrescriptionItems')
BEGIN
    CREATE TABLE PrescriptionItems (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PrescriptionId INT NOT NULL,
        MedicineName NVARCHAR(150) NOT NULL,
        DosagePerTime NVARCHAR(50) NOT NULL,       -- '1 viên', '5ml'
        MealTiming NVARCHAR(50) NOT NULL,          -- 'Trước ăn 30p', 'Sau ăn', 'Trong bữa ăn'
        SpecialInstruction NVARCHAR(255) NULL,
        CONSTRAINT FK_PrescriptionItems_Prescriptions FOREIGN KEY (PrescriptionId) REFERENCES Prescriptions(Id) ON DELETE CASCADE
    );
END
GO

-- 4.3 Bảng Khung giờ uống thuốc trong ngày (PrescriptionDoses)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PrescriptionDoses')
BEGIN
    CREATE TABLE PrescriptionDoses (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PrescriptionItemId INT NOT NULL,
        DoseTime TIME(0) NOT NULL,                 -- 08:00:00, 12:30:00, 20:00:00
        Quantity DECIMAL(4,1) NOT NULL DEFAULT 1.0,
        CONSTRAINT FK_PrescriptionDoses_PrescriptionItems FOREIGN KEY (PrescriptionItemId) REFERENCES PrescriptionItems(Id) ON DELETE CASCADE
    );
END
GO

-- =========================================================================================
-- [MODULE 5] LẬP LỊCH TÁI KHÁM & TIÊM PHÒNG
-- =========================================================================================

-- 5.1 Bảng Lịch hẹn tái khám và tiêm chủng (Appointments)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Appointments')
BEGIN
    CREATE TABLE Appointments (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        MemberId INT NOT NULL,
        MedicalRecordId INT NULL,                  -- Nếu là lịch tái khám sinh từ bệnh án
        AppointmentType NVARCHAR(50) NOT NULL,     -- 'Tái khám', 'Tiêm phòng', 'Xét nghiệm định kỳ'
        Title NVARCHAR(150) NOT NULL,
        AppointmentDateTime DATETIME2 NOT NULL,
        Location NVARCHAR(200) NOT NULL,
        DoctorName NVARCHAR(100) NULL,
        PreparationNotes NVARCHAR(300) NULL,       -- 'Cần nhịn ăn sáng', 'Uống nhiều nước'
        Status NVARCHAR(30) NOT NULL DEFAULT N'Chờ khám', -- 'Chờ khám', 'Đã hoàn thành', 'Đã hủy'
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_Appointments_Members FOREIGN KEY (MemberId) REFERENCES Members(Id) ON DELETE CASCADE,
        CONSTRAINT FK_Appointments_MedicalRecords FOREIGN KEY (MedicalRecordId) REFERENCES MedicalRecords(Id) ON DELETE NO ACTION
    );
END
GO

-- 5.2 Bảng Sổ tiêm chủng thực tế (ImmunizationRecords)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ImmunizationRecords')
BEGIN
    CREATE TABLE ImmunizationRecords (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        MemberId INT NOT NULL,
        VaccineName NVARCHAR(150) NOT NULL,
        DoseNumber INT NOT NULL,                   -- Mũi 1, Mũi 2, Mũi nhắc
        InjectedDate DATE NOT NULL,
        Location NVARCHAR(150) NULL,
        BatchNumber VARCHAR(50) NULL,
        ReactionNotes NVARCHAR(255) NULL,          -- Phản ứng sau tiêm: sốt, sưng đau
        NextDoseDate DATE NULL,
        CONSTRAINT FK_ImmunizationRecords_Members FOREIGN KEY (MemberId) REFERENCES Members(Id) ON DELETE CASCADE
    );
END
GO

-- =========================================================================================
-- [MODULE 6] TRUNG TÂM LỊCH NHẮC CHĂM SÓC TOÀN DIỆN (Unified Care Reminder Hub)
-- =========================================================================================

-- Bảng Nhắc nhở chăm sóc hằng ngày (CareReminders)
-- Sinh tự động từ Module 4 (Thuốc), Module 5 (Lịch hẹn), hoặc do người dùng tự tạo
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CareReminders')
BEGIN
    CREATE TABLE CareReminders (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        MemberId INT NOT NULL,
        SourceType NVARCHAR(50) NOT NULL,          -- 'Uống thuốc', 'Lịch hẹn', 'Đo chỉ số', 'Tập luyện'
        SourceReferenceId INT NULL,                -- Id của bảng PrescriptionDoses hoặc Appointment
        Title NVARCHAR(200) NOT NULL,
        Description NVARCHAR(300) NULL,
        ScheduledDateTime DATETIME2 NOT NULL,
        IsCompleted BIT NOT NULL DEFAULT 0,
        CompletedAt DATETIME2 NULL,
        CompletedByUserId INT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_CareReminders_Members FOREIGN KEY (MemberId) REFERENCES Members(Id) ON DELETE CASCADE,
        CONSTRAINT FK_CareReminders_CompletedByUsers FOREIGN KEY (CompletedByUserId) REFERENCES Users(Id)
    );
END
GO

-- =========================================================================================
-- [MODULE 7] THEO DÕI TÌNH TRẠNG & CHỈ SỐ SỨC KHỎE
-- =========================================================================================

-- 7.1 Bảng Chỉ số sức khỏe đo lường (HealthMetrics)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HealthMetrics')
BEGIN
    CREATE TABLE HealthMetrics (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        MemberId INT NOT NULL,
        RecordedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        WeightKg DECIMAL(5,2) NULL,
        HeightCm DECIMAL(5,2) NULL,
        SystolicBP INT NULL,                       -- Huyết áp tâm thu (mmHg)
        DiastolicBP INT NULL,                      -- Huyết áp tâm trương (mmHg)
        HeartRate INT NULL,                        -- Nhịp tim (bpm)
        BloodSugar DECIMAL(5,2) NULL,              -- Đường huyết (mmol/L)
        SpO2 INT NULL,                             -- Nồng độ oxy trong máu (%)
        BodyTemperature DECIMAL(4,1) NULL,         -- Nhiệt độ cơ thể (°C)
        IsAbnormal BIT NOT NULL DEFAULT 0,         -- Đánh dấu vượt ngưỡng an toàn
        Notes NVARCHAR(255) NULL,
        CONSTRAINT FK_HealthMetrics_Members FOREIGN KEY (MemberId) REFERENCES Members(Id) ON DELETE CASCADE
    );
END
GO

-- =========================================================================================
-- [MODULE 8] QUẢN TRỊ DANH MỤC DÙNG CHUNG & AUDIT LOGS (Admin Back-office)
-- =========================================================================================

-- 8.1 Bảng Từ điển thuốc chuẩn (MedicineDictionary)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MedicineDictionary')
BEGIN
    CREATE TABLE MedicineDictionary (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        BrandName NVARCHAR(150) NOT NULL,          -- Panadol Extra
        ActiveIngredient NVARCHAR(150) NOT NULL,   -- Paracetamol + Caffeine
        DefaultDosageForm NVARCHAR(50) NOT NULL,   -- Viên nén, Gói bột, Siro
        StandardUsage NVARCHAR(255) NULL,
        IsActive BIT NOT NULL DEFAULT 1
    );
END
GO

-- 8.2 Bảng Phác đồ tiêm chủng chuẩn quốc gia (VaccineProtocols)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'VaccineProtocols')
BEGIN
    CREATE TABLE VaccineProtocols (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        VaccineName NVARCHAR(150) NOT NULL,
        TargetDisease NVARCHAR(200) NOT NULL,
        RecommendedAgeMonths INT NOT NULL,         -- 0 (sơ sinh), 2 tháng, 6 tháng...
        DoseIndex INT NOT NULL,
        Notes NVARCHAR(255) NULL
    );
END
GO

-- 8.3 Bảng Nhật ký hệ thống (SystemAuditLogs)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SystemAuditLogs')
BEGIN
    CREATE TABLE SystemAuditLogs (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NULL,
        Action NVARCHAR(100) NOT NULL,             -- 'LOGIN', 'CREATE_MEMBER', 'DELETE_RECORD'
        EntityName NVARCHAR(100) NULL,
        EntityId INT NULL,
        Timestamp DATETIME2 NOT NULL DEFAULT GETDATE(),
        IpAddress VARCHAR(45) NULL,
        Details NVARCHAR(MAX) NULL,
        CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL
    );
END
GO

-- =========================================================================================
-- ĐÁNH CHỈ MỤC (INDEXES) TỐI ƯU HÓA HIỆU NĂNG CHO CÁC MODULE
-- =========================================================================================

CREATE NONCLUSTERED INDEX IX_Members_FamilyGroupId ON Members(FamilyGroupId);
CREATE NONCLUSTERED INDEX IX_MedicalRecords_MemberId_ExamDate ON MedicalRecords(MemberId, ExamDate DESC);
CREATE NONCLUSTERED INDEX IX_Prescriptions_MemberId_Active ON Prescriptions(MemberId, IsActive);
CREATE NONCLUSTERED INDEX IX_Appointments_MemberId_DateTime ON Appointments(MemberId, AppointmentDateTime);
CREATE NONCLUSTERED INDEX IX_CareReminders_Schedule_Completed ON CareReminders(ScheduledDateTime, IsCompleted) INCLUDE (MemberId, Title);
CREATE NONCLUSTERED INDEX IX_HealthMetrics_MemberId_RecordedAt ON HealthMetrics(MemberId, RecordedAt DESC);
CREATE NONCLUSTERED INDEX IX_Medicine_Name ON MedicineDictionary(BrandName, ActiveIngredient);
GO

-- =========================================================================================
-- DỮ LIỆU MẪU BAN ĐẦU (SEED DATA CHO NHÓM TEST)
-- =========================================================================================

-- 1. Thêm Roles
INSERT INTO Roles (RoleName, Description) VALUES
(N'Admin', N'Quản trị viên toàn hệ thống'),
(N'FamilyManager', N'Người đại diện quản lý hộ gia đình'),
(N'Member', N'Thành viên nhận chăm sóc');

-- 2. Thêm Tài khoản Admin & Quản lý mẫu
INSERT INTO Users (Username, PasswordHash, FullName, Email, PhoneNumber) VALUES
(N'admin', N'AQAAAAEAACcQAAAAEJ...hash_demo', N'Quản Trị Viên Hệ Thống', N'admin@healthcare.local', '0901234567'),
(N'manager', N'AQAAAAEAACcQAAAAEJ...hash_demo', N'Trần Viết Đạt', N'dat@healthcare.local', '0987654321');

-- 3. Gán quyền
INSERT INTO UserRoles (UserId, RoleId) VALUES (1, 1), (2, 2);

-- 4. Thêm Hộ gia đình mẫu
INSERT INTO FamilyGroups (GroupName, CreatedByUserId) VALUES (N'Gia đình Trần Viết Đạt', 2);

-- 5. Thêm Thành viên trong hộ gia đình
INSERT INTO Members (FamilyGroupId, UserId, FullName, Relationship, DateOfBirth, Gender, BloodGroup) VALUES
(1, 2, N'Trần Viết Đạt', N'Chủ hộ', '2006-06-20', N'Nam', 'O+'),
(1, NULL, N'Trần Văn An (Bố)', N'Bố mẹ', '1975-03-12', N'Nam', 'A+'),
(1, NULL, N'Nguyễn Thị Bình (Mẹ)', N'Bố mẹ', '1978-08-25', N'Nữ', 'B+');

-- 6. Thêm Danh mục thuốc & Phác đồ tiêm mẫu
INSERT INTO MedicineDictionary (BrandName, ActiveIngredient, DefaultDosageForm, StandardUsage) VALUES
(N'Panadol Extra', N'Paracetamol 500mg, Caffeine 65mg', N'Viên nén', N'Giảm đau, hạ sốt'),
(N'Amlodipine 5mg', N'Amlodipine besylate', N'Viên nén', N'Điều trị cao huyết áp'),
(N'Augmentin 1g', N'Amoxicillin, Clavulanic acid', N'Viên nén bao phim', N'Kháng sinh');

INSERT INTO VaccineProtocols (VaccineName, TargetDisease, RecommendedAgeMonths, DoseIndex) VALUES
(N'BCG', N'Lao', 0, 1),
(N'Viêm gan B (Sơ sinh)', N'Viêm gan B', 0, 1),
(N'6 trong 1 (Hexaxim)', N'Bạch hầu, Ho gà, Uốn ván, Bại liệt, Hib, Viêm gan B', 2, 1);
GO

ALTER TABLE dbo.Members DROP CONSTRAINT UQ__Members__1788CC4DF999345E;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Members_UserId_Unique' AND object_id = OBJECT_ID('dbo.Members'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Members_UserId_Unique
    ON dbo.Members(UserId)
    WHERE UserId IS NOT NULL;
END
GO