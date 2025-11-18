# WPR 0.0.14-pre-alpha :: dev branch
![](Images/logo.png)

WPR is a WP7-8 XNA app runner. This is only some *clone* of [WPR](https://github.com/8212369/WPR), not the original. 

*CAUTION*: this *dev* branch is for internal dev use. Experienced devs only!


## Status
- I tried to "devide" WPR system on/by 2 parts: XapToWPR converter & WPR "installer/launcher". Work-in-progress (5% done).
- XapToWPR uses Avalonia "engine"
- WPR.UWP, WPR.Android, WPR.iOS - Xamarin Forms (see new Src/WPR folder).
- XapToWPR damaged (great tech. problems on app run!)
- WPR.UWP (and "common" WPR) damaged too, sadly


## Tech. details
- Newest VS 2022 Preview used to "assemble" (build) this "dev branch"
- I think that WPR "dev edition" incompatible with Win10 because of .NET 8... So, fresh Windows11-based OS needed to run WPR (Official Win 11 Pro recommended.. however, some reduced Windows 11 Tiny is good choice too, for example my 15-year-old retro-notebook Sony Vaio)))

## Bugs 

- After Avalonia 11.0.999-beta upgrade I damaged Windows target: 
```
System.TypeInitializationException: 'The type initializer for 'DialogHost.DialogHost' threw an exception.'

MissingMethodException: Method not found: 'Avalonia.StyledProperty`1<!!1> Avalonia.AvaloniaProperty.Register(System.String, !!1, Boolean, Avalonia.Data.BindingMode, System.Func`2<!!1,Boolean>, System.Func`3<Avalonia.IAvaloniaObject,!!1,!!1>, System.Action`2<Avalonia.IAvaloniaObject,Boolean>)'.
```
- Android target damaged too... idk how to fix it.


## ToDo
- Try to fix XapToWPR 
- Try to fix WPR.UWP, etc.
- Actualize Wiki section
- Transtale/dublicate Readme to RU and CN
- Fix resolution scaling (for example, Zuma's scaling bug)


## Credits
- Tyler Jaacks (https://github.com/TylerJaacks) - for net5/6 -> net8 upgrade !
- Hector47 (https://github.com/Hector47) for try to add some online services and more :)

## Another cool forks I noticed over 3 years 
-  https://github.com/TylerJaacks/WPR (branches *net8_upgrade* & *dotnet_upgrade* are very interesting & useful!)
-  https://github.com/Hector47/WPR (master branch: some GameServices ideas)

## :: ::
AS IS. No support. Developers / Geeks only. "DIY mode"

## ::
[m][e] Nov, 18 2025

![](Images/footer.png) 

[ https://www.youtube.com/watch?v=X-G8oXebid0 ]

