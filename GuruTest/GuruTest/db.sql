USE [TestGuruDB]
GO
/****** Object:  Schema [Auth]    Script Date: 02/02/2010 10:48:56 ******/
CREATE SCHEMA [Auth] AUTHORIZATION [dbo]
GO
/****** Object:  Schema [Articles]    Script Date: 02/02/2010 10:48:56 ******/
CREATE SCHEMA [Articles] AUTHORIZATION [dbo]
GO
/****** Object:  Table [Auth].[AccountRole]    Script Date: 02/02/2010 10:48:57 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Auth].[AccountRole](
	[Account] [int] NOT NULL,
	[Role] [int] NOT NULL,
 CONSTRAINT [PK_AccountRole] PRIMARY KEY CLUSTERED 
(
	[Account] ASC,
	[Role] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Trigger [AccountRoleBlockUpdate]    Script Date: 02/02/2010 10:48:57 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Dawid Lewicki
-- Create date: 20.11.2009r.
-- Description:	Zablokowanie jakiejkolwiek edycji rekordów w tabeli AccountRole
-- =============================================
CREATE TRIGGER [Auth].[AccountRoleBlockUpdate]
   ON  [Auth].[AccountRole]
   INSTEAD OF UPDATE
AS 
BEGIN
	--Zablokowanie operacji
	IF (SELECT COUNT(*) FROM deleted) > 0
	BEGIN
		RAISERROR ('Rekordy tabeli Auth.AccountRole nie mogą być edytowane.', 17, 1)
		ROLLBACK TRAN
		RETURN
	END
END
GO
/****** Object:  Table [Articles].[Comments]    Script Date: 02/02/2010 10:48:57 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [Articles].[Comments](
	[CommentID] [int] IDENTITY(1,1) NOT NULL,
	[ArticleID] [int] NOT NULL,
	[AuthorID] [int] NOT NULL,
	[PostDate] [datetime] NOT NULL,
	[Content] [varchar](2000) NOT NULL,
 CONSTRAINT [PK_Comments] PRIMARY KEY CLUSTERED 
(
	[CommentID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [Articles].[Articles]    Script Date: 02/02/2010 10:48:57 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [Articles].[Articles](
	[ArticleID] [int] IDENTITY(1,1) NOT NULL,
	[AuthorID] [int] NOT NULL,
	[Title] [varchar](150) NOT NULL,
	[Tags] [varchar](600) NULL,
	[Category] [varchar](40) NOT NULL,
	[CreationDate] [datetime] NOT NULL,
	[Status] [int] NOT NULL,
	[ShowCount] [int] NOT NULL,
	[AverageGrade] [float] NOT NULL,
	[GradesCount] [int] NOT NULL,
	[Content] [text] NOT NULL,
 CONSTRAINT [PK_Articles] PRIMARY KEY CLUSTERED 
(
	[ArticleID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [Articles].[Attachments]    Script Date: 02/02/2010 10:48:57 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Articles].[Attachments](
	[ArticleID] [int] NOT NULL,
	[FileID] [int] NOT NULL,
 CONSTRAINT [PK_Attachments] PRIMARY KEY CLUSTERED 
(
	[ArticleID] ASC,
	[FileID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [Articles].[UserFiles]    Script Date: 02/02/2010 10:48:57 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [Articles].[UserFiles](
	[FileID] [int] IDENTITY(1,1) NOT NULL,
	[OwnerID] [int] NULL,
	[Size] [int] NOT NULL,
	[FilePath] [varchar](280) NOT NULL,
	[DisplayedName] [varchar](100) NOT NULL,
 CONSTRAINT [PK_UserFiles] PRIMARY KEY CLUSTERED 
(
	[FileID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [Auth].[Role]    Script Date: 02/02/2010 10:48:57 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Auth].[Role](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[Administrator] [bit] NOT NULL,
 CONSTRAINT [PK_Role] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY],
 CONSTRAINT [SK_RoleName] UNIQUE NONCLUSTERED 
(
	[Name] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Trigger [SaveLastAdminRole]    Script Date: 02/02/2010 10:48:57 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Dawid Lewicki
-- Create date: 20.11.2009r.
-- Description:	Trigger blokujący usuwanie ostatniej istniejącej roli administratorskiej
-- =============================================
CREATE TRIGGER [Auth].[SaveLastAdminRole]
   ON  [Auth].[Role]
   INSTEAD OF UPDATE, DELETE
AS 
BEGIN
	--Zapobieżenie dodatkowemu zliczaniu wierszy
	SET NOCOUNT ON;

	--Sprawdzenie czy wykonanie polecenia UPDATE/DELETE cokolwiek zmieni
	IF (SELECT COUNT(*) FROM deleted) = 0 RETURN
	
	--Liczba usuwanych ról administratorskich
	DECLARE @deletedAdmins int
	--Liczba wszystkich ról administratorskich
	DECLARE @allAdmins int
	--Przetwarzane polecenie to UPDATE - true, DELETE - false
	DECLARE @update bit

	--Ustalenie czy mamy do czynienie z poleceniem UPDATE czy DELETE
	IF ((SELECT COUNT(*) FROM inserted) > 0)
	BEGIN
		SELECT @update = 1;
	END
	ELSE
	BEGIN
		SELECT @update = 0;
	END

	--Obliczenie ilości ról administratorskich
	SELECT @deletedAdmins = COUNT(*) FROM deleted WHERE Administrator = 1;
	SELECT @allAdmins = COUNT(*) FROM Auth.Role WHERE Administrator = 1;

	--W przypadku polecenia UPDATE od ilości usuwanych ról administratorskich
	--należy odjąć liczbę ról administratorskich dodawanych do tabeli
	IF @update = 1
	BEGIN
		SELECT @deletedAdmins = 
			@deletedAdmins - (SELECT COUNT(*) FROM inserted WHERE Administrator = 1)
	END

	--Ewentualne zablokowanie wykonywania polecenia
	IF @deletedAdmins = @allAdmins AND @deletedAdmins > 0
	BEGIN
		RAISERROR ('Nie można usunąć ostatniego administratora z tabeli Auth.Roles', 16, 1)
		ROLLBACK TRAN
		RETURN
	END

	IF @update = 1
	BEGIN
		UPDATE Auth.[Role] SET
			[Name] = i.[Name],
			[Administrator] = i.[Administrator]
		FROM inserted i WHERE Auth.[Role].[ID] = i.[ID]
	END
	ELSE
	BEGIN
		--Usunięcie powiązań wyznaczonych ról z tabeli AccountRole
		DELETE FROM Auth.AccountRole WHERE [Role] IN (SELECT [ID] FROM deleted)
	
		--Usunięcie wyznaczonych ról
		DELETE FROM Auth.[Role] WHERE [ID] IN (SELECT [ID] FROM deleted)
	END
END
GO
/****** Object:  Table [Auth].[Account]    Script Date: 02/02/2010 10:48:57 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [Auth].[Account](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Login] [nvarchar](50) NOT NULL,
	[Password] [binary](16) NOT NULL,
	[PasswordQuestion] [nvarchar](256) NOT NULL,
	[PasswordAnswer] [binary](16) NOT NULL,
	[Active] [bit] NOT NULL,
	[Deleted] [bit] NOT NULL,
	[Locked] [bit] NOT NULL,
	[ActivationGUID] [uniqueidentifier] NOT NULL,
	[TerminationGUID] [uniqueidentifier] NOT NULL,
	[Email] [nvarchar](50) NULL,
	[FirstName] [nvarchar](50) NULL,
	[LastName] [nvarchar](50) NULL,
	[City] [nvarchar](50) NULL,
	[Created] [datetime] NOT NULL,
	[LastActivityDate] [datetime] NOT NULL,
	[LockDateTime] [datetime] NULL,
	[BadPasswordAttempts] [int] NOT NULL,
	[BadPasswordWindowStart] [datetime] NOT NULL,
	[BadAnswerAttempts] [int] NOT NULL,
	[BadAnswerWindowStart] [datetime] NOT NULL,
 CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY],
 CONSTRAINT [SK_Login] UNIQUE NONCLUSTERED 
(
	[Login] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Trigger [SaveLastAdminAccountRole]    Script Date: 02/02/2010 10:48:57 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Dawid Lewicki
-- Create date: 20.11.2009r.
-- Description:	Trigger zapobiegający usunięciu powiązania ostatniego
--				administratora z ostanią jego rolą administratorską
-- =============================================
CREATE TRIGGER [Auth].[SaveLastAdminAccountRole]
   ON  [Auth].[AccountRole]
   INSTEAD OF DELETE
AS
BEGIN
	--Zapobieżenie dodatkowemu zliczaniu wierszy
	SET NOCOUNT ON;

	--Sprawdzenie czy usuwane są jakiekolwiek wiersze
	IF (SELECT COUNT(*) FROM deleted) = 0 RETURN

	IF 
		--Usuwanie wybranych rekordów nie spowoduje usunięcia jakichkolwiek
		--powiązań pomiędzy aktywnym userem a rolą administratorską
		0 = (SELECT COUNT(*) FROM deleted del WHERE
			EXISTS (SELECT * FROM Auth.Account WHERE
				Active = 1 AND Deleted = 0 And [ID] = del.[Account])
			AND EXISTS (SELECT * FROM Auth.Role WHERE
				Administrator = 1 AND [ID] = del.[Role])
		)
		OR
		--Istnieje jakieś nie usuwane właśnie połączenie pomiędzy
		--aktywnym userem a rolą administratorską
		0 < (SELECT COUNT(*) FROM Auth.AccountRole WHERE
		(Account NOT IN (SELECT Account FROM deleted)
		OR [Role] NOT IN (SELECT [Role] FROM deleted))
		AND EXISTS (SELECT * FROM Auth.Account WHERE
			Active = 1 AND Deleted = 0 AND Auth.Account.[ID] = Account)
		AND EXISTS (SELECT * FROM Auth.[Role] WHERE
			Administrator = 1 AND Auth.[Role].[ID] = [Role]))
	BEGIN
		--Usunięcie wyznaczonych powiązań
		DECLARE Pointer CURSOR
		FOR SELECT Account, [Role]
		FROM deleted
		OPEN Pointer;

		DECLARE @TempRole int
		DECLARE @TempAccount int
		
		FETCH NEXT FROM Pointer INTO @TempAccount, @TempRole
		--wykonianie pętli dla każdego rekordu
		WHILE (@@FETCH_STATUS <> -1)
		BEGIN
			DELETE FROM Auth.AccountRole WHERE Account = @TempAccount AND [Role] = @TempRole
			FETCH NEXT FROM Pointer INTO @TempAccount, @TempRole
		END

		CLOSE Pointer;
		DEALLOCATE Pointer;
	END
	ELSE
	BEGIN
		--Zablokowanie usuwania
		RAISERROR ('Nie można usunąć ostatniego powiązania administratora z jego administratorską rolą z tabeli Auth.AccountRole', 11, 1)
		ROLLBACK TRAN
		RETURN
	END
END
GO
/****** Object:  Trigger [SaveLastAdminAccount]    Script Date: 02/02/2010 10:48:57 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Dawid Lewicki
-- Create date: 20.11.2009r.
-- Description:
			--Usuwanie użytkowników spowoduje ich edycję do pożądanego stanu
			--Usuwanie ostatniego administratora zostanie zablokowane
-- =============================================
CREATE TRIGGER [Auth].[SaveLastAdminAccount]
   ON  [Auth].[Account]
   INSTEAD OF DELETE
AS 
BEGIN
	--Zapobieżenie dodatkowemu zliczaniu wierszy
	SET NOCOUNT ON;

    --Sprawdzenie czy usuwane są jakiekolwiek wiersze
	IF (SELECT COUNT(*) FROM deleted WHERE Deleted = 0) = 0 RETURN

	--Liczba usuwanych administratorów
	DECLARE @deletedAdmins int
	--Liczba wszystkich administratorów
	DECLARE @allAdmins int

	--Obliczenie ilości wszystkich administratorów
	SELECT @allAdmins = COUNT(*) FROM Auth.Account acc WHERE
		Deleted = 0 AND Active = 1 AND EXISTS (
		SELECT * FROM Auth.AccountRole accRl WHERE acc.[ID] = accRl.Account AND EXISTS (
			SELECT * FROM Auth.[Role] rl WHERE accRl.[Role] = rl.[ID] AND rl.Administrator = 1
		)
	)

	--Obliczenie ilości usuwanych administratorów
	SELECT @deletedAdmins = COUNT(*) FROM Auth.Account acc WHERE 
		acc.[ID] IN (SELECT [ID] FROM deleted WHERE Deleted = 0 AND Active = 1) AND EXISTS (
		SELECT * FROM Auth.AccountRole accRl WHERE acc.[ID] = accRl.Account AND EXISTS (
			SELECT * FROM Auth.[Role] rl WHERE accRl.[Role] = rl.[ID] AND rl.Administrator = 1
		)
	)

	IF @allAdmins <= @deletedAdmins AND @deletedAdmins > 0
	BEGIN
		--Zablokowanie usuwania rekordów
		RAISERROR ('Nie można usunąć ostatniego aktywnego administratora z tabeli Auth.Account', 16, 1)
		ROLLBACK TRAN
		RETURN
	END
	ELSE
	BEGIN
		--Usunięcie wszelkich powiązań z wyznaczonymi kontami w tabeli AccountRole
		DELETE FROM Auth.AccountRole WHERE Account IN (SELECT [ID] FROM deleted)

		--Logiczne usunięcie wyznaczonych kont
		--W celu zwolnienia loginu jest on zmieniany na przypadkowy GUID
		UPDATE Auth.Account SET
			Active = 0,
			Deleted = 1,
			[Login] = newid()
		WHERE [ID] IN (SELECT [ID] FROM deleted WHERE Deleted = 0)
	END
END
GO
/****** Object:  Table [Auth].[RestrictedEmail]    Script Date: 02/02/2010 10:48:57 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Auth].[RestrictedEmail](
	[Email] [nvarchar](50) NOT NULL,
	[Blocked] [datetime] NOT NULL,
 CONSTRAINT [PK_RestrictedEmail] PRIMARY KEY NONCLUSTERED 
(
	[Email] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Trigger [AccountInsertValidation]    Script Date: 02/02/2010 10:48:57 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Dawid Lewicki
-- Create date: 21.11.2009r.
-- Description:	Kontrola poprawności wstawianych loginów i e-maili
--				Uzupełnianie daty wstawienia rekordu
-- =============================================
CREATE TRIGGER [Auth].[AccountInsertValidation]
   ON  [Auth].[Account]
   INSTEAD OF INSERT, UPDATE
AS
BEGIN
	--Zapobieżenie dodatkowemu zliczaniu wierszy
	--SET NOCOUNT ON

	--Sprawdzenie czy wykonanie polecenia UPDATE/DELETE cokolwiek zmieni
	IF (SELECT COUNT(*) FROM inserted) = 0 RETURN

	--Przetwarzane polecenie to UPDATE - true, INSERT - false
	DECLARE @update bit

    --Ustalenie czy mamy do czynienia z poleceniem UPDATE czy INSERT
	IF ((SELECT COUNT(*) FROM deleted) > 0)
	BEGIN
		SELECT @update = 1;
	END
	ELSE
	BEGIN
		SELECT @update = 0;
	END

	--Próba wstawienia drugi raz istniejącego loginu
	IF 0 < (SELECT COUNT(*) FROM inserted ins WHERE
		[Login] IN (SELECT [Login] FROM Auth.Account WHERE [ID] <> ins.[ID]))
	BEGIN
		--Zablokowanie wstawiania/edycji rekordów
		RAISERROR ('Podany login już istnieje w tabeli Auth.Account', 11, 4)
		ROLLBACK TRAN
		RETURN
	END

	--Próba wstawienia zastrzeżonego adresu e-mail, różnego od poprzedniego
	IF 0 < (SELECT COUNT(*) FROM inserted ins WHERE
		(@update = 0 OR ins.Email <> (SELECT Email FROM deleted WHERE [ID] = ins.[ID]))
		AND (ins.Email IN (SELECT Email FROM Auth.RestrictedEmail)))
	BEGIN
		--Zablokowanie wstawiania/edycji rekordów
		RAISERROR ('Próba wstawienia zastrzeżonego adresu e-mail do tabeli Auth.Account', 11, 3)
		ROLLBACK TRAN
		RETURN
	END

	--Wykonanie operacji
	IF @update = 1
	BEGIN
		UPDATE Auth.Account SET
			[Login] = i.[Login],
			Password = i.Password,
			PasswordQuestion = i.PasswordQuestion,
			PasswordAnswer = i.PasswordAnswer,
			Active = i.Active,
			Deleted = i.Deleted,
			ActivationGUID = i.ActivationGUID,
			TerminationGUID = i.TerminationGUID,
			Email = i.Email,
			FirstName = i.FirstName,
			LastName = i.LastName,
			City = i.City,
			Locked = i.Locked,
			LockDateTime = i.LockDateTime,
			BadPasswordAttempts = i.BadPasswordAttempts,
			BadPasswordWindowStart = i.BadPasswordWindowStart,
			BadAnswerAttempts = i.BadAnswerAttempts,
			BadAnswerWindowStart = i.BadAnswerWindowStart,
			Created = i.Created,
			LastActivityDate = i.LastActivityDate
		FROM inserted i WHERE Auth.Account.[ID] = i.[ID]
	END
	ELSE
	BEGIN
		INSERT INTO Auth.Account (
			[Login],
			Password,
			PasswordQuestion,
			PasswordAnswer,
			Active,
			Deleted,
			ActivationGUID,
			TerminationGUID,
			Email,
			FirstName,
			LastName,
			City,
			Locked,
			LockDateTime,
			BadPasswordAttempts,
			BadPasswordWindowStart,
			BadAnswerAttempts,
			BadAnswerWindowStart,
			Created,
			LastActivityDate)
			SELECT 
				[Login],
				Password,
				PasswordQuestion,
				PasswordAnswer,
				Active,
				Deleted,
				ActivationGUID,
				TerminationGUID,
				Email,
				FirstName,
				LastName,
				City,
				0,
				NULL,
				0,
				GETDATE(),
				0,
				GETDATE(),
				GETDATE(),
				GETDATE()
			FROM inserted
	END
END
GO
/****** Object:  Trigger [ProcessRestrictedEmail]    Script Date: 02/02/2010 10:48:57 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Dawid Lewicki
-- Create date: 21.11.2009r.
-- Description:	Zapobieganie dublowaniu się adresów e-mail
--				Usuwanie powiązanych nieaktywnych userów z podanych e-mailem
-- =============================================
CREATE TRIGGER [Auth].[ProcessRestrictedEmail]
   ON  [Auth].[RestrictedEmail]
   INSTEAD OF INSERT ,UPDATE
AS 
BEGIN
	--Zapobieżenie dodatkowemu zliczaniu wierszy
	SET NOCOUNT ON;

	--Sprawdzenie czy wykonanie polecenia UPDATE/DELETE cokolwiek zmieni
	IF (SELECT COUNT(*) FROM inserted) = 0 RETURN

	--Przetwarzane polecenie to UPDATE - true, INSERT - false
	DECLARE @update bit

    --Ustalenie czy mamy do czynienie z poleceniem UPDATE czy INSERT
	IF ((SELECT COUNT(*) FROM deleted) > 0)
	BEGIN
		SELECT @update = 1;
	END
	ELSE
	BEGIN
		SELECT @update = 0;
	END

	--Usunięcie powiązanych z adresem nieaktywnych userów
	DELETE FROM Auth.Account WHERE Active = 0 AND Deleted = 0 AND Email IN
		(SELECT Email FROM inserted)

	--Wykasowanie wszystkich edytowanych adresów e-mail
	IF @update = 1
	BEGIN
		DELETE FROM Auth.RestrictedEmail WHERE Email IN
			(SELECT Email FROM deleted)
	END

	--Dodanie do tabeli tylko unikalnych adresów
	INSERT INTO Auth.RestrictedEmail (Email, Blocked)
		(SELECT Email, GETDATE() FROM inserted WHERE Email NOT IN
			(SELECT Email FROM Auth.RestrictedEmail))
END
GO
/****** Object:  Table [Auth].[PermissionRole]    Script Date: 02/02/2010 10:48:57 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Auth].[PermissionRole](
	[Permission] [nvarchar](50) NOT NULL,
	[Role] [int] NOT NULL,
 CONSTRAINT [PK_PermissionRole_1] PRIMARY KEY CLUSTERED 
(
	[Permission] ASC,
	[Role] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [Auth].[Permission]    Script Date: 02/02/2010 10:48:57 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Auth].[Permission](
	[Name] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_Permission_Name] PRIMARY KEY CLUSTERED 
(
	[Name] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Default [DF_Articles_Status]    Script Date: 02/02/2010 10:48:57 ******/
