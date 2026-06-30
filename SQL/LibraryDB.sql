/*
This script is intended to be cleanly re-runnable. I should be able to 
run it top to bottom and recreate my database in place - this is for demo purposes. 

Im going to break this script into sections, denoted by comments.
*/


IF DB_ID(N'LibraryDB') IS NULL
    CREATE DATABASE LibraryDB;
GO

-- At the top of my file, before any commands/statements. I want to make sure,
-- They run in the correct database.
USE LibraryDB;
GO -- GO is MS SQL Server specific - it is a batching statement. It's telling
-- the underlying SQL Server instance that is reading (running) this file 
-- "execute everything above this GO, and after any existing GO above, as one 
-- batch of statements."

/*

USE LibraryDB;
GO -- First batch 

CREATE TABLE AUTHOR{ }
CREATE TABLE BOOK { }
...
GO -- second batch 

... and so on and so forth

*/
-- ===================================================
-- Section 1 - DDL - Defining our Database
-- CREATE - Creating new tables, schemas, databases: NOT INDIVIDUAL ROWS
-- DROP - Deletes a table, or schema, or database entirely. 
-- TRUNCATE - Deletes all data in a table - preserves structure (columns + constraints)
-- ALTER - Used to edit the structure of an existing table (add columns, tweak constraints, etc)
-- ===================================================
-- I will come back once we've created out tables, and include a DROP section
DROP TABLE IF EXISTS dbo.Loan;
DROP TABLE IF EXISTS dbo.Book;
DROP TABLE IF EXISTS dbo.Member;
DROP TABLE IF EXISTS dbo.Author;
GO


-- The first thing I want to do, is create my tables. 
-- Table names are technically pre-pended by the schema
-- We have a schema already - MS SQL Server creates a default schema called "dbo"
-- "Database Owner". If I create Author without specifying a schema, SQL Server
-- associates it with this default dbo schema. So the full table name becomes:
-- dbo.Author
CREATE TABLE dbo.Author
(   -- Column-name data-type constraints (optional)
    -- In MS SQL Server, identity lets us define a PK that automatically increments
    AuthorId INT IDENTITY(1,1) NOT NULL,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    BirthYear INT NULL, -- NULL here signifies that we INTEND for this to maybe be null

    -- After I define my columns, datatypes and basic constraints 
    -- I can optionally add some named constraints. If I don't name constraints,
    -- nothing breaks BUT I can make my life easier and make error messages more 
    -- functional/readable by explicitly naming my constraints
    CONSTRAINT PK_Author PRIMARY KEY (AuthorId),

    -- When someone tries to add an Author, make sure that BirthYear is either NULL, OR between 300 and 2050
    CONSTRAINT CK_Author_BirthYear CHECK (BirthYear IS NULL OR BirthYear BETWEEN 300 AND 2050)
    -- If you THINK that you might need to alter a table's constraints,
    -- you should name them. It makes running ALTER TABLE commands easier later
);
GO -- including my GO batch statement for MS SQL Server
-- SELECT * FROM dbo.Author;
USE LibraryDB;
CREATE TABLE dbo.Member 
(   
    -- This is perhaps faster but not best practice. For example, lets say I want to change from MemberID as my PK
    -- to email as my PK. By doing an in-line non-named constraint, I've shot myself in the foot. It is much harder
    -- to ALTER our table later on, to play with the constraint. 
    MemberId INT IDENTITY(1,1) NOT NULL PRIMARY KEY, -- Fun fact, Identity will not reuse any numbers even if deleted
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Email VARCHAR(125) NOT NULL UNIQUE,
    -- Using a DEFAULT constraint, if no value is provided, the built in GETDATE() function gets a value to satisfy the column
    JoinedDate DATE NOT NULL DEFAULT (GETDATE())
);
GO 

-- Book is our largest table so far
USE LibraryDB;
CREATE TABLE dbo.Book
(
    -- Columns + some constraints
    BookId INT IDENTITY(1,1) NOT NULL,
    Title VARCHAR(200) NOT NULL, --UNIQUE
    ISBN CHAR(13) NOT NULL,
    PublishedYear INT NULL,
    -- Creating a named constraint inline, useful for the constraint code isn't super long
    CategoryName VARCHAR(60) NOT NULL CONSTRAINT DF_Book_CategoryName DEFAULT ('General'),
    AuthorId INT NOT NULL, -- This will be a foreign key, we'll set the FK constraint below
    TotalCopies INT NOT NULL CONSTRAINT DF_Book_TotalCopies DEFAULT(1),
    AvailableCopies INT NOT NULL CONSTRAINT DF_Book_AvailableCopies DEFAULT(1),
    -- More named constraints below
    CONSTRAINT PK_Book PRIMARY KEY (BookId),
    CONSTRAINT UQ_Book_ISBN UNIQUE (ISBN),

    -- Setting our first Foreign Key constraint 
    -- We need to tell the SQL engine, what column in this table is getting the FK constraint
    -- as well as what column in another existing table that FK points to
    -- When I set a FK constraint, I can optionally set the delete behavior via ON DELETE
    -- CASCADE - VERY RISKY - If an author is deleted, all their books go too.
    -- SET NULL - Low risk - If an author is deleted, this AuthorId in Book is set to null. (requires a nullable column)
    -- RESTRICT - SAFE - Default behavior, blocks deletion of an author if any books reference it
    -- NO ACTION - SAFE - Same as restrict in MS SQL Server
    -- SET DEFAULT - Low risk - Requires a default value constraint, will set the FK column to that value if the author is deleted
    CONSTRAINT FK_Book_Author FOREIGN KEY (AuthorId) REFERENCES dbo.Author (AuthorId) ON DELETE CASCADE,

    -- The final thing I want to do is enforce some logical rules about Available and Total copies
    -- AvailableCopies CANNOT exceed TotalCopies
    CONSTRAINT CK_Book_Copies CHECK (TotalCopies >= AvailableCopies)
)
GO

