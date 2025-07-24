module VMS.TPS.PlanFunctions

open VMS.TPS.Common.Model.API
open VMS.TPS.Common.Model.Types
open FsToolkit.ErrorHandling

/// Attempts to set prescription, returns Result
let setPrescriptionSafe (dose : float) (plan : PlanSetup) =
    let doseValue =
        DoseValue(dose, "Gy")

    try
        plan.SetPrescription(1, doseValue, 1)
        Ok(plan)
    with ex ->
        Error $"Failed to set prescription: {ex.Message}"

/// Attempts to set calculation model, returns Result
let setCalculationModelSafe (model : string) (plan : PlanSetup) =
    try
        plan.SetCalculationModel(CalculationType.PhotonVolumeDose, model)
        Ok(plan)
    with ex ->
        Error $"Failed to set calculation model: {ex.Message}"

/// Copies a plan onto the image of another plan
let copyPlanToNewImageSafe
    (course : Course)
    (originalPlan : PlanSetup)
    (newImagePlan : PlanSetup)
    (imagingDeviceId : string)
    (suffix : string)
    : Result<PlanSetup, string>
    =
    let newImage =
        newImagePlan.StructureSet.Image

    let outputDiagnostics =
        System.Text.StringBuilder("Diagnostics: ")

    try
        newImage.Series.SetImagingDevice(imagingDeviceId) // set the imaging device id

        let copiedPlan =
            course.CopyPlanSetup(originalPlan, newImage, outputDiagnostics)

        copiedPlan.Id <- newImagePlan.Id + suffix // renaming logic
        Ok copiedPlan

    with ex ->
        Error $"Failed to copy plan: {ex.Message}"

/// Validates that beam IDs match in order
let checkBeamIdOrderAndEquality
    (originalPlan : PlanSetup)
    (newImagePlan : ExternalPlanSetup)
    : Result<unit, string>
    =
    let origIds =
        originalPlan.Beams
        |> Seq.map (fun b -> b.Id)
        |> Seq.toList

    let newIds =
        newImagePlan.Beams
        |> Seq.map (fun b -> b.Id)
        |> Seq.toList

    if origIds = newIds then
        Ok()
    else
        Error "Beam IDs do not match in order."

/// Adjusts a copied beam's weight factor to match the original MU ratio
let adjustWeightsOfBeamPair (sourceBeam : Beam, targetBeam : Beam) : Result<unit, string> =
    let origMU =
        sourceBeam.Meterset.Value

    let copiedMU =
        targetBeam.Meterset.Value

    let targetBeamParameters =
        targetBeam.GetEditableParameters()

    try
        targetBeamParameters.WeightFactor <- origMU / copiedMU
        targetBeam.ApplyParameters(targetBeamParameters)
        Ok()
    with ex ->
        Error $"Failed to adjust beam weight of {targetBeam.Id}: {ex.Message}"

/// Adjusts beam weight factors for two matching plans by pairing beams by ID
let adjustBeamWeightsofPlans
    (sourcePlan : ExternalPlanSetup, targetPlan : ExternalPlanSetup)
    : Result<unit, string>
    =
    result {
        let sourceBeams =
            sourcePlan.Beams
            |> Seq.filter (fun b -> not b.IsSetupField)
            |> Seq.sortBy (fun b -> b.Id)
            |> Seq.toArray

        let targetBeams =
            targetPlan.Beams
            |> Seq.filter (fun b -> not b.IsSetupField)
            |> Seq.sortBy (fun b -> b.Id)
            |> Seq.toArray

        do!
            if
                sourceBeams.Length
                <> targetBeams.Length
            then
                Error "Source and target plans have different number of beams"
            else
                Ok()

        for i in 0 .. sourceBeams.Length - 1 do
            do! adjustWeightsOfBeamPair (sourceBeams[i], targetBeams[i])
    }
