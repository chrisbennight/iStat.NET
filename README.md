iStat.NET
=========

Server implementation for iPhone iStat app. 
Windows analogue to [iStat Server on OS X](http://bjango.com/iphone/istat/ "iStat Server on OS X") or [iStatd on linux/solaris](https://github.com/tiwilliam/istatd "iStatd on linux/solaris").
Hardware monitoring (cpu/temp) via Open Hardware Monitor

The server runs in the systems tray and starts listening on port 5109 on program launch.  The right click context menu (on the systems tray icon) gives access to an exit and settings choice.  The settings choice allows the user to set the expected pin code and clear the local client authentication cache.  

The manifest is set to request administrative access in order to query the system mainboard for fan/temperature information.  All other stats can be queried without this access level.

**The default PIN is 12345.  You can change it via the right click context menu.**