-- Loan -- two foreign keys
-- Represents the library loaning a book to a member
USE LibraryDB;
CREATE TABLE dbo.Loan
(
    LoanId INT IDENTITY(1,1) NOT NULL, -- PK
    BookId INT NOT NULL, -- FK
    MemberId INT NOT NULL, -- FK 
    -- Date stamp for when the book was lent to the member
    LoanDate DATE NOT NULL CONSTRAINT DF_Loan_LoanDate DEFAULT(GETDATE()),
    DueDate DATE NOT NULL,
    ReturnDate DATE NULL, -- This will remain NULL until the book is actually returned

    -- More named constraints below
    CONSTRAINT PK_Loan PRIMARY KEY (LoanId),
    -- Note: Technically, FK columns don't have to match the column in the table they are a PK in. 
    CONSTRAINT FK_Loan_Book FOREIGN KEY (BookId) REFERENCES dbo.Book (BookId),
    CONSTRAINT FK_Loan_Member FOREIGN KEY (MemberId) REFERENCES dbo.Member (MemberId),
    CONSTRAINT CK_Loan_Dates CHECK (DueDate >= LoanDate) -- DueDate has to be in the future
);
GO 

-- Now that I've created the tables, using CREATE (DDL), how can I edit the tables themselves?

-- Let's add a column to an existing table, lets use dbo.Book
-- We can set constraints for this new column in line as well
ALTER TABLE dbo.Book ADD Edition INT NOT NULL CONSTRAINT DF_Book_Edition DEFAULT (1);

-- We can get more granular, and not just add a new column, we can edit things about existing columns
-- I can use this ALTER TABLE + ALTER COLUMN to add or edit constraints 
ALTER TABLE dbo.Book ALTER COLUMN Title VARCHAR(250) NOT NULL;
GO

-- Ideally, we would never have to ALTER stuff. When possible, do it in CREATE. In the real world, 
-- you will need to ALTER things about the tables in a schema. Once you have data in a table, you're
-- stuck ALTER-ing it. Prior to giving a table any data, it is often easier to drop the table and
-- edit the CREATE statement for it. 

-- DROP and Truncate - please learn the difference
-- DROP: removes a table entirely. Data is lost, the structure is also gone. Like it never existed.
-- DROP TABLE dbo.Loan;

-- TRUNCATE: removes all the data (rows) in table, leaves behind the structure.
-- TRUNCATE TABLE dbo.Loan;

/*
========================================================
Section 2: DML + DQL - Reading and Writing (CRUD)
-- DML - Data Manipulation Language - Used for affecting rows in a table
-- INSERT - Used to insert new rows in an existing table
-- UPDATE - Used to update an existing row's information
-- DELETE - Used to remove a row

-- DQL - Data Query Language - Selecting rows. 
-- SELECT - Used to select a record or records. (This is where other SQL
-- keywords like GROUP BY, HAVING. WHERE, etc live.)
========================================================
*/ 

-- DML first - let's seed our database

-- Single row insertion. It is best practice - borderline mandatory - to explicitly list the columns 
-- you are inserting into 
INSERT INTO dbo.Author (FirstName, LastName, BirthYear) 
VALUES ('Robert', 'Martin', 1952);

-- We can also do a multi-row insert
INSERT INTO dbo.Author (FirstName, LastName, BirthYear) VALUES
    ('Martin', 'Fowler', 1963),
    ('Frank', 'Herbert', 1920),
    ('Kent', 'Beck', 1961); -- Final row in a multi-insert gets the semi-colon

-- SELECT every row from author to check our work
SELECT * FROM dbo.Author;

-- Now that we have authors, we can add books.
INSERT INTO dbo.Book (Title, ISBN, PublishedYear, CategoryName, AuthorId,
                             TotalCopies, AvailableCopies, Edition)
VALUES ('Clean Code', '9780132350885', 2008, 'Software', 1, 3, 3, 1);

SELECT * FROM dbo.Book;
GO 

