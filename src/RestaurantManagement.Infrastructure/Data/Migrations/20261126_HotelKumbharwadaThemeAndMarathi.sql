-- Migration Script: Hotel Kumbharwada Bilingual Fields & Mode Settings Removal
-- Target: SQL Server (InMemory DB auto-applies via EnsureCreated + DbInitializer)

-- 1. Add Marathi localization columns to RestaurantSettings
IF OBJECT_ID(N'[RestaurantSettings]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('RestaurantSettings', 'RestaurantNameMr') IS NULL
        ALTER TABLE [RestaurantSettings] ADD [RestaurantNameMr] NVARCHAR(200) NULL;

    IF COL_LENGTH('RestaurantSettings', 'TaglineMr') IS NULL
        ALTER TABLE [RestaurantSettings] ADD [TaglineMr] NVARCHAR(200) NULL;

    IF COL_LENGTH('RestaurantSettings', 'AddressMr') IS NULL
        ALTER TABLE [RestaurantSettings] ADD [AddressMr] NVARCHAR(400) NULL;

    -- Remove mode columns completely
    IF COL_LENGTH('RestaurantSettings', 'KitchenMode') IS NOT NULL
        ALTER TABLE [RestaurantSettings] DROP COLUMN [KitchenMode];

    IF COL_LENGTH('RestaurantSettings', 'TrackReadyStatus') IS NOT NULL
        ALTER TABLE [RestaurantSettings] DROP COLUMN [TrackReadyStatus];

    IF COL_LENGTH('RestaurantSettings', 'KitchenLiteScreen') IS NOT NULL
        ALTER TABLE [RestaurantSettings] DROP COLUMN [KitchenLiteScreen];

    IF COL_LENGTH('RestaurantSettings', 'SinglePersonMode') IS NOT NULL
        ALTER TABLE [RestaurantSettings] DROP COLUMN [SinglePersonMode];

    UPDATE [RestaurantSettings]
    SET [RestaurantName] = N'Hotel Kumbharwada',
        [RestaurantNameMr] = N'हॉटेल कुंभारवाडा',
        [Tagline] = N'Authentic Village Dining & Hospitality',
        [TaglineMr] = N'अस्सल गावरान जेवण आणि आतिथ्य',
        [AddressMr] = N'१०१ हेरिटेज कोर्टयार्ड, कुंभारवाडा रोड, पुणे, महाराष्ट्र - ४११००२',
        [LogoUrl] = N'/assets/hotel-logo.jpg'
    WHERE [Id] = 1;
END;

-- 2. Add NameMr to Floors and populate Marathi floor names
IF OBJECT_ID(N'[Floors]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('Floors', 'NameMr') IS NULL
        ALTER TABLE [Floors] ADD [NameMr] NVARCHAR(200) NULL;

    UPDATE [Floors] SET [NameMr] = N'तळमजला' WHERE [Name] = N'Ground Floor' AND [NameMr] IS NULL;
    UPDATE [Floors] SET [NameMr] = N'पहिला मजला' WHERE [Name] = N'First Floor' AND [NameMr] IS NULL;
    UPDATE [Floors] SET [NameMr] = N'बाहेरील पॅटिओ' WHERE [Name] = N'Outdoor Patio' AND [NameMr] IS NULL;
END;

-- 3. Add NameMr to Tables and populate Marathi table labels
IF OBJECT_ID(N'[Tables]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('Tables', 'NameMr') IS NULL
        ALTER TABLE [Tables] ADD [NameMr] NVARCHAR(100) NULL;

    UPDATE [Tables] SET [NameMr] = N'टेबल ०१' WHERE [TableNumber] = N'01' AND [NameMr] IS NULL;
    UPDATE [Tables] SET [NameMr] = N'टेबल ०२' WHERE [TableNumber] = N'02' AND [NameMr] IS NULL;
    UPDATE [Tables] SET [NameMr] = N'टेबल ०३' WHERE [TableNumber] = N'03' AND [NameMr] IS NULL;
    UPDATE [Tables] SET [NameMr] = N'टेबल ०४' WHERE [TableNumber] = N'04' AND [NameMr] IS NULL;
    UPDATE [Tables] SET [NameMr] = N'टेबल ०५' WHERE [TableNumber] = N'05' AND [NameMr] IS NULL;
    UPDATE [Tables] SET [NameMr] = N'टेबल ११' WHERE [TableNumber] = N'11' AND [NameMr] IS NULL;
    UPDATE [Tables] SET [NameMr] = N'टेबल १२' WHERE [TableNumber] = N'12' AND [NameMr] IS NULL;
    UPDATE [Tables] SET [NameMr] = N'टेबल २१' WHERE [TableNumber] = N'21' AND [NameMr] IS NULL;
    UPDATE [Tables] SET [NameMr] = N'टेबल २२' WHERE [TableNumber] = N'22' AND [NameMr] IS NULL;
END;

-- 4. Add FullNameMr to Users
IF OBJECT_ID(N'[Users]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('Users', 'FullNameMr') IS NULL
        ALTER TABLE [Users] ADD [FullNameMr] NVARCHAR(200) NULL;

    UPDATE [Users] SET [FullNameMr] = N'सिस्टम प्रशासक' WHERE [Username] = N'admin' AND [FullNameMr] IS NULL;
    UPDATE [Users] SET [FullNameMr] = N'राजेश वर्मा' WHERE [Username] = N'manager' AND [FullNameMr] IS NULL;
    UPDATE [Users] SET [FullNameMr] = N'सुरेश पटेल' WHERE [Username] = N'cashier' AND [FullNameMr] IS NULL;
    UPDATE [Users] SET [FullNameMr] = N'अमित कुमार' WHERE [Username] = N'waiter' AND [FullNameMr] IS NULL;
    UPDATE [Users] SET [FullNameMr] = N'शेफ सुनीता' WHERE [Username] = N'kitchen' AND [FullNameMr] IS NULL;
    UPDATE [Users] SET [FullNameMr] = N'विक्रम सिंग' WHERE [Username] = N'inventory' AND [FullNameMr] IS NULL;
END;
