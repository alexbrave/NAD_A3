--------
 SERVER
--------
Authors: Andrey Takhtamirov and Alex Braverman

This is the README for the server.py application.
This is a server which can accept multiple clients at a time over TCP and allows logging to a file. 
The client's message must be of proper format.

Simply run the server script to start. Some python modules might need to be installed before the first run.

Logged time is represented in UTC


CONFIG
------
- The address and port must be configured in the config.yaml file.

Can be configured:
- The delimiter (client message element separator)
- file name, directory
- log time format
- complete log message format
- client sending format
- log levels and client log level codes
- log levels to turn on/off client logging
- indexes of client message elements
- noise handling limits (time and number of messages)

The accepted format is:

	<int_ms_since_epoch>|<int_client_id>|<int_log_level>|<str_message>

- The client ID is the time at client's startup, in seconds.


This message will be logged in such format:

	<Y-m-d H:M:S.3ms> <int_client_id> <str_log_level>\t<message>\n

- This format can be configured in the config file.


LOGGING LEVELS
--------------
By default, 8 logging levels are supported:

Client Code	Logging Level	Notes
0		ALL		Turns on client logging (if off)
1		DEBUG
2		INFO
3		WARN
4		ERROR
5		FATAL
6		OFF		Turns client logging off until turned back on.
7		TRACE

- Note: When "OFF", all messages other than "ALL" are ignored.
- New levels can be added in the config file.
- Configurable "ON" and "OFF" codes.


NOISE
-----
Anti-Noise Features:

- Message/Time limit configured in config file.
- If a client has reached the max message limit, other messages from the client are ignored until the specified time limit runs out.


Mis-Configured Clients:

- A mis-configured client is any client which doesn't meet the required message restrictions.
- Mis-configured clients are ignored.