INSERT INTO dbo.Author (FirstName, LastName, BirthYear) VALUES
    ('Robert',  'Martin',   1952),   -- 1
    ('Martin',  'Fowler',   1963),   -- 2
    ('Kent',    'Beck',     1961),   -- 3
    ('Erich',   'Gamma',    1961),   -- 4
    ('Andrew',  'Hunt',     1964),   -- 5
    ('David',   'Thomas',   1956);   -- 6
GO

INSERT INTO dbo.Member (FirstName, LastName, Email, JoinedDate) VALUES
    ('Ada',     'Lovelace', 'ada@example.com',     '2025-01-10'),  -- 1
    ('Grace',   'Hopper',   'grace@example.com',   '2025-02-02'),  -- 2
    ('Alan',    'Turing',   'alan@example.com',    '2025-02-20'),  -- 3
    ('Linus',   'Torvalds', 'linus@example.com',   '2025-03-15'),  -- 4
    ('Margaret','Hamilton', 'margaret@example.com','2025-04-01'),  -- 5
    ('Dennis',  'Ritchie',  'dennis@example.com',  '2025-05-05');  -- 6
GO

INSERT INTO dbo.Book (Title, ISBN, PublishedYear, CategoryName, AuthorId, TotalCopies, AvailableCopies, Edition) VALUES
    ('Clean Architecture',                 '9780134494166', 2017, 'Software',            1, 2, 2, 1),
    ('Refactoring',                        '9780201485677', 1999, 'Software',      2, 2, 1, 2),
    ('Patterns of Enterprise Application Architecture','9780321127426',2002,'Software', 2, 1, 1, 1),
    ('Test Driven Development',            '9780321146533', 2002, 'Testing',         3, 2, 2, 1),
    ('Extreme Programming Explained',      '9780321278654', 2004, 'Process',           3, 1, 0, 2),
    ('Design Patterns',                    '9780201633610', 1994, 'Software',          4, 2, 2, 1),
    ('The Pragmatic Programmer',           '9780201616224', 1999, 'Software',           5, 4, 3, 1),
    ('The Pragmatic Programmer 20th Anniv','9780135957059', 2019, 'Software',            5, 2, 2, 2),
    ('Programming Ruby',                   '9780974514055', 2004, 'Languages',           6, 1, 1, 1);
GO

INSERT INTO dbo.Loan (BookId, MemberId, DueDate, ReturnDate) VALUES
    (6, 1, '2026-06-30', NULL);


SELECT * FROM dbo.Author;
SELECT * FROM dbo.Member;
SELECT * FROM dbo.Book;

-- We have some data in our DB - lets do some UPDATE 
-- lets grab a book and give it a new Edition number
UPDATE dbo.Book
SET Edition = 2
WHERE BookId = 3; -- If I leave this off, EVERY ROW gets that new value.

-- I can also do calculations based on existing values inside the SET area
UPDATE dbo.Book
SET AvailableCopies = AvailableCopies - 1 -- removing a copy from circulation entirely
WHERE BookId = 1;

-- Lets remove a row 
-- Same general rules as UPDATE - if you don't include a WHERE you have truncated the table
DELETE FROM dbo.Member 
WHERE Email = 'someone@nowhere.com'; -- don't forget the WHERE

DELETE FROM dbo.Member 
WHERE Email = 'dennis@example.com'; 

DELETE FROM dbo.Author 
WHERE AuthorId = 1;

SELECT * FROM dbo.Author;

GO
-- DELETE FROM dbo.Book
-- WHERE BookId = 6;

-- DQL - SELECT to return data
SELECT * FROM dbo.Book; -- The simplest select

-- SELECT for specific columns
SELECT Title, PublishedYear, AvailableCopies FROM dbo.Book;

-- SELECT with a computed column, aliased with AS
SELECT Title, TotalCopies - AvailableCopies AS CopiesOut FROM dbo.Book;



-- Getting back everything from a table is fine, for our training. Usually, we want to be 
-- more specific. 
SELECT Title, PublishedYear
FROM dbo.Book
WHERE PublishedYear >= 2000; -- Using WHERE as a filter. 

-- I can use things like BETWEEN, LIKE, and IS combined with my WHERE 
-- to provide more complex/precise filtering logic

-- I want just the title from every book published between 1999 and 2004
SELECT Title
FROM dbo.Book
WHERE PublishedYear
BETWEEN 1999 AND 2004;

-- I want title, categoryname from every book who's category name is either software or testing
SELECT Title, CategoryName
FROM dbo.Book
WHERE CategoryName
IN ('Software', 'testing'); -- By default, many SQL RDBMS systems are case-insensitive for comparisons
-- They render case, and when you return a value back to say a C# program, case is preserved. BUT
-- when doing comparisons on the DB 'testing' = 'Testing' UNLESS we change the collation setting during Server creation

-- I want every book title where the title starts with "Test"
SELECT Title 
FROM dbo.Book
WHERE Title
LIKE 'Test%';

-- Last SELECT in this section 
-- Give me every book title where the category is software AND available copies is greater than 1
SELECT Title
FROM dbo.Book
WHERE CategoryName = 'Software' AND AvailableCopies > 1;

