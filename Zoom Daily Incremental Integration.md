Zoom Daily Incremental Integration Documentation

1. Document Purpose
This document explains the end-to-end process used to extract Zoom operational reporting data from the Zoom API, clean and standardize it, and load it into Azure SQL target tables using an incremental daily load design.
The goal of this document is to explain:
•	what the process does, 
•	why it exists, 
•	what systems are involved, 
•	how authentication works, 
•	how data moves from source to target, 
•	how the daily date window is determined, 
•	how users, instances, and participants are loaded, 
•	how deduplication is performed, 
•	how schema mismatches are handled, 
•	how API and DB failures are logged, 
•	and how this design could later be migrated to Microsoft Fabric. 
________________________________________
2. High-Level Business Summary
This process pulls Zoom reporting data for a daily date window and stores it into Azure SQL tables under schema zm.
The process does not behave like a full refresh. Instead, it is an incremental historical load process.
That means:
1.	It retrieves all Zoom users from the source API. 
2.	It inserts only users that do not already exist in the target table. 
3.	It determines a reporting window for the previous day. 
4.	It retrieves meetings for each user for that date window. 
5.	It expands those meetings into meeting instances. 
6.	It skips meeting instances already present in the target SQL table. 
7.	It retrieves participant details for newly identified meeting instances. 
8.	It deduplicates participants using a composite business key. 
9.	It inserts only new rows into the target tables. 
10.	It writes CSV and text logs for execution traceability. 
Business meaning of this design
This process is intended to build and maintain a historical store of Zoom activity data.
Unlike a full refresh process, the target tables are not wiped and reloaded. Instead, they accumulate data over time while avoiding duplicates.
That makes this process useful when:
•	historical tracking is needed, 
•	the source data keeps growing daily, 
•	reloading everything each time would be inefficient, 
•	and the business wants a running history of Zoom users, meetings, and attendance details. 
________________________________________
3. Process Overview
End-to-End Flow
Source System : Zoom API
Intermediate Processing : Python script
Target System : Azure SQL Database
Optional Outputs : CSV extracts, text logs, failure logs
Simplified Process Flow
1.	Initialize current run timestamp using Central Time fixed offset 
2.	Create output directory if not already available 
3.	Build a robust HTTP session with retry handling 
4.	Get Zoom OAuth token 
5.	Connect to Azure SQL 
6.	Read target table column metadata 
7.	Read existing users and instance IDs from SQL 
8.	Pull all Zoom users from API with pagination 
9.	Save all users raw CSV 
10.	Identify only new users 
11.	Convert selected user datetime columns 
12.	Insert new users into [zm].[tbl_Users] 
13.	Re-read all user IDs from database 
14.	Compute yesterday’s date window 
15.	For every user, fetch meetings for the date window 
16.	For each meeting, fetch instance list 
17.	For each instance, skip duplicates already in DB or current run 
18.	Fetch full instance details 
19.	Standardize instance fields and save instance CSV 
20.	Preload participant dedupe keys from database 
21.	For each instance, fetch participants with pagination 
22.	Convert participant timestamps 
23.	Deduplicate participants against DB and current run 
24.	Save participant CSV 
25.	Insert new instances into [zm].[tbl_Instances] 
26.	Insert new participants into [zm].[tbl_Participants] 
27.	Save failure CSVs and info logs 
28.	Build and save execution summary 
29.	Close SQL connection 
________________________________________
4. Source System Details
Source Application :Zoom
Source API Usage
The script uses multiple Zoom API endpoints to collect the required data. 
4.1 OAuth Token Endpoint
Used to authenticate and obtain an access token.
The script calls:
•	https://zoom.us/oauth/token?grant_type=account_credentials&account_id=... 
This is an account credentials based OAuth flow.
4.2 Users Endpoint
Used to retrieve Zoom user records.
The script calls:
•	https://api.zoom.us/v2/users 
This endpoint is paginated using next_page_token.
4.3 User Meetings Report Endpoint
Used to retrieve meetings for each user for a given date window.
The script calls:
•	https://api.zoom.us/v2/report/users/{user_id}/meetings 
This is filtered using:
•	from 
•	to 
•	page_size 
•	next_page_token 
4.4 Meeting Instances Endpoint
Used to retrieve all past meeting instances for a meeting ID.
The script calls:
•	https://api.zoom.us/v2/past_meetings/{meeting_id}/instances 
This is important because a single Zoom meeting can have multiple past instances.
4.5 Meeting Details Endpoint
Used to fetch full details for a specific meeting instance.
The script calls:
•	https://api.zoom.us/v2/past_meetings/{encoded_uuid} 
The UUID is double URL encoded before sending the request. That behavior is explicitly implemented in the script because Zoom requires it for some UUID patterns. 
4.6 Participants Endpoint
Used to retrieve participant-level attendance records for a meeting instance.
The script calls:
•	https://api.zoom.us/v2/past_meetings/{encoded_uuid}/participants 
This is paginated using next_page_token.
________________________________________
5. Target System Details
Target Platform :Azure SQL Database
Database :BHG_DR
Schema :zm
Tables Used
5.1 Users Table :[zm].[tbl_Users]
Stores user-level Zoom records.
5.2 Instances Table :[zm].[tbl_Instances]
Stores past meeting instance level details.
5.3 Participants Table :[zm].[tbl_Participants]
Stores participant level meeting attendance rows.
The script is written to dynamically align source dataframe columns to these SQL table structures by reading the target metadata first. 
________________________________________
6. Technology Stack
The script uses the following technologies and libraries:
•	Python 
•	requests for API calls 
•	requests.Session() for connection reuse 
•	urllib3 Retry for retry handling 
•	HTTPAdapter for retry and pooling configuration 
•	pandas for dataframe transformation 
•	pyodbc for Azure SQL connectivity 
•	base64 for credential encoding 
•	urllib.parse.quote / unquote for UUID encoding and decoding 
•	datetime for time window and timestamp handling 
•	os for output directory and file writing 
________________________________________
7. Load Design
What type of load is this?
This is an incremental append-style load.
It is not a full refresh.
What happens in this incremental process?
Each run:
•	gets an OAuth token, 
•	reads all Zoom users, 
•	inserts only users not already present, 
•	looks only at the previous day’s meetings, 
•	identifies only new meeting instances, 
•	identifies only new participant rows, 
•	inserts only those new rows into the target tables. 
Why this design is used
This design is useful because:
•	users do not need to be reloaded every day if they already exist, 
•	instances should only be loaded once, 
•	participants should only be loaded once, 
•	history should be preserved in the target tables, 
•	the load window is limited to the previous day for efficiency. 
Difference between this and a full refresh
A full refresh would:
•	delete all target rows, 
•	pull all source rows, 
•	and reload everything again. 
This process does not do that.
Instead, it:
•	keeps old data, 
•	checks what already exists, 
•	and adds only new valid rows. 
________________________________________
8. Time Handling Design
Central Time logic used in the script
The script defines:
•	central_offset = timedelta(hours=-5) 
Then it computes:
•	current UTC time 
•	shifted Central Time based on fixed offset 
This means the script is not using timezone libraries for daylight saving awareness. It is simply subtracting 5 hours from UTC. The code itself notes that fixed offset -5 does not automatically handle DST. 
Why this matters
This affects:
•	the run timestamp, 
•	file naming, 
•	log timestamps, 
•	the previous-day date window, 
•	and the conversion of Zoom timestamps from UTC into SQL-ready naive datetime values. 
Date window logic
The script defines:
•	start_date = yesterday 
•	end_date = yesterday 
So this process only extracts data for one day at a time: the prior day in Central Time fixed offset logic. 
________________________________________
9. Output Directory and File Generation
The script writes outputs to a shared network path:
•	//bhgdallapp16/Users/AyxBHG/Alteryx/APIReports/ZoomAPI 
If the folder does not exist, it is created using os.makedirs(..., exist_ok=True). 
Why this output path exists
This directory stores:
•	raw extracted CSVs, 
•	transformed output CSVs, 
•	API failure CSVs, 
•	DB insert failure CSVs, 
•	info text files, 
•	summary log text files. 
This gives the process an audit trail and makes troubleshooting easier.
________________________________________
10. Logging Framework
The script defines several in-memory logging collections:
•	log_lines 
•	info_lines 
•	api_failures 
•	db_insert_failures 
10.1 Standard execution log
The log(msg) function:
•	creates a Central Time style timestamp, 
•	prints the line, 
•	appends it to log_lines. 
This is the main execution trace log.
10.2 CSV save helper
The save_csv(df, filename) function:
•	builds full path, 
•	writes dataframe to CSV, 
•	logs the saved path. 
10.3 Text save helper
The save_text(lines, filename) function:
•	writes lines into a .txt file, 
•	logs the saved path. 
10.4 Safe JSON parsing helper
The safe_json(resp, context) function:
•	attempts resp.json(), 
•	if parsing fails, captures context, URL, status, error, body snippet, 
•	appends that failure into api_failures, 
•	returns None. 
This is useful because some failed API responses may not be valid JSON.
________________________________________
11. Robust HTTP Session Design
The script creates a shared requests.Session() object through build_session().
What it configures
•	retry total = 8 
•	connect retries = 8 
•	read retries = 8 
•	backoff factor = 1.0 
•	retry status codes: 
o	429 
o	500 
o	502 
o	503 
o	504 
It also configures connection pooling:
•	pool connections = 20 
•	pool max size = 20 
Why this matters
This makes the integration more reliable by:
•	reducing failure on temporary API/network issues, 
•	handling throttling or transient server errors, 
•	reusing connections for performance, 
•	supporting a large number of repeated API calls. 
The function also handles urllib3 version compatibility by trying allowed_methods first and falling back to method_whitelist if necessary. 
________________________________________
12. Authentication Details
Authentication Method
The script uses Zoom OAuth account credentials flow.
Step-by-step authentication behavior
1.	A password string is defined in the script. 
2.	It is base64 encoded. 
3.	The token endpoint URL is built with: 
o	grant_type=account_credentials 
o	account_id=... 
4.	The request header is: 
o	Authorization: Basic <base64_password> 
5.	A POST request is sent to the Zoom token endpoint. 
6.	If HTTP status is not 200: 
o	the failure is recorded in api_failures 
o	the script raises a runtime error. 
7.	If response JSON does not contain access_token: 
o	the script raises a runtime error. 
8.	Otherwise, the token is stored and used in future API requests. 
Data API Header
After successful token retrieval, the script uses:
•	Authorization: Bearer <TOKEN> 
•	Content-Type: application/json 
________________________________________
13. Azure SQL Connection Details
The script connects to Azure SQL using pyodbc.connect(...). 
Target connection details used in code
•	Driver = ODBC Driver 17 for SQL Server 
•	Server = bhgazuresql01.database.windows.net 
•	Authentication = ActiveDirectoryPassword 
•	Database = BHG_DR 
A SQL cursor is then created for all metadata reads and insert operations.
Business importance
This database acts as the persistent target store for Zoom operational data.
________________________________________
14. Database Metadata and Schema Alignment
One important thing in this script is that it does not blindly push every API field into SQL.
Instead, it first reads the database schema using INFORMATION_SCHEMA.COLUMNS. 
Helper function: get_db_columns(cursor, schema, table)
This function returns all column names for a target SQL table.
It is used for:
•	tbl_Users 
•	tbl_Instances 
•	tbl_Participants 
Why this matters
APIs sometimes return extra fields that do not exist in the target table.
Rather than failing the insert, the script:
•	keeps only matching columns, 
•	drops non-DB columns, 
•	optionally logs what was dropped. 
This makes the process more schema tolerant.
________________________________________
15. Null Handling
The helper function normalize_missing(df) converts pandas missing values into Python None. 
Why this matters
SQL inserts need proper NULL values.
Pandas often represents missing values as:
•	NaN 
•	NaT 
These need to become None so pyodbc can write them correctly to SQL.
________________________________________
16. Generic Insert Logic
The most important reusable helper in the script is push_dataframe(...). 
What this function does
It receives:
•	SQL cursor 
•	full table name 
•	dataframe 
•	DB column set 
•	identifier fields for failure tracking 
Step-by-step internal behavior
1.	If dataframe is empty, return 0. 
2.	Copy dataframe. 
3.	Keep only columns that exist in SQL. 
4.	Record dropped source columns into info_lines. 
5.	If no matching columns remain: 
o	write warning to info_lines 
o	return 0. 
6.	Normalize missing values to None. 
7.	Build dynamic insert SQL: 
o	column list 
o	parameter placeholders 
8.	Convert dataframe rows into tuples. 
9.	Try batch insert using: 
o	cursor.fast_executemany = True 
o	cursor.executemany(...) 
10.	If batch insert succeeds: 
•	return inserted row count. 
11.	If batch insert fails: 
•	disable fast_executemany 
•	log batch error into info_lines 
•	retry row-by-row. 
12.	For any row-by-row failure: 
•	build identifier dictionary using the configured identifier fields 
•	store table name, identifiers, and error into db_insert_failures. 
Business purpose
This function makes the load:
•	efficient when batch insert works, 
•	resilient when one or more records fail, 
•	traceable because failures are logged with business identifiers. 
________________________________________
17. Initial Existing Record Lookup
Before pulling incremental data, the script reads target IDs already موجود in SQL.
Existing user IDs
It executes:
•	SELECT id FROM [zm].[tbl_Users] 
Existing instance UUIDs
It executes:
•	SELECT uuid FROM [zm].[tbl_Instances] 
These are stored as Python sets.
Why this matters
These sets are used to determine whether a source record is new or already loaded.
This is the foundation of the incremental load behavior.
________________________________________
18. Step 1: Pull All Zoom Users
The script starts user extraction by calling the Zoom users endpoint. 
User extraction logic
1.	Start with: 
o	url_users = https://api.zoom.us/v2/users 
o	empty all_users 
o	empty next_page_token 
2.	In a loop: 
o	request users with page_size = 300 
o	if next page exists, pass next_page_token 
3.	If the response status is not 200: 
o	log into api_failures 
o	stop the loop 
4.	Parse JSON using safe_json 
5.	Append returned users into all_users 
6.	Update next_page_token 
7.	Exit loop when no more token exists 
Output produced
After collection:
•	df_users_all = pd.DataFrame(all_users) 
This full raw user dataframe is written to:
•	zoom_all_users_raw_<timestamp>.csv 
Why save all users raw?
This gives a full source snapshot of all users returned by the API during that run, even though not all of them will be inserted into SQL.
________________________________________
19. Identify New Users Only
The script builds a filtered dataframe of users that do not already exist in SQL.
Logic
For each user in all_users:
•	user must have id 
•	id must not exist in existing_user_ids 
Those records are placed into:
•	df_users_new 
Datetime standardization for users
The script checks for these columns:
•	created_at 
•	last_login_time 
•	user_created_at 
If present:
1.	convert to pandas datetime with UTC 
2.	add central fixed offset 
3.	remove timezone info 
This produces SQL-friendly naive Central-Time-style datetime values. 
Output produced
The filtered new user dataframe is written to:
•	zoom_new_users_<timestamp>.csv 
Insert behavior
The script then calls push_dataframe(...) for:
•	[zm].[tbl_Users] 
Identifier fields used for failure logging:
•	id 
•	email 
After insert:
•	cnxn.commit() 
Then it logs:
•	number of users inserted 
Post-insert refresh
After inserting new users, the script reloads:
•	all user IDs from [zm].[tbl_Users] 
This is important because the next steps loop through all users currently available in the target, including newly inserted ones.
________________________________________
20. Date Window Determination
The script calculates the processing window based on now, which is already converted using the fixed Central offset.
It sets:
•	start_date = yesterday 
•	end_date = yesterday 
That means:
•	only meetings from the previous day are targeted, 
•	the window is one single date, 
•	this is effectively a daily load of yesterday’s Zoom activity. 
The date window is logged into execution logs. 
________________________________________
21. Step 2: Pull Meeting Instances
This is the biggest section of the script.
The purpose is to identify all new Zoom meeting instances from yesterday’s date window and capture their detailed attributes.
Structures initialized
•	instance_details = [] 
•	seen_instance_uuids = set() 
•	instance_refs = [] 
Purpose of each structure
instance_details
Holds detailed instance records that will later become a dataframe for SQL insert.
seen_instance_uuids
Tracks instances already seen in the current run to avoid duplicates during looping.
instance_refs
Stores simplified references needed later for participant extraction:
•	decoded UUID 
•	raw UUID 
•	meeting ID 
________________________________________
22. User-by-User Meeting Report Extraction
The script loops through all_user_ids.
For each user:
1.	set next_token = "" 
2.	call: 
o	/report/users/{user_id}/meetings 
3.	pass: 
o	from = yesterday 
o	to = yesterday 
o	page_size = 300 
o	next_page_token if present 
Failure handling
If API status is not 200:
•	log failure context = Report user meetings 
•	include URL, status, error, user_id 
•	break for that user 
Success handling
If response is valid:
•	parse JSON 
•	read meetings 
•	continue pagination using next_page_token 
________________________________________
23. Expand Meetings into Meeting Instances
For each meeting returned in the user report:
1.	read meeting_id 
2.	read host_id 
3.	if no meeting_id, skip 
4.	call: 
o	/past_meetings/{meeting_id}/instances 
Failure handling
If instance API call fails:
•	log context = Past meeting instances 
•	include meeting_id and host_id 
•	continue to next meeting 
Success handling
If instance response is valid:
•	loop through meetings inside the instance payload 
For each instance:
1.	read uuid_raw 
2.	read start_time 
3.	if either missing, skip 
4.	decode UUID using unquote() 
5.	check whether decoded UUID is already: 
o	in existing_instance_uuids 
o	in seen_instance_uuids 
6.	if yes, skip 
7.	parse start time into naive CT using helper 
8.	if parse fails, skip 
9.	if parsed date is outside yesterday’s window, skip 
Why the extra date filter exists
Even though the report query already used a date range, the script applies another date validation on the instance start time after converting it into CT naive datetime. This acts like an additional control layer.
________________________________________
24. Fetch Detailed Instance Data
If an instance survives all earlier filters, the script fetches full details.
UUID encoding logic
The script double URL encodes uuid_raw:
•	first quote(...) 
•	then quote again 
This is necessary for Zoom past meeting detail endpoints when UUIDs contain special characters. 
Details API call
It then calls:
•	/past_meetings/{encoded_uuid} 
Failure handling
If detail request fails:
•	log context = Past meeting details 
•	store URL, status, error, meeting_id, uuid_decoded, host_id 
•	continue 
Success handling
If response is valid:
the script enriches the details object with:
•	original_meeting_id 
•	instance_start_time 
•	uuid 
•	uuid_raw 
Then:
•	append full record to instance_details 
•	add decoded UUID to seen_instance_uuids 
•	append simplified reference into instance_refs 
Why uuid_raw is stored too
The script comments that uuid_raw may be auto-dropped if not in DB. That is because schema alignment later keeps only columns present in SQL. So the record can safely carry extra internal helper fields before insert. 
________________________________________
25. Build Instance DataFrame and Standardize Fields
After all users and meetings are processed:
•	df_instances = pd.DataFrame(instance_details) 
Numeric cleanup
The script standardizes these numeric fields if present:
•	duration 
•	total_minutes 
•	participants_count 
For each:
1.	convert using pd.to_numeric(..., errors="coerce") 
2.	replace inf and -inf with None 
Datetime cleanup
The script standardizes these datetime fields if present:
•	start_time 
•	end_time 
•	instance_start_time 
For each:
1.	parse with pandas datetime as UTC 
2.	add central fixed offset 
3.	remove timezone info 
Output produced
The instance dataframe is written to:
•	zoom_meeting_instances_<timestamp>.csv 
________________________________________
26. Participant Deduplication Design
Before extracting participants, the script prepares deduplication logic.
Deduplication key
The participant business key is defined as:
•	uuid 
•	user_id 
•	join_time 
These are stored in:
•	PARTICIPANT_KEY_COLS = ("uuid", "user_id", "join_time") 
Why this key is used
This means a participant row is considered duplicate if the same:
•	meeting instance UUID, 
•	user ID, 
•	and join time 
already exists.
This is a strong incremental business rule because the same person may attend different meetings or even the same meeting at different times, but the combination of UUID + user + join time is expected to be unique.
Check whether DB supports this key
The script verifies that all three columns exist in [zm].[tbl_Participants].
If not:
•	dedupe cannot be safely done, 
•	a warning is written into info_lines. 
________________________________________
27. Preload Existing Participant Keys from SQL
If dedupe columns exist, the script preloads participant keys already in SQL for the current run’s UUIDs.
Helper function: fetch_existing_participant_keys(...)
Inputs:
•	cursor 
•	UUID list 
•	chunk size = 800 
What it does
1.	If UUID list is empty, return empty set. 
2.	Create: 
o	time_min = start_date 00:00:00 
o	time_max = next day 00:00:00 
3.	Loop through UUIDs in chunks of 800 
4.	Build dynamic IN (...) SQL 
5.	Query: 
o	uuid 
o	user_id 
o	join_time
from [zm].[tbl_Participants]
where: 
o	uuid in current chunk 
o	join_time >= time_min 
o	join_time < time_max 
6.	Add those tuples to a Python set 
7.	Return the full set 
Why chunking is used
Chunking avoids creating excessively large SQL parameter lists.
Why time filter is used
It limits lookup to the current date window and reduces unnecessary data retrieval.
________________________________________
28. Step 3: Pull Participants
Now the script loops through instance_refs.
Each reference contains:
•	uuid_decoded 
•	uuid_raw 
•	meeting_id 
Per-instance logic
1.	Double URL encode uuid_raw 
2.	Initialize next_token = "" 
3.	Call: 
o	/past_meetings/{encoded_uuid}/participants 
4.	Pass: 
o	page_size = 300 
o	next_page_token if present 
Failure handling
If response fails:
•	log context = Past meeting participants 
•	include URL, status, error, meeting_id, uuid_decoded 
•	break for that instance 
Success handling
If response is valid:
•	parse participants 
•	process each participant row 
________________________________________
29. Participant Transformation Logic
For each participant row:
1.	add: 
o	uuid = uuid_decoded 
o	meeting_id = meeting_id 
2.	parse join_time into CT naive datetime 
3.	parse leave_time into CT naive datetime 
4.	overwrite those participant fields with parsed values 
Deduplication during participant processing
If DB participant dedupe is enabled:
1.	read user_id 
2.	if user_id is not null and join_time is not null: 
o	build key (uuid_decoded, user_id, join_time) 
3.	if key already exists in existing_participant_keys: 
o	skip this participant 
4.	else: 
o	add key to existing_participant_keys 
o	append participant to output 
Why this is important
This logic prevents two kinds of duplicate:
1.	duplicates already موجود in SQL from previous runs 
2.	duplicates appearing multiple times within the current run 
That is a very important part of the incremental design. 
Output produced
After all instances are processed:
•	df_participants = pd.DataFrame(participant_records) 
This dataframe is saved to:
•	zoom_meeting_participants_<timestamp>.csv 
________________________________________
30. Insert Meeting Instances into SQL
The script then pushes instance data into:
•	[zm].[tbl_Instances] 
Identifier fields for error tracking are:
•	uuid 
•	id 
•	original_meeting_id 
Insert behavior
The generic push_dataframe(...) function handles:
•	dropping columns not in DB, 
•	NULL conversion, 
•	batch insert, 
•	fallback row-by-row insert if batch fails, 
•	failure capture per row. 
After insert:
•	commit is issued 
•	inserted row count is logged 
________________________________________
31. Insert Participants into SQL
The script then pushes participant data into:
•	[zm].[tbl_Participants] 
Identifier fields for error tracking are:
•	uuid 
•	user_id 
•	join_time 
•	meeting_id 
•	id 
•	email 
•	name 
Why more identifier fields are used here
Participants are more granular than users or instances, so additional fields help support troubleshooting when one row fails.
After insert:
•	commit is issued 
•	inserted row count is logged 
________________________________________
32. Failure Output Files
The script writes several failure and info outputs if data exists.
32.1 API failures CSV
If api_failures is not empty:
•	convert to dataframe 
•	write to: 
o	zoom_api_failures_<timestamp>.csv 
This contains items such as:
•	context 
•	URL 
•	status code 
•	error message 
•	response snippet 
•	sometimes business identifiers like user_id or meeting_id 
32.2 DB insert failures CSV
If db_insert_failures is not empty:
•	normalize nested identifier dictionary 
•	flatten into columns 
•	write to: 
o	zoom_db_insert_failures_<timestamp>.csv 
This helps identify exactly which row failed and why.
32.3 Info text file
If info_lines is not empty:
•	write to: 
o	zoom_info_<timestamp>.txt 
This file captures non-fatal operational notes, such as:
•	columns dropped because they do not exist in SQL, 
•	warnings that no matching columns existed, 
•	batch insert failure fallback messages, 
•	participant dedupe capability warnings. 
________________________________________
33. Summary Generation
At the end of the run, the script computes:
•	script end time 
•	total runtime 
Then it creates a summary block with:
•	date window 
•	all users pulled 
•	new users detected 
•	users inserted 
•	instances fetched 
•	instances inserted 
•	participants fetched after dedupe 
•	participants inserted 
•	API failures logged 
•	DB insert failures logged 
•	script start time 
•	script completed time 
•	total runtime 
Each summary line is also passed through the log() function so it becomes part of execution trace. 
Summary text output
Then the script writes:
•	summary lines 
•	blank line 
•	full execution log lines 
into:
•	zoom_summary_log_<timestamp>.txt 
This becomes the main audit log for the run.
________________________________________
34. Resource Cleanup
At the end, the script closes:
•	SQL cursor 
•	SQL connection 
Then it logs:
•	Done. 
This ensures DB resources are properly released.
________________________________________
35. Transformations Summary
Transformation 1: OAuth token retrieval
Access token is fetched using account credentials flow.
Transformation 2: UTC to Central-Time-style conversion
Zoom UTC timestamps are shifted using fixed offset -5 and converted into naive datetimes.
Transformation 3: New user filtering
Only users not already in [zm].[tbl_Users] are inserted.
Transformation 4: Meeting instance dedupe
Only meeting instance UUIDs not already in DB or current run are processed.
Transformation 5: Date window filtering
Only instance records whose CT-converted date falls within yesterday’s date are processed.
Transformation 6: Numeric cleanup
Instance numeric fields are converted to numbers and invalid infinity values are nulled.
Transformation 7: Participant dedupe
Participant rows are deduplicated using:
•	uuid 
•	user_id 
•	join_time 
Transformation 8: Schema alignment
Only columns existing in the target SQL tables are inserted.
Transformation 9: Missing value normalization
Pandas missing values are converted to Python None.
Transformation 10: Failure logging
API and DB failures are logged into output files.
________________________________________
36. Source-to-Target Mapping Logic
This script uses dynamic source-to-target mapping.
Mapping rule
Source column name = Target column name
Only columns present in both:
•	source dataframe 
•	target SQL table 
are inserted.
Important note
This script does not explicitly rename columns before load.
It also does not create many derived business columns, except helper additions such as:
•	original_meeting_id 
•	instance_start_time 
•	uuid 
•	meeting_id 
If such fields do not exist in SQL, they are automatically dropped during schema alignment.
Benefit
This prevents SQL insert errors caused by extra source fields.
________________________________________
37. Error Handling
37.1 API errors
The script captures API failures for:
•	OAuth token retrieval 
•	user extraction 
•	report meetings 
•	meeting instances 
•	meeting details 
•	participant extraction 
•	JSON parse failures 
For each failure, the script attempts to record enough context to support investigation.
37.2 SQL insert errors
SQL failures are handled in two levels:
Batch level
If batch insert fails:
•	a message is logged, 
•	script falls back to row-by-row. 
Row level
If a specific row fails:
•	identifiers are captured, 
•	error is stored in db_insert_failures. 
This is a very useful production-style behavior because one bad row does not block the whole dataset.
________________________________________
38. Assumptions
This process assumes:
1.	Zoom OAuth credentials are valid. 
2.	Zoom API endpoints are active and reachable. 
3.	Users endpoint supports next_page_token. 
4.	User meeting report endpoint returns previous-day meetings correctly. 
5.	Past meeting instances endpoint returns valid UUIDs and timestamps. 
6.	Double URL encoded UUIDs work for detail and participant endpoints. 
7.	Azure SQL target tables already exist. 
8.	SQL schema names and table names are correct. 
9.	SQL login has insert/select privileges. 
10.	Participant dedupe columns exist if dedupe is expected. 
11.	Output network directory is accessible and writable. 
12.	Fixed Central offset -5 is acceptable for current business use. 
________________________________________
39. Current Operational Design
Runtime behavior
This script runs as a batch process.
Frequency
It is intended to run daily.
Windowing approach
Each run processes only:
•	yesterday’s date 
Data retention behavior
The script keeps accumulating history in SQL and does not delete old data.
Why this is a good fit
This suits operational reporting and historical attendance tracking where daily increments are enough.
________________________________________
40. Business Rules Identified from Script
The following rules are implemented directly in code:
1.	A user is inserted only if the user ID does not already exist in SQL. 
2.	A meeting instance is inserted only if its decoded UUID does not already exist in SQL and was not already seen in the current run. 
3.	A meeting instance must have both UUID and start time. 
4.	A meeting instance must belong to the previous-day date window after CT conversion. 
5.	Participant rows are unique by (uuid, user_id, join_time) when dedupe columns are available. 
6.	Join and leave times are converted to SQL-friendly naive datetimes. 
7.	Only target-supported columns are inserted. 
8.	Rows with source-only columns do not fail the load; extra columns are dropped. 
9.	If batch insert fails, row-by-row insert is attempted. 
10.	Failures are logged rather than silently ignored. 
________________________________________
41. Validation / Reconciliation Checks
After each run, the following checks are recommended.
41.1 Users duplicate validation
SELECT id, COUNT(*)
FROM zm.tbl_Users
GROUP BY id
HAVING COUNT(*) > 1;
41.2 Instances duplicate validation
SELECT uuid, COUNT(*)
FROM zm.tbl_Instances
GROUP BY uuid
HAVING COUNT(*) > 1;
41.3 Participants duplicate validation
SELECT uuid, user_id, join_time, COUNT(*)
FROM zm.tbl_Participants
GROUP BY uuid, user_id, join_time
HAVING COUNT(*) > 1;
41.4 Yesterday row count check
Compare:
•	instance rows fetched in CSV 
•	instance rows inserted to SQL 
•	participant rows fetched after dedupe 
•	participant rows inserted to SQL 
41.5 API failure review
Review:
•	zoom_api_failures_<timestamp>.csv 
41.6 DB failure review
Review:
•	zoom_db_insert_failures_<timestamp>.csv 
41.7 Schema drift review
Review:
•	zoom_info_<timestamp>.txt 
This can show whether Zoom returned columns not present in SQL.
________________________________________
42. Limitations of Current Design
42.1 Fixed timezone offset
The script uses -5 fixed offset instead of DST-aware timezone logic.
This may cause timing issues during daylight saving transitions.
42.2 Many API calls
The design makes multiple API calls per user and per meeting, which may become slow at scale.
42.3 User extraction is full-source read
Although user insertion is incremental, the source users extraction still reads all users every run.
42.4 Credentials are hardcoded
OAuth secret-like value and SQL password are present directly in code, which is a security risk.
42.5 No deletes or updates
This script inserts new records only. It does not handle:
•	changed user attributes, 
•	updated meeting records, 
•	deleted source records. 
42.6 Participant dedupe depends on table design
If target dedupe columns are missing, participant duplicate prevention becomes weaker.
42.7 Yesterday-only design
If the script misses a run, there is no automatic backfill for missed dates unless the code is manually adjusted.
________________________________________
43. Technical Function-by-Function Explanation
build_session()
Creates a reusable HTTP session with retries and connection pooling.
log(msg)
Builds a timestamped execution log line and stores it in memory.
save_csv(df, filename)
Writes a dataframe to a CSV file in the shared output directory.
save_text(lines, filename)
Writes text lines to a log file.
safe_json(resp, context)
Safely parses JSON and logs parse failures if the response body is invalid.
parse_zoom_iso_to_ct_naive(dt_str)
Converts Zoom ISO UTC datetime strings into fixed-offset Central naive datetime values.
get_db_columns(cursor, schema, table)
Reads target SQL table column names.
normalize_missing(df)
Converts pandas missing values into Python None.
push_dataframe(cursor, table_full, df, db_cols, identifier_fields)
Performs schema-aligned inserts with batch mode and row-by-row fallback plus failure tracking.
fetch_existing_participant_keys(cursor, uuids, chunk_size=800)
Reads already existing participant dedupe keys from SQL for the current run’s UUID/date window.
________________________________________
44. Technical Summary
This integration is a Python-based daily incremental ETL process for Zoom reporting data. It authenticates through Zoom OAuth account credentials, extracts users, prior-day meeting instances, and participant records through multiple paginated API calls, standardizes selected numeric and datetime fields, dynamically aligns source columns with target SQL schema, deduplicates users, instances, and participants using database lookups and in-run memory sets, and inserts only new rows into Azure SQL tables. It also produces CSV extracts, detailed logs, and failure outputs to support traceability and troubleshooting. 
________________________________________
45. Documentation for Required Credentials
Zoom
Required items include:
•	account credentials compatible OAuth setup 
•	account ID 
•	encoded Basic authorization source secret / credential value 
Azure SQL
Required items include:
•	SQL server name 
•	database name 
•	authentication method 
•	user ID / service account 
•	password 
Important note
These should ideally be stored securely in:
•	environment variables, 
•	secret vault, 
•	orchestration platform secrets, 
•	or secured configuration stores 
rather than directly in source code. The current script embeds them in code, which should be considered a security weakness. 
________________________________________
46. Documentation for Required Infrastructure
This process requires the following infrastructure to run successfully:
•	network access to Zoom API endpoints 
•	network access to Azure SQL server 
•	Python runtime with required libraries 
•	ODBC Driver 17 for SQL Server 
•	access to shared output folder path 
•	permissions to read from and insert into target SQL tables 
________________________________________
47. Final Conclusion
This integration provides a robust and production-oriented way to capture Zoom activity data into Azure SQL using an incremental daily load strategy. It is more advanced than a simple full refresh because it preserves history, performs duplicate prevention, handles pagination and retries, aligns source schema to target metadata, and logs both API and row-level SQL failures. It is well suited for operational analytics and attendance reporting where the business needs to retain daily meeting and participant data over time rather than replacing the target with only the latest snapshot. 

