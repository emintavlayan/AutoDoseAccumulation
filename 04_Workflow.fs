/// High-level workflow for generating modified plans from daily images.
module VMS.TPS.Workflow

open PlanFunctions
open PlanModifiers
open FsToolkit.ErrorHandling
open VMS.TPS.Common.Model.API

/// Copies the reference plan to a new image, prepares it,
/// recalculates dose, adjusts beam weights, recalculates again,
/// and returns the final plan.
let createModifiedPlanFromDailyImage
    (course : Course)
    (referencePlan : ExternalPlanSetup)
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
                referencePlan
                newImagePlan
                imagingDeviceId
                suffix

        let! preparedPlan =
            preparePlan copiedPlan prescriptionDose calculationModel

        // First dose calculation after setting prescription and model
        do! calculateDoseSafe "pre-weight" preparedPlan

        // Adjust beam weights using original plan MU values
        do! adjustBeamWeightsofPlans (referencePlan, preparedPlan)

        // Second dose calculation after modifying beam weights
        do! calculateDoseSafe "post-weight" preparedPlan

        return preparedPlan
    }

/// Processes multiple image plans, returning lists of success and error messages.
let createModifiedPlansFromDailyImages
    (course : Course)
    (referencePlan : ExternalPlanSetup)
    (allImagePlans : ExternalPlanSetup list)
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
                referencePlan
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
    List.fold folder ([], []) allImagePlans
    |> fun (s, e) -> List.rev s, List.rev e
