/// Composite operations that prepare a plan and perform dose calculations.
module VMS.TPS.PlanModifiers

open VMS.TPS.Common.Model.API
open FsToolkit.ErrorHandling
open PlanFunctions

/// Applies prescription and calculation model, returning the plan as ExternalPlanSetup
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

/// Performs dose calculation and reports the MU of the first beam;
/// wraps failures in a Result
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
