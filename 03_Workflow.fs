module VMS.TPS.Workflow

open PlanFunctions
open FsToolkit.ErrorHandling
open VMS.TPS.Common.Model.API

/// Creates a modified plan from a given original plan and a new image plan
let createModifiedPlanFromDailyImage
    (course : Course)
    (originalPlan : PlanSetup)
    (newImagePlan : PlanSetup)
    (imagingDeviceId : string)
    (suffix : string)
    : Result<ExternalPlanSetup, string>
    =

    result {
        let! copiedPlan =
            PlanFunctions.copyPlanToNewImage course originalPlan newImagePlan imagingDeviceId suffix

        let! modifiedPlan =
            copiedPlan
            |> trySetPrescription 2.0
            |> Result.bind (trySetCalculationModel "AcurosXB_18.0.1")

        let modifiedExternalPlan =
            modifiedPlan :?> ExternalPlanSetup

        // First dose calculation after setting prescription and model
        try
            modifiedExternalPlan.CalculateDose()
            |> ignore
        with ex ->
            return! Error $"Dose calculation failed (pre-weight): {ex.Message}"

        // Adjust beam weights using original plan MU values
        do! adjustBeamWeightsofPlans (originalPlan, modifiedPlan)

        // Second dose calculation after modifying beam weights
        try
            modifiedExternalPlan.CalculateDose()
            |> ignore
        with ex ->
            return! Error $"Dose calculation failed (post-weight): {ex.Message}"

        return modifiedExternalPlan
    }

/// Creates modified plans from a list of image plans, returns success and error messages
let createModifiedPlansFromDailyImages
    (course : Course)
    (originalPlan : ExternalPlanSetup)
    (dailyImagePlans : ExternalPlanSetup list)
    (imagingDeviceId : string)
    (suffix : string)
    : string list * string list
    =

    // Folder function for accumulating results:
    // - On success, prepend a success message to the success list
    // - On failure, prepend a labeled error message to the error list
    let folder (successes, errors) imagePlan =
        match
            createModifiedPlanFromDailyImage course originalPlan imagePlan imagingDeviceId suffix
        with
        | Ok plan ->
            let msg =
                $"Plan '{plan.Id}' created successfully."

            (msg :: successes, errors)
        | Error err ->
            let msg =
                $"[ERROR: {imagePlan.Id}] {err}"

            (successes, msg :: errors)

    // Fold over all image plans, accumulating successes and errors
    // Then reverse both lists to restore original processing order
    List.fold folder ([], []) dailyImagePlans
    |> fun (s, e) -> List.rev s, List.rev e
