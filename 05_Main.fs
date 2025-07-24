namespace VMS.TPS

open VMS.TPS.Common.Model.API
open Workflow
open FsToolkit.ErrorHandling
open System.Reflection

[<assembly : ESAPIScript(IsWriteable = true)>]
[<assembly : AssemblyVersion("1.0.1.2")>]
[<assembly : AssemblyFileVersion("1.0.1.2")>]
do ()

[<System.Runtime.CompilerServices.CompilerGeneratedAttribute>]
type Script() =
    member __.Execute(context : ScriptContext) =

        let result = result {
            // Gets the currently loaded patient and begins modifications
            let! patient = Utilities.tryGetCurrentPatient context
            do patient.BeginModifications()

            // Gets the current course
            let! course = Utilities.tryGetCurrentCourse context

            // Gets the original plan containing 'HH' in its ID
            let! originalPlan = Utilities.tryFindPlanByIdPattern course "HH"

            // Gets all daily image plans containing 'aCT' in their IDs
            let! allImagePlans = Utilities.tryFindMatchingPlans course "aCT"

            // --- MULTI PLAN VERSION ---
            let successMsgs, errorMsgs =
                createModifiedPlansFromDailyImages
                    course
                    originalPlan
                    allImagePlans
                    "Default_CT_Scanner" // imagingDeviceId
                    "C" // suffix to add to modified plan ids
                    2.0 // prescriptionDose
                    "AcurosXB_18.0.1" // calculationModel

            return successMsgs @ errorMsgs
        }

        match result with
        | Ok messages ->
            // Show all success and error messages after workflow completes
            Utilities.showMessageBox (String.concat "\n" messages)
        | Error msg ->
            // Show fatal setup error (context, course, or plan loading)
            Utilities.showMessageBox $"[Setup Error] {msg}"
