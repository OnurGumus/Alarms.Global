module Tests

open Expecto
open ExpectoTickSpecHelper

[<Tests>]
let show_tasks = featureTest "GlobalEvent.feature"