module Tests

open Expecto
open ExpectoTickSpecHelper

[<Tests>]
let global_event = featureTest "GlobalEvent.feature"