-- Give me every book title where the PublishedYear was not provided (null)
SELECT Title
FROM dbo.Book
WHERE PublishedYear IS NULL; -- If we are trying to do a comparison to assert that something
-- is null, we don't use = . In SQL null doesnt equal anything. Its unknown, the absence of a value. 

-- LIKE vs IN vs =
-- = - matches one exact value
-- IN - matches any value in the provided list
-- LIKE - matches some pattern with wildcards %

-- ORDER BY and DISTINCT
-- We probably want to be able to order the returned records based on some logic
-- atleast sometimes

SELECT Title, PublishedYear
FROM dbo.Book
ORDER BY PublishedYear DESC, Title; -- By default, SQL order by ASC (if omitted, its ASC)

-- Using Distinct
-- Give me all the distinct category names that appear in dbo.Book
SELECT DISTINCT CategoryName
FROM dbo.Book
ORDER BY CategoryName; -- ASC

-- ORDER BY - sorts the output, by default in Ascending order. You can order by multiple keys,
-- it uses subsequent keys to sort within some category.

-- DISTINCT - removes duplicates from the result set. 

-- GROUP BY and HAVING - a preview
-- We are definitely coming back to this later this week.

-- Give me the category name, and the count of books in that category
-- where the count is more than 2. Order the results by book count descending
SELECT CategoryName, COUNT(*) AS BookCount
FROM dbo.Book
GROUP BY CategoryName
HAVING COUNT(*) > 2 -- I can't use an alias name in HAVING, either a column that exists, or some function
ORDER BY BookCount DESC;

GO

-- GROUP BY CategoryName - Collapses all rows within that category into one group
-- COUNT(*) - an aggregate function that counts the rows in each group
-- We get back one line per categoryName, with the number of books in with that CategoryName

-- GROUP BY vs DISTINCT
-- DISTINCT is just straight de-duping. 
-- GROUP BY lets you run computation against the groups. Count how many per group for example. 

-- HAVING vs WHERE
-- HAVING filters groups in a GROUP BY
-- WHERE filters rows.

-- If I have a SELECT that blends WHERE, GROUP BY and HAVING 
-- WHERE runs before any grouping, and filters the raw rows that are then passed 
-- to GROUP BY, then HAVING filters the groups. 

-- ---- DROP: children before parents ----------------------------------------------
DROP TABLE IF EXISTS dbo.Loan;
DROP TABLE IF EXISTS dbo.BookAuthor;
DROP TABLE IF EXISTS dbo.Book;
DROP TABLE IF EXISTS dbo.Member;
DROP TABLE IF EXISTS dbo.Author;
DROP TABLE IF EXISTS dbo.Category;
GO

-- ---- CREATE normalized: parents before children ---------------------------------
-- NEW: Category is now its own entity (was the free-text Book.CategoryName).
CREATE TABLE dbo.Category
(
    CategoryId  INT IDENTITY(1,1) NOT NULL,
    Name        VARCHAR(60)  NOT NULL,
    Description VARCHAR(200) NULL,
    CONSTRAINT PK_Category PRIMARY KEY (CategoryId),
    CONSTRAINT UQ_Category_Name UNIQUE (Name)
);

CREATE TABLE dbo.Author
(
    AuthorId  INT IDENTITY(1,1) NOT NULL,
    FirstName VARCHAR(50) NOT NULL,
    LastName  VARCHAR(50) NOT NULL,
    BirthYear INT NULL,
    CONSTRAINT PK_Author PRIMARY KEY (AuthorId),
    CONSTRAINT CK_Author_BirthYear CHECK (BirthYear IS NULL OR BirthYear BETWEEN 300 AND 2050)
);

CREATE TABLE dbo.Member
(
    MemberId   INT IDENTITY(1,1) NOT NULL,
    FirstName  VARCHAR(50)  NOT NULL,
    LastName   VARCHAR(50)  NOT NULL,
    Email      VARCHAR(125) NOT NULL,
    JoinedDate DATE NOT NULL CONSTRAINT DF_Member_JoinedDate DEFAULT (GETDATE()),
    CONSTRAINT PK_Member PRIMARY KEY (MemberId),
    CONSTRAINT UQ_Member_Email UNIQUE (Email)
);

-- CHANGED: Book now carries CategoryId (FK) instead of CategoryName, and has NO AuthorId
-- (authorship lives in the BookAuthor bridge). Edition and the wider Title are folded into
-- CREATE rather than added later with ALTER.
CREATE TABLE dbo.Book
(
    BookId          INT IDENTITY(1,1) NOT NULL,
    Title           VARCHAR(250) NOT NULL,
    ISBN            CHAR(13) NOT NULL,
    PublishedYear   INT NULL,
    CategoryId      INT NOT NULL,
    TotalCopies     INT NOT NULL CONSTRAINT DF_Book_TotalCopies     DEFAULT (1),
    AvailableCopies INT NOT NULL CONSTRAINT DF_Book_AvailableCopies DEFAULT (1),
    Edition         INT NOT NULL CONSTRAINT DF_Book_Edition         DEFAULT (1),
    CONSTRAINT PK_Book PRIMARY KEY (BookId),
    CONSTRAINT UQ_Book_ISBN UNIQUE (ISBN),
    CONSTRAINT CK_Book_Copies CHECK (TotalCopies >= AvailableCopies),
    CONSTRAINT FK_Book_Category FOREIGN KEY (CategoryId) REFERENCES dbo.Category (CategoryId)
);

