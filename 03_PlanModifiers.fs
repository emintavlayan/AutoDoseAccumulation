module VMS.TPS.PlanModifiers

open VMS.TPS.Common.Model.API
open FsToolkit.ErrorHandling
open PlanFunctions

/// Copies a plan to a new image and applies prescription and calculation model
let preparePlan
    (plan : PlanSetup)
    (prescriptionDose : float)
    (calculationModel : string)
    : Result<ExternalPlanSetup, string>
    =
    result {

        let! prepared =
            plan
            |> setPrescriptionSafe prescriptionDose
            |> Result.bind (setCalculationModelSafe calculationModel)

        return prepared :?> ExternalPlanSetup
    }

/// Calculates dose for a plan and returns an error message if it fails
let calculateDoseSafe (stage : string) (plan : ExternalPlanSetup) =
    try
        plan.CalculateDose() |> ignore

        let mu =
            plan.Beams
            |> Seq.head
            |> fun b -> b.Meterset.Value
            |> string

        VMS.TPS.Utilities.showMessageBox (
            $"Plan {plan.Id} first beam MU : {mu:F2}"
        )

        Ok()
    with ex ->
        Error $"Dose calculation failed ({stage}): {ex.Message}"
