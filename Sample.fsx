#r "./bin/debug/Disinherit.dll"

open Disinherit

type Forms = Disinherited< @"System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" >

// Provided type
let button = Forms.Button()
// Provided property
button.AutoSizeMode
// Provided method
button.PerformClick()
// Provided event
button.DoubleClick.Add(fun e -> ())
// Reference all instance members
button.__Instance.Click.Add(fun e -> ())

#r "WindowsBase.dll"
#r "PresentationCore.dll"
#r "PresentationFramework.dll"
type WPF = Disinherited< @"PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" >

let b = WPF.Button()
