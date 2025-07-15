namespace VMS.TPS

open System
open System.Windows.Forms
open VMS.TPS.Common.Model.API

module Utilities =

    // Utility to show result messages
    let showMessage (text : string) =
        MessageBox.Show(text, "Result", MessageBoxButtons.OK, MessageBoxIcon.Information)
        |> ignore

    // Get the current patient or return a string error
    let getCurrentPatient (context : ScriptContext) =
        match context.Patient with
        | null -> Error "No patient is currently loaded."
        | patient -> Ok patient

    // Get the current course or return a string error
    let getCurrentCourse (context : ScriptContext) =
        match context.Course with
        | null -> Error "No course is currently loaded."
        | course -> Ok course

    // Find the first ExternalPlanSetup with "aCT" in ID, or return an error
    let tryGetMatchingExternalPlan (course : Course) (pattern : string) =
        let matchPlan =
            course.ExternalPlanSetups
            |> Seq.tryFind (fun plan -> plan.Id.Contains(pattern))

        match matchPlan with
        | Some plan -> Ok plan
        | None -> Error(sprintf "No external plan with %s in ID found." pattern)

    let tryGetMatchingExternalPlans (course : Course) (pattern : string) =
        let planList =
            course.ExternalPlanSetups
            |> Seq.filter (fun plan -> plan.Id.Contains(pattern))
            |> Seq.toList

        match planList with
        | [] -> Error(sprintf "No external plan with %s in ID found." pattern)
        | _ -> Ok planList
