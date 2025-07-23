module VMS.TPS.Utilities

open VMS.TPS.Common.Model.API

/// Shows the given message for debugging purposes
let showMessageBox (message : string) =
    System.Windows.Forms.MessageBox.Show(message)
    |> ignore

/// Gets the currently loaded patient or returns an error
let tryGetCurrentPatient (context : ScriptContext) =
    if isNull context.Patient then
        Error "No patient is currently loaded."
    else
        Ok context.Patient

/// Gets the currently loaded course or returns an error
let tryGetCurrentCourse (context : ScriptContext) =
    if isNull context.Course then
        Error "No course is currently loaded."
    else
        Ok context.Course

/// Finds a single external plan containing the given pattern in its ID
let tryFindPlanByIdPattern (course : Course) (pattern : string) =
    course.PlanSetups
    |> Seq.tryFind (fun plan ->
        plan.Id.Contains(pattern)
        && plan :? ExternalPlanSetup)
    |> function
        | Some(:? ExternalPlanSetup as extPlan) -> Ok extPlan
        | _ -> Error $"No external plan with '{pattern}' in ID found."

/// Finds all external plans containing the given pattern in their IDs
let tryFindMatchingPlans (course : Course) (pattern : string) =
    let planList =
        course.PlanSetups
        |> Seq.choose (fun plan ->
            match plan with
            | :? ExternalPlanSetup as extPlan when extPlan.Id.Contains(pattern) -> Some extPlan
            | _ -> None)
        |> Seq.toList

    if List.isEmpty planList then
        Error $"No external plans with '{pattern}' in ID found."
    else
        Ok planList
