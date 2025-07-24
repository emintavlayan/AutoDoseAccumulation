module VMS.TPS.Workflow

open PlanFunctions
open PlanModifiers
open FsToolkit.ErrorHandling
open VMS.TPS.Common.Model.API

/// Creates a modified plan from a given original plan and a new image plan
let createModifiedPlanFromDailyImage
    (course : Course)
    (originalPlan : ExternalPlanSetup)
    (newImagePlan : PlanSetup)
    (imagingDeviceId : string)
    (suffix : string)
    (prescriptionDose : float)
    (calculationModel : string)
    : Result<ExternalPlanSetup, string>
    =

    result {
        let! copiedPlan =
            copyPlanToNewImageSafe
                course
                originalPlan
                newImagePlan
                imagingDeviceId
                suffix

        let! preparedPlan =
            preparePlan copiedPlan prescriptionDose calculationModel

        // First dose calculation after setting prescription and model
        do! calculateDoseSafe "pre-weight" preparedPlan

        // Adjust beam weights using original plan MU values
        do! adjustBeamWeightsofPlans (originalPlan, preparedPlan)

        // Second dose calculation after modifying beam weights
        do! calculateDoseSafe "post-weight" preparedPlan

        return preparedPlan
    }

/// Creates modified plans from a list of image plans, returns success and error messages
let createModifiedPlansFromDailyImages
    (course : Course)
    (originalPlan : ExternalPlanSetup)
    (dailyImagePlans : ExternalPlanSetup list)
    (imagingDeviceId : string)
    (suffix : string)
    (prescriptionDose : float)
    (calculationModel : string)
    : string list * string list
    =

    // Folder function for accumulating results:
    // - On success, prepend a success message to the success list
    // - On failure, prepend a labeled error message to the error list
    let folder (successes, errors) imagePlan =
        match
            createModifiedPlanFromDailyImage
                course
                originalPlan
                imagePlan
                imagingDeviceId
                suffix
                prescriptionDose
                calculationModel
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
