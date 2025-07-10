namespace VMS.TPS

open System
open System.Windows.Forms
open FsToolkit.ErrorHandling
open VMS.TPS.Common.Model.API
open VMS.TPS.Common.Model.Types

module Utilities =

    // Utility to show result messages
    let showMessage (text: string) =
        MessageBox.Show(text, "Result", MessageBoxButtons.OK, MessageBoxIcon.Information)
        |> ignore

    // Get the current patient or return a string error
    let getCurrentPatient (context: ScriptContext) =
        match context.Patient with
        | null -> Error "No patient is currently loaded."
        | patient -> Ok patient

    // Get the current course or return a string error
    let getCurrentCourse (context: ScriptContext) =
        match context.Course with
        | null -> Error "No course is currently loaded."
        | course -> Ok course

    // Find the first ExternalPlanSetup with "aCT" in ID, or return an error
    let tryGetMatchingExternalPlan (course: Course) =
        let matchPlan =
            course.ExternalPlanSetups |> Seq.tryFind (fun plan -> plan.Id.Contains("aCT"))

        match matchPlan with
        | Some plan -> Ok plan
        | None -> Error "No external plan with 'aCT' in ID found."
