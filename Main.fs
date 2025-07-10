namespace VMS.TPS

open Functions
open FsToolkit.ErrorHandling
open VMS.TPS.Common.Model.API


// Assembly attribute to indicate the script can write data
[<assembly: ESAPIScript(IsWriteable = true)>]
do ()

/// This type of Running the code is imposed by Varian Esapi library.
[<System.Runtime.CompilerServices.CompilerGeneratedAttribute>]
type Script() =
    member __.Execute(context: ScriptContext) =

        context.Patient.BeginModifications()

        result {
            let! patient = getCurrentPatient context

            let! course = getCurrentCourse context

            let! currentPlan = tryGetMatchingExternalPlan course

            let modifiedPlan = modifyPlan currentPlan

            let! copiedPlan = copyPlanToNewStructureSet course patient modifiedPlan

            copiedPlan.CalculateDose() |> ignore
            //copiedplan.CalculateDoseWithPresetValues() //if you need tio set MU values

            return "Plan copied and dose calculated successfully."
        }

        |> function
            | Ok msg -> showMessage msg
            | Error err -> showMessage err
