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
[<Literal>]
let name = @"PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"
type WPF = Disinherited< name , level=1 >
let button' = WPF.Button()
// Access level 1 inherited event
button'.Click.Add(fun e -> ())
// Get disinherited instance from existing instance
let from = WPF.Button.From(System.Windows.Controls.Button())
from.IsPressed