-- NEW: the M:N bridge between Book and Author. Composite PK, no extra attributes.
CREATE TABLE dbo.BookAuthor
(
    BookId   INT NOT NULL,
    AuthorId INT NOT NULL,
    CONSTRAINT PK_BookAuthor PRIMARY KEY (BookId, AuthorId),
    CONSTRAINT FK_BookAuthor_Book   FOREIGN KEY (BookId)   REFERENCES dbo.Book   (BookId)   ON DELETE CASCADE,
    CONSTRAINT FK_BookAuthor_Author FOREIGN KEY (AuthorId) REFERENCES dbo.Author (AuthorId) ON DELETE CASCADE
);

CREATE TABLE dbo.Loan
(
    LoanId     INT IDENTITY(1,1) NOT NULL,
    BookId     INT NOT NULL,
    MemberId   INT NOT NULL,
    LoanDate   DATE NOT NULL CONSTRAINT DF_Loan_LoanDate DEFAULT (GETDATE()),
    DueDate    DATE NOT NULL,
    ReturnDate DATE NULL,
    CONSTRAINT PK_Loan PRIMARY KEY (LoanId),
    CONSTRAINT FK_Loan_Book   FOREIGN KEY (BookId)   REFERENCES dbo.Book   (BookId),
    CONSTRAINT FK_Loan_Member FOREIGN KEY (MemberId) REFERENCES dbo.Member (MemberId),
    CONSTRAINT CK_Loan_Dates  CHECK (DueDate >= LoanDate)
);
GO


-- ---- SEED normalized: parents before children -----------------------------------
-- Category rows are seeded directly (no SELECT DISTINCT off Book, since Book no longer
-- holds the name). Comments mark the IDENTITY value each row receives.
INSERT INTO dbo.Category (Name, Description) VALUES
    ('Software',  'Software design and craftsmanship'),  -- 1
    ('Testing',   'Testing and TDD'),                    -- 2
    ('Process',   'Process and methodology'),            -- 3
    ('Languages', 'Programming languages');              -- 4

INSERT INTO dbo.Author (FirstName, LastName, BirthYear) VALUES
    ('Robert', 'Martin', 1952),   -- 1
    ('Martin', 'Fowler', 1963),   -- 2
    ('Kent',   'Beck',   1961),   -- 3
    ('Erich',  'Gamma',  1961),   -- 4
    ('Andrew', 'Hunt',   1964),   -- 5
    ('David',  'Thomas', 1956);   -- 6

INSERT INTO dbo.Member (FirstName, LastName, Email, JoinedDate) VALUES
    ('Ada',     'Lovelace', 'ada@example.com',      '2025-01-10'),  -- 1
    ('Grace',   'Hopper',   'grace@example.com',    '2025-02-02'),  -- 2
    ('Alan',    'Turing',   'alan@example.com',     '2025-02-20'),  -- 3
    ('Linus',   'Torvalds', 'linus@example.com',    '2025-03-15'),  -- 4
    ('Margaret','Hamilton', 'margaret@example.com', '2025-04-01'),  -- 5
    ('Dennis',  'Ritchie',  'dennis@example.com',   '2025-05-05');  -- 6

-- Book references CategoryId (1=Software, 2=Testing, 3=Process, 4=Languages); no author column.
INSERT INTO dbo.Book (Title, ISBN, PublishedYear, CategoryId, TotalCopies, AvailableCopies, Edition) VALUES
    ('Clean Code',                                     '9780132350885', 2008, 1, 3, 3, 1),  -- 1
    ('Clean Architecture',                             '9780134494166', 2017, 1, 2, 2, 1),  -- 2
    ('Refactoring',                                    '9780201485677', 1999, 1, 2, 1, 2),  -- 3
    ('Patterns of Enterprise Application Architecture','9780321127426', 2002, 1, 1, 1, 1),  -- 4
    ('Test Driven Development',                        '9780321146533', 2002, 2, 2, 2, 1),  -- 5
    ('Extreme Programming Explained',                  '9780321278654', 2004, 3, 1, 0, 2),  -- 6
    ('Design Patterns',                                '9780201633610', 1994, 1, 2, 2, 1),  -- 7
    ('The Pragmatic Programmer',                       '9780201616224', 1999, 1, 4, 3, 1),  -- 8
    ('The Pragmatic Programmer 20th Anniv',            '9780135957059', 2019, 1, 2, 2, 2),  -- 9
    ('Programming Ruby',                               '9780974514055', 2004, 4, 1, 1, 1);  -- 10

