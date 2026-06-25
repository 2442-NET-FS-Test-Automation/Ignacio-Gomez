-- Parking Lot*******
-- *                *
-- *                *
--- *****************



-- Comment can be done single line with --
-- Comment can be done multi line with /* */

/*
DQL - Data Query Language
Keywords:

SELECT - retrieve data, select the columns from the resulting set
FROM - the table(s) to retrieve data from
WHERE - a conditional filter of the data
GROUP BY - group the data based on one or more columns
HAVING - a conditional filter of the grouped data
ORDER BY - sort the data
*/


-- BASIC CHALLENGES
-- List all customers (full name, customer id, and country) who are not in the USA
SELECT * FROM Customer;

-- List all customers from Brazil
SELECT * FROM Customer WHERE Country = 'Brazil';

-- List all sales agents
SELECT LastName, FirstName FROM Employee;

-- SELECT * FROM employee WHERE title LIKE '%Agent%;

SELECT * FROM Employee WHERE Title LIKE '%Agent%';
-- Retrieve a list of all countries in billing addresses on invoices
SELECT BillingCountry FROM Invoice;

-- Retrieve how many invoices there were in 2021, and what was the sales total for that year?
SELECT COUNT(InvoiceDate), SUM(Total) FROM Invoice WHERE InvoiceDate Like '%2021%';


-- (challenge: find the invoice count sales total for every year using one query)


-- how many line items were there for invoice #37
-- SELECT * FROM Invoice;

-- how many invoices per country? BillingCountry  # of invoices 
SELECT 
    BillingCountry,
    COUNT(InvoiceId) AS NumberOfInvoices
FROM Invoice
GROUP BY BillingCountry;

-- Retrieve the total sales per country, ordered by the highest total sales first.
SELECT 
    BillingCountry,
    SUM(Total) AS TotalSum
FROM Invoice
GROUP BY BillingCountry
ORDER BY TotalSum;
-- JOINS CHALLENGES
-- Every Album by Artist
SELECT 
    Artist.Name AS ArtistName, 
    Album.Title AS AlbumTitle
FROM Artist
INNER JOIN Album
    ON Album.ArtistId = Artist.ArtistId

-- (inner keyword is optional for inner join)

-- All songs of the rock genre
SELECT 
    Artist.Name AS ArtistName, 
    Album.Title AS AlbumTitle
FROM Album
INNER JOIN Artist 
    ON Album.ArtistId = Artist.ArtistId
WHERE Artist.Name = 'AC/DC'

-- Show all invoices of customers from brazil (mailing address not billing)
SELECT * FROM Invoice;

SELECT 
    Customer.CustomerId AS CustomerId,
    Invoice.InvoiceDate AS InvoiceDate,
    Customer.Email AS CustomerEmail,
    Invoice.BillingCountry AS InvoiceBillingCountry
FROM Customer
INNER JOIN Invoice
    ON Customer.CustomerId = Invoice.CustomerId

-- Show all invoices together with the name of the sales agent for each one
SELECT 
    Invoice.InvoiceId AS InvoiceId,
    Invoice.InvoiceDate AS InvoiceDate,
    Customer.FirstName AS CustomerFirstName,
    Customer.LastName AS CustomerLastName,
    Employee.FirstName AS SalesAgentFirstName,
    Employee.LastName AS SalesAgentLastName
FROM Invoice
INNER JOIN Customer
    ON Invoice.CustomerId = Customer.CustomerId
INNER JOIN Employee
    ON Customer.SupportRepId = Employee.EmployeeId;
-- Which sales agent made the most sales in 2021?

-- How many customers are assigned to each sales agent?


-- Which track was purchased the most in 2022?


-- Show the top three best selling artists.


-- Which customers have the same initials as at least one other customer?


-- Which countries have the most invoices?


-- Which city has the customer with the highest sales total?


-- Who is the highest spending customer?


-- Return the email and full name of of all customers who listen to Rock.


-- Which artist has written the most Rock songs?


-- Which artist has generated the most revenue?




-- ADVANCED CHALLENGES
-- solve these with a mixture of joins, subqueries, CTE, and set operators.
-- solve at least one of them in two different ways, and see if the execution
-- plan for them is the same, or different.

-- 1. which artists did not make any albums at all?


-- 2. which artists did not record any tracks of the Latin genre?


-- 3. which video track has the longest length? (use media type table)



-- 4. boss employee (the one who reports to nobody)


-- 5. how many audio tracks were bought by German customers, and what was
--    the total price paid for them?



-- 6. list the names and countries of the customers supported by an employee
--    who was hired younger than 35.




-- DML exercises

-- 1. insert two new records into the employee table.

-- 2. insert two new records into the tracks table.

-- 3. update customer Aaron Mitchell's name to Robert Walter

-- 4. delete one of the employees you inserted.

-- 5. delete customer Robert Walter.