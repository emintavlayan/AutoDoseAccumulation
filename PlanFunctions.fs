namespace VMS.TPS

open System
open System.Windows.Forms
open FsToolkit.ErrorHandling
open VMS.TPS.Common.Model.API
open VMS.TPS.Common.Model.Types

module PlanFunctions =

    // Modify an external plan by setting prescription and calculation model
    let setPrescription (plan : ExternalPlanSetup) (dose : float) =
        let doseValue =
            DoseValue(dose, "Gy")

        plan.SetPrescription(1, doseValue, 1)
        plan

    // Modify an external plan by setting prescription and calculation model
    let setCalculationModel (plan : ExternalPlanSetup) (calculationModel : string) =
        plan.SetCalculationModel(CalculationType.PhotonVolumeDose, calculationModel) // "accAcurosXB"
        plan

    // Copy the modified plan to a new image / structure set
    let copyPlanToNewImage
        (course : Course)
        (originalPlan : ExternalPlanSetup)
        (newImagePlan : ExternalPlanSetup)
        =

        // maybe we have to decide where to get structureset
        let newImage =
            newImagePlan.StructureSet.Image

        let newStructureSet =
            newImage.CreateNewStructureSet()

        let searchBodyParameters =
            newStructureSet.GetDefaultSearchBodyParameters()

        newStructureSet.CreateAndSearchBody(searchBodyParameters)
        |> ignore

        let outputDiagnostics =
            System.Text.StringBuilder("Type in the information about copied plan.")

        let copiedPlan =
            course.CopyPlanSetup(originalPlan, newStructureSet, outputDiagnostics)
            :?> ExternalPlanSetup

        copiedPlan.Id <- newImagePlan.Id + "C"
        Ok copiedPlan