-- Authorship via the bridge: the original one-author-per-book links, then real co-authors.
INSERT INTO dbo.BookAuthor (BookId, AuthorId) VALUES
    (1, 1), (2, 1), (3, 2), (4, 2), (5, 3),     -- original single-author links
    (6, 3), (7, 4), (8, 5), (9, 5), (10, 6),
    (7, 3),   -- Design Patterns co-authored (Beck)
    (8, 6);   -- The Pragmatic Programmer co-authored (Thomas)

INSERT INTO dbo.Loan (BookId, MemberId, LoanDate, DueDate, ReturnDate) VALUES
    (3, 1, '2026-06-01', '2026-06-15', NULL),         -- Refactoring, out to Ada
    (6, 2, '2026-05-20', '2026-06-03', NULL),         -- XP Explained, out to Grace (0 available)
    (8, 3, '2026-05-25', '2026-06-08', '2026-06-04'), -- Pragmatic Programmer, returned
    (1, 4, '2026-06-10', '2026-06-24', NULL),         -- Clean Code, out to Linus
    (8, 5, '2026-06-12', '2026-06-26', NULL);         -- Pragmatic Programmer, out to Margaret
GO

-- JOINS and intermediate DQL -- 

-- Aggregate functions
-- An aggregate collapses many rows into one sumber
-- COUNT() - 4 - COUNT(*) is different from COUNT(some_column) - when you COUNT() a specific column, NULLS are ignored
-- SUM() - get the sum total stored in a column across many rows
-- AVG() - get the average value stored in a column across many rows - skip nulls 
-- MIN(), MAX()

SELECT COUNT(*) AS BookCount, SUM(TotalCopies) AS TotalCopies, 
        AVG(TotalCopies) AS AvgCopies, MIN(PublishedYear) AS Oldest, 
        MAX(PublishedYear) AS Newest 
FROM dbo.Book;

-- Scalar functions - transform a value into a new value, per row. 
SELECT UPPER(LastName) AS LastUpper,
        LEN(Email) AS EmailLen,
        CONCAT(FirstName, ' ', LastName) AS FullName,
        DATEDIFF(DAY, JoinedDate, GETDATE()) AS DaysAMember-- Takes 3 arguments, the datepart (year difference, day difference, etc)
FROM dbo.Member;                                    -- start date, end date


-- SQL Joins --
-- JOINs - JOINs are one way to get information from multiple tables in the same query.
-- INNER JOIN (the default join)
-- LEFT and RIGHT JOINs
-- OUTER JOINs (LEFT, RIGHT, FULL)
-- CROSS JOINs

-- books with their categories (FK = PK)
-- This is an example of an equi-join - we join on an equality comparison
SELECT b.Title, c.Name AS Category
FROM dbo.Book AS b
INNER JOIN dbo.Category AS c ON c.CategoryId = b.CategoryId -- the join condition 
ORDER BY c.Name, b.Title;

-- Lets do a join across a many to many
-- I want stuff from Authors and Books - I need to traverse BookAuthor

-- books with ALL their authors, through the join table
SELECT b.Title, a.FirstName + ' ' + a.LastName AS Author
FROM dbo.Book AS b
JOIN dbo.BookAuthor ba ON ba.BookId = b.BookId
JOIN dbo.Author a ON a.AuthorId = ba.AuthorId
ORDER BY b.Title, Author;


-- GROUP BY + HAVING with JOINs
-- I want the name, total books in, and total copies across all books in: A category
SELECT c.Name AS Category, COUNT(*) AS Books, SUM(b.TotalCopies) AS Copies
FROM dbo.Book b
JOIN dbo.Category c ON c.CategoryId = b.CategoryId
GROUP BY c.Name
HAVING COUNT(*) > 0
ORDER BY Books DESC;

-- GROUP BY returns one row in the result PER GROUP
-- Aggregates collapse all rows in a group into a single value
-- A bare column, would have many possible values per group. 

-- LEFT and RIGHT joins 

-- LEFT JOIN - we want all records from the left table, and matching records from the right
-- every member and their loans, if they have any. Members with no loans will still appear
SELECT m.FirstName, m.LastName, l.LoanId, l.DueDate
FROM dbo.Member AS m -- left table
LEFT JOIN dbo.Loan AS l ON l.MemberId = m.MemberId -- right table
ORDER BY m.LastName;

-- These can be useful for filtering based on those nulls
SELECT m.FirstName, m.LastName
FROM dbo.Member AS m 
LEFT JOIN dbo.Loan AS l ON l.MemberId = m.MemberId
WHERE l.LoanId IS NULL;

-- RIGHT JOIN: mirror of left join
-- every book and their loans if it exists
SELECT b.Title, l.LoanId
FROM dbo.Loan AS l   
RIGHT JOIN dbo.Book AS b ON b.BookId = l.BookId
ORDER BY b.Title;

-- FULL OUTER vs CROSS 
-- FULL OUTER JOIN - Returns matched rows where they exist, as well as unmatched rows
SELECT b.Title, c.Name AS Category
FROM dbo.Book b
FULL OUTER JOIN dbo.Category c ON c.CategoryId = b.CategoryId
ORDER BY c.Name;