ALTER TABLE [Articles].[Articles] ADD  CONSTRAINT [DF_Articles_Status]  DEFAULT ((0)) FOR [Status]
GO
/****** Object:  Default [DF_Articles_ShowCount]    Script Date: 02/02/2010 10:48:57 ******/
ALTER TABLE [Articles].[Articles] ADD  CONSTRAINT [DF_Articles_ShowCount]  DEFAULT ((0)) FOR [ShowCount]
GO
/****** Object:  Default [DF_Articles_AverageGrade]    Script Date: 02/02/2010 10:48:57 ******/
ALTER TABLE [Articles].[Articles] ADD  CONSTRAINT [DF_Articles_AverageGrade]  DEFAULT ((0)) FOR [AverageGrade]
GO
/****** Object:  Default [DF_Articles_GradesCount]    Script Date: 02/02/2010 10:48:57 ******/
ALTER TABLE [Articles].[Articles] ADD  CONSTRAINT [DF_Articles_GradesCount]  DEFAULT ((0)) FOR [GradesCount]
GO
/****** Object:  Default [DF_Role_Administrator]    Script Date: 02/02/2010 10:48:57 ******/
ALTER TABLE [Auth].[Role] ADD  CONSTRAINT [DF_Role_Administrator]  DEFAULT ((0)) FOR [Administrator]
GO
/****** Object:  Default [DF_User_Active]    Script Date: 02/02/2010 10:48:57 ******/
ALTER TABLE [Auth].[Account] ADD  CONSTRAINT [DF_User_Active]  DEFAULT ((0)) FOR [Active]
GO
/****** Object:  Default [DF_User_Deleted]    Script Date: 02/02/2010 10:48:57 ******/
ALTER TABLE [Auth].[Account] ADD  CONSTRAINT [DF_User_Deleted]  DEFAULT ((0)) FOR [Deleted]
GO
/****** Object:  ForeignKey [FK_Comments_Account]    Script Date: 02/02/2010 10:48:57 ******/
ALTER TABLE [Articles].[Comments]  WITH CHECK ADD  CONSTRAINT [FK_Comments_Account] FOREIGN KEY([AuthorID])
REFERENCES [Auth].[Account] ([ID])
GO
ALTER TABLE [Articles].[Comments] CHECK CONSTRAINT [FK_Comments_Account]
GO
/****** Object:  ForeignKey [FK_Comments_Articles]    Script Date: 02/02/2010 10:48:57 ******/
ALTER TABLE [Articles].[Comments]  WITH CHECK ADD  CONSTRAINT [FK_Comments_Articles] FOREIGN KEY([ArticleID])
REFERENCES [Articles].[Articles] ([ArticleID])
ON DELETE CASCADE
GO
ALTER TABLE [Articles].[Comments] CHECK CONSTRAINT [FK_Comments_Articles]
GO
/****** Object:  ForeignKey [FK_Articles_Account]    Script Date: 02/02/2010 10:48:57 ******/
ALTER TABLE [Articles].[Articles]  WITH CHECK ADD  CONSTRAINT [FK_Articles_Account] FOREIGN KEY([AuthorID])
REFERENCES [Auth].[Account] ([ID])
ON DELETE CASCADE
GO
ALTER TABLE [Articles].[Articles] CHECK CONSTRAINT [FK_Articles_Account]
GO
/****** Object:  ForeignKey [FK_Attachments_Articles]    Script Date: 02/02/2010 10:48:57 ******/
ALTER TABLE [Articles].[Attachments]  WITH CHECK ADD  CONSTRAINT [FK_Attachments_Articles] FOREIGN KEY([ArticleID])
REFERENCES [Articles].[Articles] ([ArticleID])
ON DELETE CASCADE
GO
ALTER TABLE [Articles].[Attachments] CHECK CONSTRAINT [FK_Attachments_Articles]
GO
/****** Object:  ForeignKey [FK_Attachments_UserFiles]    Script Date: 02/02/2010 10:48:57 ******/
ALTER TABLE [Articles].[Attachments]  WITH CHECK ADD  CONSTRAINT [FK_Attachments_UserFiles] FOREIGN KEY([FileID])
REFERENCES [Articles].[UserFiles] ([FileID])
ON DELETE CASCADE
GO
ALTER TABLE [Articles].[Attachments] CHECK CONSTRAINT [FK_Attachments_UserFiles]
GO
/****** Object:  ForeignKey [FK_UserFiles_Account]    Script Date: 02/02/2010 10:48:57 ******/
ALTER TABLE [Articles].[UserFiles]  WITH CHECK ADD  CONSTRAINT [FK_UserFiles_Account] FOREIGN KEY([OwnerID])
REFERENCES [Auth].[Account] ([ID])
ON DELETE SET NULL
GO
ALTER TABLE [Articles].[UserFiles] CHECK CONSTRAINT [FK_UserFiles_Account]
GO
/****** Object:  ForeignKey [FK_AccountRole_Account]    Script Date: 02/02/2010 10:48:57 ******/
ALTER TABLE [Auth].[AccountRole]  WITH CHECK ADD  CONSTRAINT [FK_AccountRole_Account] FOREIGN KEY([Account])
REFERENCES [Auth].[Account] ([ID])
GO
ALTER TABLE [Auth].[AccountRole] CHECK CONSTRAINT [FK_AccountRole_Account]
GO
/****** Object:  ForeignKey [FK_AccountRole_Role]    Script Date: 02/02/2010 10:48:57 ******/
ALTER TABLE [Auth].[AccountRole]  WITH CHECK ADD  CONSTRAINT [FK_AccountRole_Role] FOREIGN KEY([Role])
REFERENCES [Auth].[Role] ([ID])
GO
ALTER TABLE [Auth].[AccountRole] CHECK CONSTRAINT [FK_AccountRole_Role]
GO
/****** Object:  ForeignKey [FK_PermissionRole_PermissionName]    Script Date: 02/02/2010 10:48:57 ******/
ALTER TABLE [Auth].[PermissionRole]  WITH CHECK ADD  CONSTRAINT [FK_PermissionRole_PermissionName] FOREIGN KEY([Permission])
REFERENCES [Auth].[Permission] ([Name])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [Auth].[PermissionRole] CHECK CONSTRAINT [FK_PermissionRole_PermissionName]
GO
/****** Object:  ForeignKey [FK_PermissionRole_RoleName]    Script Date: 02/02/2010 10:48:57 ******/
ALTER TABLE [Auth].[PermissionRole]  WITH CHECK ADD  CONSTRAINT [FK_PermissionRole_RoleName] FOREIGN KEY([Role])
REFERENCES [Auth].[Role] ([ID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [Auth].[PermissionRole] CHECK CONSTRAINT [FK_PermissionRole_RoleName]
GO
