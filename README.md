**PLEASE VISIT THE FOLLOWING PICTURES FOR A DEMO OF THIS PROJECT**

https://ibb.co/Lg5mZ0L

https://ibb.co/R0GsHyC

# RstruiAutomation
This project automates the system restoration using **rstrui.exe**.

Windows restoration is a crucial task for malware analysis. In our academic paper, we used the following project to automate the Windows Restore Point system restoration.

We didn't find a command-line option to automate this task, so the following project automates it by using `SendMessage` Win32 API.

As you can see from the source code, it first enumerates all of the components like "Buttons" and "ListViews" you can see a list of **rstrui.exe** components below:

![](https://github.com/skarvandi/RstruiAutomation/raw/main/Demo/Components.png)

After that, a set of actions are done to automate, 

- First, a click on the 'Next' button
- Second, select an item from the SysListView32 component
- Third, select the 'Next' button again
- Finally, click the 'Finish' button to perform the restoration

The following code shows the exact process.

![](https://github.com/skarvandi/RstruiAutomation/raw/main/Demo/AutomatingRstrui.PNG)

Note that clicking on the final 'Finish' button is commented for test purposes. You can uncomment and use it to perform the last step.
