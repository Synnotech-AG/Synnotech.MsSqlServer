CREATE TABLE Persons(
	Id INT IDENTITY(1, 1) CONSTRAINT PK_Persons PRIMARY KEY CLUSTERED,
	[Name] NVARCHAR(50) NOT NULL,
	[Age] INT NOT NULL
);

INSERT INTO Persons ([Name], Age)
VALUES ('John Doe', 42);

INSERT INTO Persons ([Name], Age)
VALUES ('Helga Orlowski', 29);

INSERT INTO Persons ([Name], Age)
VALUES ('Bruno Hitchens', 37);