-- CROSS JOIN - not common. Cartesian product
-- Every possible combination of the rows in both tables
-- very rare
SELECT a.LastName, c.Name
FROM dbo.Author a
CROSS JOIN dbo.Category c; 


-- Subqueries
-- A subquery is a query inside another query
-- Use a join to combine columns from multiple tables into the output
-- Use a subquery to filter against a computer value or set you DONT need in the output
-- Realistically, you can usually use either - typically the JOIN will be easier to write

-- scalar subquery: books that have more copies than average
SELECT Title, TotalCopies
FROM dbo.Book
WHERE TotalCopies > ( SELECT AVG(TotalCopies) FROM dbo.Book );

-- IN-subquery: members who currently have a book lent out
-- Notice - we did the opposite earlier with a JOIN, this could also 
-- simply be a join
SELECT FirstName, LastName
FROM dbo.Member
WHERE MemberId IN
(
    SELECT MemberId
    FROM dbo.Loan
    WHERE ReturnDate IS NULL
);

-- correlated subquery: this one can cause you issues.
SELECT Title, TotalCopies
FROM dbo.Book b1 
WHERE TotalCopies > (
    SELECT AVG(TotalCopies)
    FROM dbo.Book b2
    WHERE b1.PublishedYear = b2.PublishedYear
)

-- correlated subquery: loan count per book - computed by row
SELECT b.Title,
        (SELECT COUNT(*) FROM dbo.Loan l WHERE l.BookId = b.BookId) AS TimesLoaned
FROM dbo.Book b
ORDER BY TimesLoaned DESC;

-- Lets make a dashboard 
-- I want every currently out loan (loans with a null), with member, book, category, and how late the book is
SELECT m.FirstName + ' ' + m.LastName AS Member,
        b.Title,
        c.Name as Category,
        l.DueDate,
        DATEDIFF(DAY, l.DueDate, GETDATE()) AS DaysOverdue
FROM dbo.Loan AS l    
JOIN dbo.Member AS m ON m.MemberId = l.MemberId
JOIN dbo.Book AS b ON b.BookId = l.BookId
JOIN dbo.Category AS c ON c.CategoryId = b.CategoryId
WHERE l.ReturnDate IS NULL 
ORDER BY DaysOverdue DESC;

GO
-- Transactions - All of this runs or NONE of it succeeds
-- At its most basic a Transaction is just BEGIN and COMMIT
-- BUT we want to account for things going wrong gracefull - even if 
-- an error or exception WONT bring the database down.

-- SQL despite not being a programming language - has TRY and CATCH

-- SQL Server is an application. It, like any other applications, can have runtime errors
-- This flag below, causes SQL Server to abort any transaction when an error surfaces. 
-- Put it right before your try catch - this is SQL Server Specific
SET XACT_ABORT ON; -- SQL Server specific flag 

BEGIN TRY -- A little verbose but it works. 
    BEGIN TRANSACTION -- we put our transaction in here. 
        -- The logic of our transaction goes here below

        -- Member 2 checks out book 1
        INSERT INTO dbo.Loan(BookId, MemberId, DueDate)
        VALUES (6, 2, DATEADD(DAY, 14, GETDATE())) -- Using Dateadd to generate a due date

        -- So we did an insert - now let's do an update
        -- Book 1 Available copies decremented
        UPDATE dbo.Book SET AvailableCopies = AvailableCopies - 1 WHERE BookId = 6;
    COMMIT TRANSACTION -- If we make it here, both writes become persisted
    PRINT 'Checkout COMMITTED'
END TRY
BEGIN CATCH 
    -- If something goes wrong we can detect it and roll back 
    -- IF we make it to the CATCH, something has gone wrong. Either a runtime error
    -- OR some sort of constraint or data integrity violation, like trying to create orphan records
    -- The transaction remains open on this connection - we check to see if by the time we hit the catch
    -- any transactions ARE open. If there are, we want to abort them - roll them back. 
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'Checkout ROLLED BACK: ' + ERROR_MESSAGE();
END CATCH

-- Every transaction ends in either a COMMIT or a ROLLBACK - you must provide logic for both.
-- If you have a bunch of branching paths keep that in mind. 

-- Isolation levels - largely managed by the RDBMS that runs our Database
-- but we have some options. We can, if we want to, 
-- set the isolation level

-- In SQL Server there are four options (that we will worry about): 
-- READ UNCOMMITTED - Queries and statements can read data rows modified by other transactions
    -- that are yet to be committed. Does not issue shared locks or honor exclusive locks.
    -- Can accidentally result in dirty (untrue) reads, and phantom reads on records that wont exist 
    -- once that other transaction finishes.
-- READ COMMITTED (Default) - Prevents dirty reads by requiring that data read by a statement must be 
    -- committed before it is processed. 
-- REPEATABLE READ - Places shared locks on all data read by transaction and HOLDS THEM until the entire transaction
    -- No other transaction can modify or delete these rows during this time. Prevents some dirty reads, 
    -- BUT other transactions can insert new rows that may bleed into your filtering for queries 
