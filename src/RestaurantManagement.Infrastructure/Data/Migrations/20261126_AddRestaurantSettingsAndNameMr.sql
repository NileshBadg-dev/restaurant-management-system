-- Migration Script: Add Restaurant Configuration & Marathi Language Fields
-- Target: SQL Server (for production deployments; InMemory DB auto-creates via EnsureCreated)

IF OBJECT_ID(N'[RestaurantSettings]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('RestaurantSettings', 'Tagline') IS NULL
        ALTER TABLE [RestaurantSettings] ADD [Tagline] NVARCHAR(200) NOT NULL CONSTRAINT DF_RestaurantSettings_Tagline DEFAULT 'Restaurant Management System';

    IF COL_LENGTH('RestaurantSettings', 'AddressLine1') IS NULL
        ALTER TABLE [RestaurantSettings] ADD [AddressLine1] NVARCHAR(200) NOT NULL CONSTRAINT DF_RestaurantSettings_AddressLine1 DEFAULT '';

    IF COL_LENGTH('RestaurantSettings', 'AddressLine2') IS NULL
        ALTER TABLE [RestaurantSettings] ADD [AddressLine2] NVARCHAR(200) NOT NULL CONSTRAINT DF_RestaurantSettings_AddressLine2 DEFAULT '';

    IF COL_LENGTH('RestaurantSettings', 'City') IS NULL
        ALTER TABLE [RestaurantSettings] ADD [City] NVARCHAR(100) NOT NULL CONSTRAINT DF_RestaurantSettings_City DEFAULT '';

    IF COL_LENGTH('RestaurantSettings', 'State') IS NULL
        ALTER TABLE [RestaurantSettings] ADD [State] NVARCHAR(100) NOT NULL CONSTRAINT DF_RestaurantSettings_State DEFAULT '';

    IF COL_LENGTH('RestaurantSettings', 'PinCode') IS NULL
        ALTER TABLE [RestaurantSettings] ADD [PinCode] NVARCHAR(20) NOT NULL CONSTRAINT DF_RestaurantSettings_PinCode DEFAULT '';
END;

IF OBJECT_ID(N'[Categories]', N'U') IS NOT NULL AND COL_LENGTH('Categories', 'NameMr') IS NULL
    ALTER TABLE [Categories] ADD [NameMr] NVARCHAR(200) NULL;

IF OBJECT_ID(N'[MenuItems]', N'U') IS NOT NULL AND COL_LENGTH('MenuItems', 'NameMr') IS NULL
    ALTER TABLE [MenuItems] ADD [NameMr] NVARCHAR(200) NULL;

IF OBJECT_ID(N'[Users]', N'U') IS NOT NULL AND COL_LENGTH('Users', 'PreferredLanguage') IS NULL
    ALTER TABLE [Users] ADD [PreferredLanguage] NVARCHAR(10) NOT NULL CONSTRAINT DF_Users_PreferredLanguage DEFAULT 'en';
