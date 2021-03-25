# NUT.Net
A .Net client library for communicating with Network UPS Tools servers.

# What is NUT
[Network UPS Tools](https://networkupstools.org/index.html) is a project dedicated to supporting power devices, such as uninterruptible power supplies. The software is built to control and monitor many features of power devices and provides a common protocol for communicating to other devices across a network.

# What this project does
Inspired by the [WINNut](https://github.com/gawindx/WinNUT-Client) client, this project intends to create a compliant and efficient .Net library that can be used by any .Net application to communicate with, retrieve data from and send commands to a NUT server managing one or power UPSs. This project is written using the .Net Standard Framework, version 2.0.

# Current Features
- Most of the NUT protocol, including
  - GET commands to retrieve information from the server
  - LIST commands for listing information from a server or UPS
  - SET VAR command, to change a variable on a UPS
  - INSTCMD to run a command on a UPS
  - USERNAME and PASSWORD to run commands and retrieve information that are privileged
  - LOGIN and LOGOUT to indicate dependency on a UPS
  - VER and NETVER to retrieve basic information from the NUT server
- Logical data model that represents a connection to a NUT server, and each UPS on the server along with its properties
- Error handling as they're returned from the server
- Created alongside a mockup server with unit testing to achieve accurate results

# References
[Network UPS Tools GitHub project](https://github.com/networkupstools/nut/)
[jNut - A NUT client written in Java](https://github.com/networkupstools/jNut)
[WinNUT Client GitHub project](https://github.com/gawindx/WinNUT-Client)

# Extra Links
[NuGet.org Package](https://www.nuget.org/packages/NUTDotNetClient/)