-- SERIALIZABLE -- Most restrictive. Places locks on rows that prevent other transactions from updating
    -- deleting OR inserting in date within the read range until your transaction finishes. This
    -- prevents ALL concurrency anomalies - and is also slow as hell. SQL Server has to create
    -- manage and delete alot of locks. You can also create deadlocks. 

-- Fixing my error from before + constraint fix

-- Reset copies
UPDATE dbo.Book SET AvailableCopies = 0 WHERE BookId = 6;

-- Add constraint
ALTER TABLE dbo.Book
ADD CONSTRAINT CK_Book_Min_AvailableCopies
CHECK (AvailableCopies >= 0);

GO

-- VIEWS, INDEXES, STORED PROCEDURES and more. 
-- Views are like saved queries. Think back to that large triple join we did 
-- on DQL day. We can take something like that, that is annoying write or think of over and over again
-- and we can save it as a View. Then we can query the view as if it was a table. 

CREATE OR ALTER VIEW dbo.vw_ActiveLoans
AS
    SELECT m.FirstName + ' ' + m.LastName AS Member,
        b.Title,
        c.Name as Category,
        l.DueDate,
        DATEDIFF(DAY, l.DueDate, GETDATE()) AS DaysOverdue
    FROM dbo.Loan AS l    
    JOIN dbo.Member AS m ON m.MemberId = l.MemberId
    JOIN dbo.Book AS b ON b.BookId = l.BookId
    JOIN dbo.Category AS c ON c.CategoryId = b.CategoryId
    WHERE l.ReturnDate IS NULL;

GO

-- This is not a saved precompiled dataset. There is something called an indexed view
-- that is outside of our scope that is precompiled. But a VIEW like the one above, a regular VIEW
-- is just a saved select. The data updates to be current whenever you call upon it. 
SELECT * FROM dbo.vw_ActiveLoans ORDER BY DueDate;

GO

-- Increasing in complexity and potential usefulness
-- We have Stored procedures - a named program in the database. Parameters/arguments, logic, 
-- transaction wrapping and limited/optional returns 
CREATE OR ALTER PROCEDURE dbo.usp_CheckoutBook
    -- This first section between CREATE and AS holds your arguments/inputs
    @BookId INT,
    @MemberId INT,
    @Days INT = 14 -- how long is the loan for, default 14
AS
BEGIN
    SET XACT_ABORT ON;
    SET NOCOUNT ON; -- turns off the "x rows affected message print"

    BEGIN TRY
        BEGIN TRANSACTION
            -- We want to check if there are copies available to be checked out
            IF (SELECT AvailableCopies FROM dbo.Book WHERE BookId = @BookId ) <= 0
                -- Manually throwing an error in SQL Server. 
                -- THROW takes 3 arguments
                    -- error_number - some integer representing the exception, must be 50000 or higher
                    -- message - some string describing the error
                    -- state - an int between 0 and 255 describing where the error originated
                THROW 50000, 'No copies available to check out.', 1;

            INSERT INTO dbo.Loan (BookId, MemberId, DueDate)
            VALUES (@BookId, @MemberId, DATEADD(DAY, @Days, GETDATE()));

            UPDATE dbo.Book SET AvailableCopies = AvailableCopies - 1 WHERE BookId = @BookId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW; -- bubbling up the Try block error to wherever my stored procedure was called
    END CATCH
END;
GO

EXEC dbo.usp_CheckoutBook @BookId = 1, @MemberId = 3, @Days = 21;

SELECT * FROM dbo.Loan WHERE BookId = 1 AND MemberId = 3;

GO
-- Stored Procedure vs User Defined Function
-- A stored procedure DOES something. Writes, transactions, returning result sets, etc. Called with EXEC.
-- A function (user defined or otherwise) computes a value. Its called INSIDE a query, and it itself
-- does not change the data in the database. 

-- User defined function
CREATE OR ALTER FUNCTION dbo.fn_DaysOverdue (@dueDate DATE)
RETURNS INT
AS 
BEGIN
    -- Declaring a variable in SQL that is scoped to this function
    DECLARE @days INT = DATEDIFF(DAY, @dueDate, CAST(GETDATE() AS DATE));
    -- IF days is greater than 0 (the book is overdue) - return that value
    -- IF days is 0 (or less somehow) return 0 instead
    RETURN CASE WHEN @days > 0 THEN @days ELSE 0 END;
END;
GO

SELECT b.Title, l.DueDate, dbo.fn_DaysOverdue(DueDate) AS DaysOverdue
FROM dbo.Loan l
JOIN dbo.Book b ON b.BookId = l.BookId
WHERE ReturnDate is NULL;


-- Indexes - a lookup structure like an index ina  book. 
-- You create it on certain columns in a table, to allow lookup on that column
-- without the DB engine having to scan row by row. 

-- Trading READ speed for WRITE speed
CREATE INDEX IX_Loan_MemberId ON dbo.Loan (MemberId);