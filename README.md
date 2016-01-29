This software is named 'Waddle Village Game System'.

This file is part of Waddle Village Game System.

# Waddle Village

Waddle Village was a simple MMOG project I worked on, alongside some colleagues, back in 2010-2011. The game was eventually released and saw a small but respectable number of users on Alpha release. Unfortuantely due to waning interest on the project, the team eventually abandoned it, yet agreed to release the source code and files as opensource for people who are interested in rebooting the game or understanding how it worked.

## Installation

In order to be able to host & run Waddle Village in your computer with the code given as-is, you must download the latest version of the MySQL .NET connector. The MySQL .NET connector is property of Oracle. You may download it here:

http://dev.mysql.com/downloads/connector/net/

Afterwards, you must update both the Master Server and the Server references to the MySQL's .NET Connector Mysql.Data.dll. In order to do this, open up the servers with a C# IDE, for instance, Microsoft C# Express Edition 2008, and go to the Solution Explorer, docked right of the screen. Next, collapse the folder 'References' and remove the entry 'Mysql.Data'. 

Once this is done, right click 'References' and then select the option 'Add Reference...' and browse to the installation path of the MySQL .NET connector, and then select the file Mysql.Data.dll that may be found within that directory.

Please note that this must be done in both Master and Game server.

In order to run Waddle Village's servers, you must be using a Windows operating system and you must have installed version 4.0 or superior of .NET. .NET 4.0 is property of Microsoft. You may download it here:

http://www.microsoft.com/download/en/details.aspx?id=17851

You will also have to setup a table as described in the tableStructure.png file.

*Alternatively you may alter the code to disable all database dependency.*

## Usage

The server hosting the Master Server must have port 2565 open for TCP inboud and outbound traffic. The server hosting the Game Server must have ports 2566 open for inbound and outbound TCP access. Master Server must be first server to run. Multiple Game Servers may be running concurrently, but only one Master Server can be up at a single time (the Master Server is responsible for keeping track of all available Game Servers).

## Credits

See MENTIONS file for credits.

## License

This software is licensed under GNU's GPL license. Please read the file named 'LICENSE' or access <http://www.gnu.org/licenses/gpl.txt> for further details about the license.