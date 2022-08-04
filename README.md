# RstruiAutomation
This project automates the system restoration using **rstrui.exe**.

Windows restoration is a crucial task for malware analysis. In our academic paper, we used the following project to automate the Windows Restore Point system restoration.

We didn't find a command-line option to automate this task, so the following project automates it by using `SendMessage` Win32 API.

As you can see from the source code, it first enumerates all of the components like "Buttons" and "ListViews" you can see a list of **rstrui.exe** components below:

![](https://github.com/skarvandi/RstruiAutomation/raw/main/Demo/Components.png)



![](https://github.com/skarvandi/RstruiAutomation/raw/main/Demo/AutomatingRstrui.PNG)
