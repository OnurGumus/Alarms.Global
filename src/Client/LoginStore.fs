module HolidayTracker.Client.LoginStore
open ElmishSideEffect
open HolidayTracker.MVU.LoginStore

let rec execute sideEffect dispatch =
    match sideEffect with
    | SideEffect.NoEffect -> ()
   
let store,dispatcher = Program.mkStoreWithSideEffectExecute init update dispose execute ()