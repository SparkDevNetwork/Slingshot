-- Make sure they are updated to 1.6.4 (or above) or 1.7
-- NOTE: If they are at 1.6.7 or earlier, they will need the 'EntityFramework.Utilities.dll' dll put into their RockWeb/Bin folder
-- Backup the Customer’s Database
-- Update Rock > Home / System Settings / System Configuration and set Max Upload File Size to 100MB (if it isn’t already)
-- Verify that Rock > Home / General Settings / File Types / ‘Person Image’, has the Storage Type set to what you want.  Slingshot will use that when importing Photos

/********** This note can go away when we added the ForiegnId/ForeignKey with SystemName feature to slingshot **
NOTE: Right now, Slingshot assume that none of the data in the target system has ForeignIds other than what Slingshot creates.  So, if this the target system already has data in it, you'll need to check this first.
 If the target system does have ForeignIds in [Person],[FinancialTransaction], etc, you'll have to ask if they can be wiped.  If so, you can use Power Tools to do it as follows:

 -- Slingshot Tables with Foreign Ids

update [Person] set ForeignId = null where ForeignId is not null;
update [Group] set ForeignId = null where ForeignId is not null;
update [Location] set ForeignId = null where ForeignId is not null;
update [Schedule] set ForeignId = null where ForeignId is not null;
update [FinancialAccount] set ForeignId = null where ForeignId is not null;
update [FinancialBatch] set ForeignId = null where ForeignId is not null;

update [Attendance] set ForeignId = null where ForeignId is not null;
update [FinancialTransaction] set ForeignId = null where ForeignId is not null;
update [FinancialTransactionDetail] set ForeignId = null where ForeignId is not null;
update [FinancialPaymentDetail] set ForeignId = null where ForeignId is not null;

update [BinaryFile] set ForeignKey = null where ForeignKey is not null;


-- If the above times out, run these individually until all the records have been updated
-- *repeat until 0 records updated
update top (100000)[Attendance] set ForeignId = null where ForeignId is not null;
update top (100000)[FinancialTransaction] set ForeignId = null where ForeignId is not null;
update top (100000)[FinancialTransactionDetail] set ForeignId = null where ForeignId is not null;
update top (100000)[FinancialPaymentDetail] set ForeignId = null where ForeignId is not null;
********/

-- Get .slingshot file from slingshot.ccb (or other source system)
-- After Importing on the Rock System
	-- Go the General Settings / Group Types and filter by Check-in Template. This will show you the group types that already a Check-in Template
	-- Now, in a separate window, go to Power Tools / SQL Command

// Use this SQL to figure out what GroupTypes were involved in the Attendance Import, and what their Parent Group Type is

SELECT gt.NAME [GroupType.Name], gt.Id, max(gt.CreatedDateTime) [GroupType.CreateDateTime]
       ,count(*) [AttendanceCount]
       ,(
              SELECT TOP 1 pgt.NAME
              FROM GroupTypeAssociation gta
              INNER JOIN GroupType pgt ON pgt.Id = gta.GroupTypeId
              WHERE ChildGroupTypeId = gt.id
              ) [Parent Group Type]
FROM Attendance a
INNER JOIN [Group] g ON a.GroupId = g.Id
INNER JOIN [GroupType] gt ON g.GroupTypeId = gt.id
GROUP BY gt.NAME,gt.Id
order by gt.Id desc


// To see a break down by Group Name and Type, this SQL is handy

SELECT gt.NAME [GroupType.Name]
,gt.Id
     ,g.Name [Group.Name]
       ,count(*) [AttendanceCount]
       ,MAX(PGT.NAME) [Parent Group Type]
       ,MAX(PGT.GroupTypePurpose) [Parent Group Type Purpose]
	   ,max(gt.CreatedDateTime) [GroupType.CreateDateTime]
FROM Attendance a
INNER JOIN [Group] g ON a.GroupId = g.Id
INNER JOIN [GroupType] gt ON g.GroupTypeId = gt.id
OUTER APPLY (
       SELECT TOP 1 pgt.NAME
              ,dv.Value [GroupTypePurpose]
       FROM GroupTypeAssociation gta
       INNER JOIN GroupType pgt ON pgt.Id = gta.GroupTypeId
       LEFT JOIN DefinedValue dv ON pgt.GroupTypePurposeValueId = dv.Id
       WHERE gta.ChildGroupTypeId = gt.id
       ) PGT
GROUP BY gt.NAME
       ,gt.Id
       ,g.Name
order by Gt.Id, Gt.Name, g.Name

-- Now, back to Rock > Home / General Settings / Group Types, select a Checkin-Template group type.  For example, Weekly Service Check-in Area
	-- Using the SQL Results, add the Child Group Types to the appropriate Checkin-Template group type. 
	-- Ones that sound like Weekend Check-in will go in Weekend Check-in GroupType, then the 'General' panelwidget | Child Group Types
	-- Ones that sound like Volunteer Check-in will go in Volunteer Check-in GroupType, then the 'General' panelwidget | Child Group Types

-- Now Attendance Analytics will be able to show the import Attendance Data

